using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Blue.Core.ViewModels;
using Blue.Core.Services;
using Blue.Services;
using System.Threading.Tasks;
using WinUIEx;
using Blue.Tray;
using System.Threading;
using Microsoft.UI;
using WinRT.Interop;

namespace Blue
{
    public partial class App : Application
    {
        private const string MutexID = "BlueAssistantMutex";
        private static Mutex? SingleInstanceMutex;

        public new static App Current => (App)Application.Current;
        internal TrayService TrayService => _trayService;

        public IServiceProvider Services { get; }

        public App()
        {
            Services = ConfigureServices();
            this.InitializeComponent();
            UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedException;
            CheckSingleInstance();
        }

        private void CheckSingleInstance()
        {
            bool isNewInstance;
            SingleInstanceMutex = new Mutex(true, MutexID, out isNewInstance);
            if (!isNewInstance)
                System.Environment.Exit(0);
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IChatService, ChatService>();
            services.AddSingleton<IKeyService, KeyService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<AssistantViewModel>();
            return services.BuildServiceProvider();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // In unpackaged mode, AppInstance.GetActivatedEventArgs() throws COMException.
            // Always show the main window regardless.
            try
            {
                if (AppInstance.GetActivatedEventArgs().Kind != ActivationKind.StartupTask)
                    ShowBlue();
            }
            catch
            {
                ShowBlue();
            }

            TrayWindow = new TrayFlyoutWindow();
        }

        public void ShowBlue()
        {
            if (m_window is null)
            {
                m_window = new MainWindow();
                InitializeTray();
            }
            m_window.Activate();
            m_window.Show();
        }

        private void InitializeTray()
        {
            try
            {
                if (m_window is null) return;

                var hwnd = WindowNative.GetWindowHandle(m_window);
                if (_trayService.Initialize(hwnd))
                {
                    _trayService.TrayLeftClick += OnTrayLeftClick;
                }
            }
            catch
            {
                // Non-fatal — app works without tray
            }
        }

        private void OnTrayLeftClick(object? sender, EventArgs e)
        {
            if (m_window is not null)
            {
                m_window.DispatcherQueue.TryEnqueue(() =>
                {
                    m_window.Activate();
                    m_window.Show();
                    m_window.BringToFront();
                });
            }
        }

        public void OpenSettings()
        {
            if (s_window is null)
                s_window = new SettingsWindow();
            s_window.Activate();
            s_window.Closed += (sender, e) => { s_window = null; };
        }

        private MainWindow m_window;
        private readonly TrayService _trayService = new();
        private Window s_window;
        private TrayFlyoutWindow TrayWindow = null!;

        private static void OnUnobservedException(object? sender, UnobservedTaskExceptionEventArgs e) => e.SetObserved();

        private static void OnUnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) => e.Handled = true;
    }
}
