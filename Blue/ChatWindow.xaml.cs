using Blue.Core.ViewModels;
using Blue.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using TerraFX.Interop.Windows;
using Windows.System;
using Windows.UI.Core;
using WinUIEx;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WS;
using static TerraFX.Interop.Windows.GWL;

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
}
