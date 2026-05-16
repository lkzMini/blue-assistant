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
        public bool IsWindowVisible => m_window != null;

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
                    _trayService.TrayMenuClicked += OnTrayMenuClicked;
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

        private void OnTrayMenuClicked(object? sender, TrayMenuAction action)
        {
            if (m_window is null) return;

            m_window.DispatcherQueue.TryEnqueue(() =>
            {
                var viewModel = Services.GetService<AssistantViewModel>();
                if (viewModel is null) return;

                switch (action)
                {
                    case TrayMenuAction.ShowHide:
                        if (m_window.Visible)
                            m_window.Hide();
                        else
                        {
                            m_window.Show();
                            m_window.Activate();
                            m_window.BringToFront();
                        }
                        break;

                    case TrayMenuAction.TogglePin:
                        viewModel.IsPinned = !viewModel.IsPinned;
                        _trayService.IsPinned = viewModel.IsPinned;
                        break;

                    case TrayMenuAction.RestartChat:
                        if (viewModel.RefreshChatCommand.CanExecute(null))
                            viewModel.RefreshChatCommand.Execute(null);
                        m_window.Show();
                        m_window.Activate();
                        m_window.BringToFront();
                        break;

                    case TrayMenuAction.OpenSettings:
                        OpenSettings();
                        break;

                    case TrayMenuAction.CheckOllama:
                        m_window.Show();
                        m_window.Activate();
                        m_window.BringToFront();
                        _ = CheckOllamaAsync(m_window.DispatcherQueue, viewModel);
                        break;

                    case TrayMenuAction.Exit:
                        Application.Current.Exit();
                        break;
                }
            });
        }

        internal static void Log(string msg)
        {
            try
            {
                var logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Blue");
                var logPath = System.IO.Path.Combine(logDir, "crash.log");
                System.IO.Directory.CreateDirectory(logDir);
                System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {msg}{Environment.NewLine}");
            }
            catch { }
        }

        private async Task CheckOllamaAsync(Microsoft.UI.Dispatching.DispatcherQueue dispatcher, AssistantViewModel viewModel)
        {
            try
            {
                var chatService = Services.GetService<IChatService>();
                if (chatService is null) return;

                var isRunning = await Task.Run(() => chatService.HealthCheckAsync());

                dispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        // Expand the chat so the user can see the result
                        m_window.Expand();
                        viewModel.ShowOllamaResult(isRunning);
                    }
                    catch (Exception innerEx)
                    {
                        Log($"CheckOllama-UI: {innerEx.GetType().Name}: {innerEx.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"CheckOllama: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void LogException(Exception ex, string prefix)
        {
            Log($"{prefix}: {ex.GetType().Name}: {ex.Message}");
            if (ex.StackTrace is not null)
                Log($"{prefix} STACK: {ex.StackTrace}");
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

        private static void OnUnobservedException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Exception is not null)
                Log($"UnobservedTaskException: {e.Exception.GetType().Name}: {e.Exception.Message}");
            e.SetObserved();
        }

        private static void OnUnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            Log($"UnhandledException: {ex?.GetType().Name}: {ex?.Message}");
            if (ex?.InnerException != null)
            {
                Log($"InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                Log($"InnerStackTrace: {ex.InnerException.StackTrace}");
            }
            else if (ex != null)
            {
                Log($"StackTrace: {ex.StackTrace}");
            }
            e.Handled = true;
        }
    }
}
