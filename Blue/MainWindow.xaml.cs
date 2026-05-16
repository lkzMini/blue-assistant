using Blue.Core.ViewModels;
using Blue.Core.Services;
using Blue.Helpers;
using Blue.Services;
using Blue.Windows;
using CubeKit.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;
using TerraFX.Interop.Windows;
using Windows.Foundation;
using Windows.Graphics;
using Windows.System;
using Windows.UI.Core;
using WinRT.Interop;
using WinUIEx;
using WinUIEx.Messaging;
using static TerraFX.Interop.Windows.SWP;
using static TerraFX.Interop.Windows.SW;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.GWL;
using static TerraFX.Interop.Windows.WS;

namespace Blue;

public sealed partial class MainWindow : WindowEx
{
    private SettingsService Settings = (SettingsService)App.Current.Services.GetService<ISettingsService>();
    private AssistantViewModel Assistant = App.Current.Services.GetService<AssistantViewModel>();
    WindowMessageMonitor m;
    private bool isMovePointerPressed;
    private bool isMovingWindow;
    private bool suppressNextCharacterTap;
    private NativeHelper.Point moveStartCursor;
    private NativeHelper.RECT moveStartWindowRect;
    private const uint WM_ERASEBKGND = 0x0014;
    private const int MoveDragThreshold = 4;
    private const double CharacterSize = 100;
    private const double CharacterPadding = 24;
    private const double CollapsedWindowSize = CharacterSize + (CharacterPadding * 2);
    private const double ChatWidth = 360;
    private const double PreferredChatHeight = 720;
    private const double MinChatHeight = 320;
    private const double CompanionGap = 8;
    private const double MonitorMargin = 12;
    private const string CharacterTooltipText = "Click to expand or collapse. Drag Blue to move him.";

    // Separate chat window — Blue window NEVER resizes or repositions on expand/collapse
    private ChatWindow _chatWindow;
    private bool _chatWindowCreated;
    private bool _chatWindowPositioned;
    private int _chatDragOffsetX;
    private int _chatDragOffsetY;

    public MainWindow()
    {
        this.InitializeComponent();
        ConfigureFloatingWindowChrome();
        m = new(this);
        unsafe
        {
            var hwnd = (HWND)this.GetWindowHandle();
            int lExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, lExStyle | WS_EX_LAYERED);
        }
        m.WindowMessageReceived += WindowMessageReceived;

        SystemBackdrop = new TransparentBackdrop();
        LayoutCanvas.Background = new SolidColorBrush(Colors.Transparent);
        SetCharacterTooltipEnabled(true);

        KeyboardListener.Setup(this);

        Assistant.IsExpanded = false;

        // Blue window size is FIXED for its entire lifetime — never changes
        LayoutCanvas.Width = CollapsedWindowSize;
        LayoutCanvas.Height = CollapsedWindowSize;
        LayoutCanvas.MaxHeight = CollapsedWindowSize;
        Canvas.SetLeft(CharacterButton, CharacterPadding);
        Canvas.SetTop(CharacterButton, CharacterPadding);
        Width = CollapsedWindowSize;
        Height = CollapsedWindowSize;

        // First launch: anchor to bottom-right of primary monitor
        var scale = GetScale();
        var primaryArea = DisplayArea.Primary.WorkArea;
        var halfChar = Convert.ToInt32((CharacterSize / 2) * scale);
        var anchorX = primaryArea.X + primaryArea.Width - halfChar - Convert.ToInt32(80 * scale);
        var anchorY = primaryArea.Y + primaryArea.Height - halfChar - Convert.ToInt32(40 * scale);
        var initLeft = anchorX - Convert.ToInt32(CollapsedWindowSize / 2 * scale);
        var initTop = anchorY - Convert.ToInt32(CollapsedWindowSize / 2 * scale);
        AppWindow.Move(new PointInt32(initLeft, initTop));
        AppWindow.Resize(new SizeInt32(Convert.ToInt32(CollapsedWindowSize * scale), Convert.ToInt32(CollapsedWindowSize * scale)));

        this.BringToFront();
        if (Assistant.IsPinned) Pin();
        else Unpin();
        App.Current.TrayService.IsPinned = Assistant.IsPinned;

        Assistant.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
        {
            if (e.PropertyName == "IsPinned")
            {
                if (Assistant.IsPinned) Pin();
                else Unpin();
                App.Current.TrayService.IsPinned = Assistant.IsPinned;
            }
        };
    }

    private void ConfigureFloatingWindowChrome()
    {
        ExtendsContentIntoTitleBar = true;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
        }
    }

    private unsafe void Pin()
    {
        var presenter = this.AppWindow.Presenter as OverlappedPresenter;
        var hwnd = (HWND)this.GetWindowHandle();
        if (presenter is not null) presenter.IsAlwaysOnTop = true;

        int lExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, lExStyle | WS_EX_TOPMOST);
        SetWindowPos(hwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

        // Sync ChatWindow to same pin state
        PinChatWindow();
    }

    private unsafe void Unpin()
    {
        var presenter = this.AppWindow.Presenter as OverlappedPresenter;
        var hwnd = (HWND)this.GetWindowHandle();
        if (presenter is not null) presenter.IsAlwaysOnTop = false;

        int lExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, lExStyle & ~WS_EX_TOPMOST);

        this.BringToFront();

        // Sync ChatWindow to same pin state
        UnpinChatWindow();
    }

    private unsafe void PinChatWindow()
    {
        if (!_chatWindowCreated) return;
        var hwnd = (HWND)_chatWindow.GetWindowHandle();
        int lExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, lExStyle | WS_EX_TOPMOST);
        SetWindowPos(hwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    private unsafe void UnpinChatWindow()
    {
        if (!_chatWindowCreated) return;
        var hwnd = (HWND)_chatWindow.GetWindowHandle();
        int lExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, lExStyle & ~WS_EX_TOPMOST);
    }

    private void WindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        try
        {
            if (e.Message.MessageId == WM_ERASEBKGND)
            {
                e.Handled = true;
                e.Result = 1;
            }
            App.Current.TrayService.HandleWindowMessage(
                e.Message.MessageId,
                e.Message.WParam,
                e.Message.LParam);
        }
        catch (Exception ex)
        {
            App.Log($"WindowMessageReceived CRASH: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private double GetScale()
    {
        var hwnd = this.GetWindowHandle();
        var monitor = NativeHelper.MonitorFromWindow(hwnd, NativeHelper.MONITOR_DEFAULTTONEAREST);
        NativeHelper.DeviceScaleFactor scale;
        NativeHelper.GetScaleFactorForMonitor(monitor, out scale);
        if (scale == NativeHelper.DeviceScaleFactor.DEVICE_SCALE_FACTOR_INVALID)
            scale = NativeHelper.DeviceScaleFactor.SCALE_100_PERCENT;
        return Convert.ToDouble(scale) / 100;
    }

    /// <summary>
    /// Show the chat window adjacent to Blue. Blue's own window NEVER moves or resizes.
    /// </summary>
    internal void Expand()
    {
        if (!_chatWindowCreated)
        {
            _chatWindow = new ChatWindow { Owner = this };
            _chatWindow.Closed += (s, e) =>
            {
                // If ChatWindow was closed directly (Alt+F4), reset for re-creation
                _chatWindow = null;
                _chatWindowCreated = false;
                _chatWindowPositioned = false;
                if (Assistant.IsExpanded)
        Assistant.IsExpanded = false;

        // Blue window size is FIXED for its entire lifetime — never changes
        Width = CollapsedWindowSize;
        Height = CollapsedWindowSize;
            };
            _chatWindowCreated = true;
            // Hide immediately so we can position before showing
            unsafe
            {
                var hwnd = (HWND)_chatWindow.GetWindowHandle();
                ShowWindow(hwnd, SW_HIDE);
            }
        }

        var currentPos = AppWindow.Position;
        var scale = GetScale();
        var workArea = GetCurrentWorkArea();

        // Blue window bounds (physical pixels)
        var blueLeft = currentPos.X;
        var blueTop = currentPos.Y;
        var blueRight = blueLeft + (int)(CollapsedWindowSize * scale);
        var blueBottom = blueTop + (int)(CollapsedWindowSize * scale);

        // Available space right of Blue
        var monitorRight = workArea.X + workArea.Width - (int)(MonitorMargin * scale);
        var availableRight = monitorRight - blueRight;
        var chatPhysicalWidth = (int)(ChatWidth * scale);

        // Horizontal: prefer RIGHT, fall back to LEFT
        int chatX;
        if (availableRight >= chatPhysicalWidth)
        {
            chatX = blueRight + (int)(CompanionGap * scale);
        }
        else
        {
            chatX = blueLeft - chatPhysicalWidth - (int)(CompanionGap * scale);
            // Clamp to monitor left edge
            if (chatX < workArea.X + (int)(MonitorMargin * scale))
                chatX = (int)(workArea.X + MonitorMargin * scale);
        }

        // Vertical: align top with Blue, fill available space below
        var availableBelow = (workArea.Y + workArea.Height - (int)(MonitorMargin * scale)) - blueTop;
        var chatPhysicalHeight = Math.Min((int)(PreferredChatHeight * scale), availableBelow);
        if (chatPhysicalHeight < (int)(MinChatHeight * scale))
            chatPhysicalHeight = (int)(MinChatHeight * scale);

        _chatWindow.Width = ChatWidth;
        _chatWindow.Height = chatPhysicalHeight / scale;

        unsafe
        {
            var hwnd = (HWND)_chatWindow.GetWindowHandle();
            SetWindowPos(hwnd, HWND.HWND_TOP,
                chatX, blueTop,
                chatPhysicalWidth, chatPhysicalHeight,
                SWP_SHOWWINDOW | SWP_NOACTIVATE);
        }

        Assistant.IsExpanded = true;
        _chatWindowPositioned = true;

        // Apply current pin state to ChatWindow
        if (Assistant.IsPinned)
            PinChatWindow();
    }

    /// <summary>
    /// Collapse: hide the chat window. Blue window stays exactly where it is.
    /// </summary>
    internal void Collapse()
    {
        if (_chatWindowCreated && _chatWindowPositioned)
        {
            unsafe
            {
                var hwnd = (HWND)_chatWindow.GetWindowHandle();
                ShowWindow(hwnd, SW_HIDE);
            }
            _chatWindowPositioned = false;
        }

        Assistant.IsExpanded = false;
    }

    private RectInt32 GetCurrentWorkArea()
    {
        try
        {
            return DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        }
        catch
        {
            return DisplayArea.Primary.WorkArea;
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        App.Current.OpenSettings();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }

    private void Hide_Click(object sender, RoutedEventArgs e)
    {
        Collapse();
    }

    private void Character_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (suppressNextCharacterTap)
        {
            suppressNextCharacterTap = false;
            return;
        }

        if (Assistant.IsExpanded)
            Collapse();
        else
            Expand();
    }

    private void SetCharacterTooltipEnabled(bool isEnabled)
    {
        ToolTipService.SetToolTip(CharacterButton, isEnabled ? CharacterTooltipText : null);
    }

    // === Drag support ===

    private void Character_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Hide tooltip while user interacts with Blue (drag or click)
        SetCharacterTooltipEnabled(false);

        moveStartCursor = new NativeHelper.Point();
        if (!NativeHelper.GetCursorPos(out moveStartCursor))
            return;

        NativeHelper.GetWindowRect(this.GetWindowHandle(), out moveStartWindowRect);
        isMovePointerPressed = true;
        isMovingWindow = false;
        CharacterButton.CapturePointer(e.Pointer);

        // Capture chat window offset from Blue at drag start
        if (_chatWindowCreated && _chatWindowPositioned)
        {
            var chatHwnd = (HWND)_chatWindow.GetWindowHandle();
            NativeHelper.GetWindowRect(chatHwnd, out NativeHelper.RECT chatRect);
            _chatDragOffsetX = chatRect.Left - moveStartWindowRect.Left;
            _chatDragOffsetY = chatRect.Top - moveStartWindowRect.Top;
        }
    }

    private void Character_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!isMovePointerPressed)
            return;

        if (!NativeHelper.GetCursorPos(out var cursor))
            return;

        var deltaX = cursor.X - moveStartCursor.X;
        var deltaY = cursor.Y - moveStartCursor.Y;

        if (!isMovingWindow && Math.Abs(deltaX) < MoveDragThreshold && Math.Abs(deltaY) < MoveDragThreshold)
            return;

        isMovingWindow = true;
        AppWindow.Move(new PointInt32(moveStartWindowRect.Left + deltaX, moveStartWindowRect.Top + deltaY));

        // ChatWindow follows Blue with the same drag offset
        if (_chatWindowCreated && _chatWindowPositioned)
        {
            unsafe
            {
                var chatHwnd = (HWND)_chatWindow.GetWindowHandle();
                SetWindowPos(chatHwnd, HWND.HWND_TOP,
                    moveStartWindowRect.Left + deltaX + _chatDragOffsetX,
                    moveStartWindowRect.Top + deltaY + _chatDragOffsetY,
                    0, 0,
                    SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }

        e.Handled = true;
    }

    private void Character_PointerReleased(object sender, PointerRoutedEventArgs e) => EndWindowMove(e);
    private void Character_PointerCanceled(object sender, PointerRoutedEventArgs e) => EndWindowMove(e);

    private void EndWindowMove(PointerRoutedEventArgs e)
    {
        var wasMovingWindow = isMovingWindow;

        if (isMovePointerPressed)
        {
            CharacterButton.ReleasePointerCapture(e.Pointer);
            e.Handled = wasMovingWindow;
        }

        isMovePointerPressed = false;
        isMovingWindow = false;
        suppressNextCharacterTap = wasMovingWindow;
        SetCharacterTooltipEnabled(true);

        // Re-position ChatWindow after drag ends (in case we moved between monitors)
        if (_chatWindowCreated && _chatWindowPositioned && _chatWindow is not null && wasMovingWindow)
        {
            PositionChatWindowAfterMove();
        }
    }

    /// <summary>
    /// Re-position the chat window after Blue is dragged to a new location.
    /// Re-evaluates LEFT/RIGHT placement based on the new monitor.
    /// </summary>
    private void PositionChatWindowAfterMove()
    {
        var currentPos = AppWindow.Position;
        var scale = GetScale();
        var workArea = GetCurrentWorkArea();

        var blueLeft = currentPos.X;
        var blueTop = currentPos.Y;
        var blueRight = blueLeft + (int)(CollapsedWindowSize * scale);

        var monitorRight = workArea.X + workArea.Width - (int)(MonitorMargin * scale);
        var availableRight = monitorRight - blueRight;
        var chatPhysicalWidth = (int)(ChatWidth * scale);

        int chatX;
        if (availableRight >= chatPhysicalWidth)
        {
            chatX = blueRight + (int)(CompanionGap * scale);
        }
        else
        {
            chatX = blueLeft - chatPhysicalWidth - (int)(CompanionGap * scale);
            if (chatX < workArea.X + (int)(MonitorMargin * scale))
                chatX = (int)(workArea.X + MonitorMargin * scale);
        }

        var availableBelow = (workArea.Y + workArea.Height - (int)(MonitorMargin * scale)) - blueTop;
        var chatPhysicalHeight = Math.Min((int)(PreferredChatHeight * scale), availableBelow);
        if (chatPhysicalHeight < (int)(MinChatHeight * scale))
            chatPhysicalHeight = (int)(MinChatHeight * scale);

        unsafe
        {
            var hwnd = (HWND)_chatWindow.GetWindowHandle();
            SetWindowPos(hwnd, HWND.HWND_TOP,
                chatX, blueTop,
                chatPhysicalWidth, chatPhysicalHeight,
                SWP_NOACTIVATE);
        }
    }
}
