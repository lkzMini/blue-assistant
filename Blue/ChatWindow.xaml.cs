using Blue.Core.ViewModels;
using Blue.Windows;
using CubeKit.UI.Helpers;
using CubeKit.UI.Icons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using TerraFX.Interop.Windows;
using Windows.Graphics;
using Windows.System;
using Windows.UI.Core;
using WinUIEx;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WS;
using static TerraFX.Interop.Windows.GWL;
using static TerraFX.Interop.Windows.SWP;

namespace Blue;

public sealed partial class ChatWindow : WindowEx
{
    private AssistantViewModel Assistant => App.Current.Services.GetService<AssistantViewModel>();

    /// <summary>
    /// Reference to the owner MainWindow — used to signal collapse, share position info.
    /// Set by MainWindow right after construction.
    /// </summary>
    public MainWindow Owner { get; set; }

    public ChatWindow()
    {
        this.InitializeComponent();
        FollowToggle.IsChecked = true;
        ConfigureFloatingWindow();

        SystemBackdrop = new TransparentBackdrop();

        unsafe
        {
            var hwnd = (HWND)this.GetWindowHandle();
            int lExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, lExStyle | WS_EX_LAYERED);
        }
    }

    private void ConfigureFloatingWindow()
    {
        ExtendsContentIntoTitleBar = true;
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
        }
    }

    private void TextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        var isShiftDown = (shiftState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        if (isShiftDown)
            return;

        e.Handled = true;
        Assistant.SendPromptCommand.Execute(null);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        App.Current.OpenSettings();
    }

    private void Hide_Click(object sender, RoutedEventArgs e)
    {
        Owner?.Collapse();
    }

    public Visibility BoolToVis(bool b) => b ? Visibility.Visible : Visibility.Collapsed;
    public Visibility InvertBoolToVis(bool b) => b ? Visibility.Collapsed : Visibility.Visible;

    // === Independent Drag Support ===

    private bool _isDragPointerPressed;
    private bool _isDragging;
    private NativeHelper.Point _dragStartCursor;
    private NativeHelper.RECT _dragStartWindowRect;
    private const int DragThreshold = 4;
    private const double ChatMonitorMargin = 12;

    private void DragHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!NativeHelper.GetCursorPos(out _dragStartCursor))
            return;
        NativeHelper.GetWindowRect(this.GetWindowHandle(), out _dragStartWindowRect);
        _isDragPointerPressed = true;
        _isDragging = false;
        DragHandle.CapturePointer(e.Pointer);
    }

    private void DragHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragPointerPressed) return;
        if (!NativeHelper.GetCursorPos(out var cursor)) return;

        var deltaX = cursor.X - _dragStartCursor.X;
        var deltaY = cursor.Y - _dragStartCursor.Y;

        if (!_isDragging && Math.Abs(deltaX) < DragThreshold && Math.Abs(deltaY) < DragThreshold)
            return;

        _isDragging = true;

        unsafe
        {
            var hwnd = (HWND)this.GetWindowHandle();
            int x = _dragStartWindowRect.Left + deltaX;
            int y = _dragStartWindowRect.Top + deltaY;
            int w = _dragStartWindowRect.Right - _dragStartWindowRect.Left;
            int h = _dragStartWindowRect.Bottom - _dragStartWindowRect.Top;

            ClampToWorkArea(ref x, ref y, w, h);

            SetWindowPos(hwnd, HWND.HWND_TOP, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
            e.Handled = true;
        }
    }

    private void DragHandle_PointerReleased(object sender, PointerRoutedEventArgs e) => EndDrag(e);
    private void DragHandle_PointerCanceled(object sender, PointerRoutedEventArgs e) => EndDrag(e);

    private void EndDrag(PointerRoutedEventArgs e)
    {
        if (_isDragPointerPressed)
        {
            DragHandle.ReleasePointerCapture(e.Pointer);
            e.Handled = _isDragging;
        }
        _isDragPointerPressed = false;
        _isDragging = false;
    }

    private void ClampToWorkArea(ref int x, ref int y, int width, int height)
    {
        var scale = GetChatScale();
        var workArea = GetChatWorkArea();
        var margin = (int)(ChatMonitorMargin * scale);

        var minX = workArea.X + margin;
        var maxX = workArea.X + workArea.Width - width - margin;
        if (x < minX) x = minX;
        if (x > maxX) x = maxX;

        var minY = workArea.Y + margin;
        var maxY = workArea.Y + workArea.Height - height - margin;
        if (y < minY) y = minY;
        if (y > maxY) y = maxY;
    }

    private double GetChatScale()
    {
        var hwnd = this.GetWindowHandle();
        var monitor = NativeHelper.MonitorFromWindow(hwnd, NativeHelper.MONITOR_DEFAULTTONEAREST);
        NativeHelper.DeviceScaleFactor scale;
        NativeHelper.GetScaleFactorForMonitor(monitor, out scale);
        if (scale == NativeHelper.DeviceScaleFactor.DEVICE_SCALE_FACTOR_INVALID)
            scale = NativeHelper.DeviceScaleFactor.SCALE_100_PERCENT;
        return Convert.ToDouble(scale) / 100;
    }

    private RectInt32 GetChatWorkArea()
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

    // === Follow Toggle ===

    private void FollowToggle_Checked(object sender, RoutedEventArgs e)
    {
        Owner?.SetChatFollowsBlue(true);
        FollowIcon.Symbol = FluentSymbol.Link20;
        ToolTipService.SetToolTip(FollowToggle, "Chat follows Blue");
    }

    private void FollowToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        Owner?.SetChatFollowsBlue(false);
        FollowIcon.Symbol = FluentSymbol.LinkDismiss20;
        ToolTipService.SetToolTip(FollowToggle, "Chat is detached");
    }
}
