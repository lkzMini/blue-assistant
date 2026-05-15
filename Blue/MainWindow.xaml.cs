using CubeKit.UI.Helpers;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;
using Windows.Win32;
using WinRT.Interop;
using Clippy.Windows;
using WinUIEx.Messaging;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI;
using Microsoft.UI.Input;
using Clippy.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Clippy.Services;
using Clippy.Helpers;
using Clippy.Core.Services;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Input;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;
using System.Diagnostics.Eventing.Reader;
using TerraFX.Interop.Windows;
using Windows.Graphics;
using static TerraFX.Interop.Windows.WS;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.GWL;
using static TerraFX.Interop.Windows.SWP;
using static TerraFX.Interop.Windows.SW;
using System.Reflection.Metadata;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Clippy
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        private SettingsService Settings = (SettingsService)App.Current.Services.GetService<ISettingsService>();
        private ClippyViewModel Clippy = App.Current.Services.GetService<ClippyViewModel>();
        WindowMessageMonitor m;
        private bool isMovePointerPressed;
        private bool isMovingWindow;
        private bool suppressNextCharacterTap;
        private NativeHelper.Point moveStartCursor;
        private NativeHelper.RECT moveStartWindowRect;
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

        private enum CompanionPlacement
        {
            Left,
            Right,
            Up,
            Down
        }

        private readonly struct CompanionLayout
        {
            public CompanionLayout(
                CompanionPlacement placement,
                double windowWidth,
                double windowHeight,
                double windowLeft,
                double windowTop,
                double characterLeft,
                double characterTop,
                double chatLeft,
                double chatTop,
                double chatHeight,
                double overflow)
            {
                Placement = placement;
                WindowWidth = windowWidth;
                WindowHeight = windowHeight;
                WindowLeft = windowLeft;
                WindowTop = windowTop;
                CharacterLeft = characterLeft;
                CharacterTop = characterTop;
                ChatLeft = chatLeft;
                ChatTop = chatTop;
                ChatHeight = chatHeight;
                Overflow = overflow;
            }

            public CompanionPlacement Placement { get; }
            public double WindowWidth { get; }
            public double WindowHeight { get; }
            public double WindowLeft { get; }
            public double WindowTop { get; }
            public double CharacterLeft { get; }
            public double CharacterTop { get; }
            public double ChatLeft { get; }
            public double ChatTop { get; }
            public double ChatHeight { get; }
            public double Overflow { get; }
        }

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

            ClippyKeyboardListener.Setup(this);

            Clippy.IsClippyEnabled = false;
            Collapse();

            this.BringToFront();
			if (Clippy.IsPinned) Pin();
			else Unpin();
			Clippy.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) =>
            {
                if(e.PropertyName == "IsPinned")
                {
                    if (Clippy.IsPinned) Pin();
                    else Unpin();
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

			// Add the extended window styles for always on top
			int lExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, lExStyle | WS_EX_TOPMOST);

			// Move window to top
			SetWindowPos(hwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
		}

        private unsafe void Unpin()
        {
			var presenter = this.AppWindow.Presenter as OverlappedPresenter;
			var hwnd = (HWND)this.GetWindowHandle();
			if (presenter is not null) presenter.IsAlwaysOnTop = false;

			// Add the extended window styles for always on top
			int lExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, lExStyle & ~WS_EX_TOPMOST);

			this.BringToFront();
		}

        private void WindowMessageReceived(object? sender, WindowMessageEventArgs e)
        {
            if (e.Message.MessageId == PInvoke.WM_ERASEBKGND)
            {
                e.Handled = true;
                e.Result = 1;
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

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            App.Current.OpenSettings();
        }

        private Visibility BtoV(bool b) => b ? Visibility.Visible : Visibility.Collapsed;

        private void Clippy_Checked(object sender, RoutedEventArgs e) => Expand();

        private void Clippy_Unchecked(object sender, RoutedEventArgs e) => Collapse();

        private void Collapse()
        {
            ApplyCompanionLayout(false, GetCharacterAnchorScreenPoint());
        }

        private void Expand()
        {
            ApplyCompanionLayout(true, GetCharacterAnchorScreenPoint());
        }

        private void ApplyCompanionLayout(bool isExpanded, PointInt32 requestedAnchor)
        {
            var scale = GetScale();
            var workArea = GetCurrentWorkArea();
            var anchor = ClampCharacterAnchor(requestedAnchor, workArea, scale);

            if (!isExpanded)
            {
                ChatPanel.Visibility = Visibility.Collapsed;
                ApplyLayout(
                    windowWidth: CollapsedWindowSize,
                    windowHeight: CollapsedWindowSize,
                    windowLeft: anchor.X - (CollapsedWindowSize / 2 * scale),
                    windowTop: anchor.Y - (CollapsedWindowSize / 2 * scale),
                    characterLeft: CharacterPadding,
                    characterTop: CharacterPadding,
                    chatLeft: 0,
                    chatTop: 0,
                    chatHeight: 0,
                    workArea: workArea,
                    scale: scale);
                return;
            }

            var layout = ChooseExpandedLayout(anchor, workArea, scale);
            ChatPanel.Visibility = Visibility.Visible;
            ApplyLayout(
                layout.WindowWidth,
                layout.WindowHeight,
                layout.WindowLeft,
                layout.WindowTop,
                layout.CharacterLeft,
                layout.CharacterTop,
                layout.ChatLeft,
                layout.ChatTop,
                layout.ChatHeight,
                workArea,
                scale);
        }

        private CompanionLayout ChooseExpandedLayout(PointInt32 anchor, RectInt32 workArea, double scale)
        {
            var reservedCharacterHeight = CharacterSize + (CharacterPadding * 2);
            var availableHeight = Math.Max(MinChatHeight, (workArea.Height / scale) - (MonitorMargin * 2));
            var horizontalChatHeight = Math.Min(PreferredChatHeight, availableHeight);
            var verticalChatHeight = Math.Min(PreferredChatHeight, Math.Max(MinChatHeight, (workArea.Height / scale) - reservedCharacterHeight - CompanionGap - (MonitorMargin * 2)));

            var candidates = new[]
            {
                BuildExpandedLayout(CompanionPlacement.Left, anchor, workArea, scale, horizontalChatHeight),
                BuildExpandedLayout(CompanionPlacement.Right, anchor, workArea, scale, horizontalChatHeight),
                BuildExpandedLayout(CompanionPlacement.Up, anchor, workArea, scale, verticalChatHeight),
                BuildExpandedLayout(CompanionPlacement.Down, anchor, workArea, scale, verticalChatHeight),
            };

            return candidates
                .OrderBy(layout => layout.Overflow)
                .ThenBy(layout => layout.Placement == CompanionPlacement.Left ? 0 :
                                  layout.Placement == CompanionPlacement.Right ? 1 :
                                  layout.Placement == CompanionPlacement.Up ? 2 : 3)
                .First();
        }

        private CompanionLayout BuildExpandedLayout(CompanionPlacement placement, PointInt32 anchor, RectInt32 workArea, double scale, double chatHeight)
        {
            double windowWidth;
            double windowHeight;
            double characterLeft;
            double characterTop;
            double chatLeft;
            double chatTop;

            switch (placement)
            {
                case CompanionPlacement.Right:
                    windowWidth = CharacterPadding + CharacterSize + CompanionGap + ChatWidth;
                    windowHeight = Math.Max(chatHeight, CharacterSize + (CharacterPadding * 2));
                    characterLeft = CharacterPadding;
                    characterTop = windowHeight - CharacterSize - CharacterPadding;
                    chatLeft = CharacterPadding + CharacterSize + CompanionGap;
                    chatTop = 0;
                    break;
                case CompanionPlacement.Up:
                    windowWidth = Math.Max(ChatWidth, CharacterSize + (CharacterPadding * 2));
                    windowHeight = chatHeight + CompanionGap + CharacterSize + CharacterPadding;
                    characterLeft = (windowWidth - CharacterSize) / 2;
                    characterTop = chatHeight + CompanionGap;
                    chatLeft = (windowWidth - ChatWidth) / 2;
                    chatTop = 0;
                    break;
                case CompanionPlacement.Down:
                    windowWidth = Math.Max(ChatWidth, CharacterSize + (CharacterPadding * 2));
                    windowHeight = CharacterPadding + CharacterSize + CompanionGap + chatHeight;
                    characterLeft = (windowWidth - CharacterSize) / 2;
                    characterTop = CharacterPadding;
                    chatLeft = (windowWidth - ChatWidth) / 2;
                    chatTop = CharacterPadding + CharacterSize + CompanionGap;
                    break;
                default:
                    windowWidth = ChatWidth + CompanionGap + CharacterSize + CharacterPadding;
                    windowHeight = Math.Max(chatHeight, CharacterSize + (CharacterPadding * 2));
                    characterLeft = ChatWidth + CompanionGap;
                    characterTop = windowHeight - CharacterSize - CharacterPadding;
                    chatLeft = 0;
                    chatTop = 0;
                    break;
            }

            var windowLeft = anchor.X - ((characterLeft + (CharacterSize / 2)) * scale);
            var windowTop = anchor.Y - ((characterTop + (CharacterSize / 2)) * scale);
            var overflow = CalculateOverflow(windowLeft, windowTop, windowWidth * scale, windowHeight * scale, workArea);

            return new CompanionLayout(placement, windowWidth, windowHeight, windowLeft, windowTop, characterLeft, characterTop, chatLeft, chatTop, chatHeight, overflow);
        }

        private void ApplyLayout(
            double windowWidth,
            double windowHeight,
            double windowLeft,
            double windowTop,
            double characterLeft,
            double characterTop,
            double chatLeft,
            double chatTop,
            double chatHeight,
            RectInt32 workArea,
            double scale)
        {
            var windowWidthPixels = windowWidth * scale;
            var windowHeightPixels = windowHeight * scale;
            windowLeft = ClampToRange(windowLeft, workArea.X + MonitorMargin, workArea.X + workArea.Width - windowWidthPixels - MonitorMargin);
            windowTop = ClampToRange(windowTop, workArea.Y + MonitorMargin, workArea.Y + workArea.Height - windowHeightPixels - MonitorMargin);
            var visibleCharacterPosition = ClampWindowPositionToKeepCharacterVisible(windowLeft, windowTop, characterLeft, characterTop, workArea, scale);
            windowLeft = visibleCharacterPosition.X;
            windowTop = visibleCharacterPosition.Y;

            Width = windowWidth;
            Height = windowHeight;
            LayoutCanvas.Width = windowWidth;
            LayoutCanvas.Height = windowHeight;
            ChatPanel.Width = ChatWidth;
            ChatPanel.Height = chatHeight;
            Canvas.SetLeft(ChatPanel, chatLeft);
            Canvas.SetTop(ChatPanel, chatTop);
            Canvas.SetLeft(CharacterButton, characterLeft);
            Canvas.SetTop(CharacterButton, characterTop);

            AppWindow.ResizeClient(new SizeInt32(
                Convert.ToInt32(Math.Ceiling(windowWidthPixels)),
                Convert.ToInt32(Math.Ceiling(windowHeightPixels))));
            AppWindow.Move(new PointInt32(
                Convert.ToInt32(windowLeft),
                Convert.ToInt32(windowTop)));
            LayoutCanvas.MaxHeight = windowHeight;
        }

        private PointInt32 GetCharacterAnchorScreenPoint()
        {
            var scale = GetScale();

            if (CharacterButton is not null && CharacterButton.ActualWidth > 0 && CharacterButton.ActualHeight > 0)
            {
                try
                {
                    var relative = CharacterButton
                        .TransformToVisual(LayoutCanvas)
                        .TransformPoint(new Point(CharacterButton.ActualWidth / 2, CharacterButton.ActualHeight / 2));

                    return new PointInt32(
                        AppWindow.Position.X + Convert.ToInt32(relative.X * scale),
                        AppWindow.Position.Y + Convert.ToInt32(relative.Y * scale));
                }
                catch
                {
                    // Fall through to the default anchor if layout is not ready yet.
                }
            }

            var workArea = GetCurrentWorkArea();
            var halfCharacter = Convert.ToInt32((CharacterSize / 2) * scale);
            return new PointInt32(
                workArea.X + workArea.Width - halfCharacter - Convert.ToInt32(80 * scale),
                workArea.Y + workArea.Height - halfCharacter - Convert.ToInt32(40 * scale));
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

        private PointInt32 ClampCharacterAnchor(PointInt32 anchor, RectInt32 workArea, double scale)
        {
            var halfCharacter = (CharacterSize / 2) * scale;
            return new PointInt32(
                Convert.ToInt32(ClampToRange(anchor.X, workArea.X + halfCharacter + MonitorMargin, workArea.X + workArea.Width - halfCharacter - MonitorMargin)),
                Convert.ToInt32(ClampToRange(anchor.Y, workArea.Y + halfCharacter + MonitorMargin, workArea.Y + workArea.Height - halfCharacter - MonitorMargin)));
        }

        private static double CalculateOverflow(double left, double top, double width, double height, RectInt32 bounds)
        {
            var overflowLeft = Math.Max(0, bounds.X + MonitorMargin - left);
            var overflowTop = Math.Max(0, bounds.Y + MonitorMargin - top);
            var overflowRight = Math.Max(0, (left + width) - (bounds.X + bounds.Width - MonitorMargin));
            var overflowBottom = Math.Max(0, (top + height) - (bounds.Y + bounds.Height - MonitorMargin));
            return overflowLeft + overflowTop + overflowRight + overflowBottom;
        }

        private PointInt32 ClampCurrentWindowPositionToKeepCharacterVisible(double windowLeft, double windowTop)
        {
            var scale = GetScale();
            var workArea = GetCurrentWorkArea();
            var characterLeft = Canvas.GetLeft(CharacterButton);
            var characterTop = Canvas.GetTop(CharacterButton);

            if (double.IsNaN(characterLeft))
                characterLeft = CharacterPadding;

            if (double.IsNaN(characterTop))
                characterTop = CharacterPadding;

            return ClampWindowPositionToKeepCharacterVisible(windowLeft, windowTop, characterLeft, characterTop, workArea, scale);
        }

        private static PointInt32 ClampWindowPositionToKeepCharacterVisible(
            double windowLeft,
            double windowTop,
            double characterLeft,
            double characterTop,
            RectInt32 workArea,
            double scale)
        {
            var minWindowLeft = workArea.X + MonitorMargin - (characterLeft * scale);
            var maxWindowLeft = workArea.X + workArea.Width - MonitorMargin - ((characterLeft + CharacterSize) * scale);
            var minWindowTop = workArea.Y + MonitorMargin - (characterTop * scale);
            var maxWindowTop = workArea.Y + workArea.Height - MonitorMargin - ((characterTop + CharacterSize) * scale);

            return new PointInt32(
                Convert.ToInt32(ClampToRange(windowLeft, minWindowLeft, maxWindowLeft)),
                Convert.ToInt32(ClampToRange(windowTop, minWindowTop, maxWindowTop)));
        }

        private static double ClampToRange(double value, double min, double max)
        {
            if (max < min)
                return min;

            return Math.Min(Math.Max(value, min), max);
        }

		// Bool to Visibility
		public Visibility BoolToVis(bool b) => b ? Visibility.Visible : Visibility.Collapsed;

		// Bool to inverted visibility
		public Visibility InvertBoolToVis(bool b) => b ? Visibility.Collapsed : Visibility.Visible;

        private void Character_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (suppressNextCharacterTap)
            {
                suppressNextCharacterTap = false;
                e.Handled = true;
                return;
            }

            Clippy.IsClippyEnabled = !Clippy.IsClippyEnabled;
            if (Clippy.IsClippyEnabled)
                Expand();
            else
                Collapse();
        }

		private void Character_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
            var pointer = e.GetCurrentPoint(CharacterButton);
            if (!pointer.Properties.IsLeftButtonPressed)
                return;

            SetCharacterTooltipEnabled(false);

            if (!NativeHelper.GetCursorPos(out moveStartCursor))
            {
                SetCharacterTooltipEnabled(true);
                return;
            }

            NativeHelper.GetWindowRect(this.GetWindowHandle(), out moveStartWindowRect);
            isMovePointerPressed = true;
            isMovingWindow = false;
            CharacterButton.CapturePointer(e.Pointer);
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
            var nextPosition = ClampCurrentWindowPositionToKeepCharacterVisible(moveStartWindowRect.Left + deltaX, moveStartWindowRect.Top + deltaY);
            AppWindow.Move(nextPosition);
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
        }

        private void SetCharacterTooltipEnabled(bool isEnabled)
        {
            ToolTipService.SetToolTip(CharacterButton, isEnabled ? CharacterTooltipText : null);
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

            if (sender is TextBox textBox)
                Clippy.CurrentText = textBox.Text;

            if (!Clippy.SendPromptCommand.IsRunning && Clippy.SendPromptCommand.CanExecute(null))
                Clippy.SendPromptCommand.Execute(null);
		}

		private void TextBox_KeyUp(object sender, KeyRoutedEventArgs e)
		{
		}

		private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Exit();

		private void Hide_Click(object sender, RoutedEventArgs e)
		{
            Clippy.IsClippyEnabled = false;
            Collapse();
            Activate();
		}
	}
}
