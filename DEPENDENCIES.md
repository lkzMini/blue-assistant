# Blue Assistant — Dependencies

## For end users (running Blue)

### Runtime requirements
- **Windows 10** (build 18362+) or **Windows 11**
- **Ollama** installed and running locally
- **phi3:latest** model pulled in Ollama
- **Microsoft Windows App SDK Runtime** (installed automatically with the app if missing, or via: https://aka.ms/windowsappsdk/runtime)

### Required model
```
ollama pull phi3:latest
```

### Startup check
Blue verifies that Ollama is reachable at `http://localhost:11434` and that `phi3:latest` is available on startup. If either check fails, a message is shown in the UI.

---

## For developers (building from source)

### Build requirements
- **.NET 9 SDK** or later
- **Visual Studio 2022** (17.2+) with:
  - .NET desktop development workload
  - Windows App SDK workload
  - Windows 10 SDK (10.0.26100.0)
- **Git**

### Restore & build
```powershell
git clone https://github.com/lkzMini/blue-assistant.git
cd blue-assistant
git checkout beta
dotnet restore
dotnet build /p:Platform=x64 /p:Configuration=Release
```

### NuGet dependencies (auto-restored)
| Package | Version | Purpose |
|---------|---------|---------|
| CommunityToolkit.Mvvm | 8.4.0 | MVVM framework |
| CommunityToolkit.WinUI.UI.Controls.Markdown | 7.1.2 | Markdown rendering in chat |
| CommunityToolkit.WinUI.UI.Animations | 7.1.2 | UI animations |
| CommunityToolkit.WinUI.UI.Media | 7.1.2 | Media/effects helpers |
| Microsoft.Extensions.DependencyInjection | 9.0.8 | DI container |
| Microsoft.Windows.CsWinRT | 2.2.0 | WinRT interop |
| Microsoft.WindowsAppSDK | 1.6+ | Windows App SDK |
| TerraFX.Interop.Windows | 10.0.26100.2 | Win32 P/Invoke bindings |
| WindowsInput | 6.4.1 | Input simulation |
| WinUIEx | 2.9.0 | Window management helpers |

---

## Project structure
```
BlueAssistant.sln
├── Blue/             Main desktop app (WinUI 3, unpackaged)
│   ├── CubeKit.UI/   UI components (icons, DropShadowPanel, themes)
│   ├── Controls/     Custom UI controls
│   ├── Helpers/      Keyboard listener, native helpers
│   ├── Services/     Settings, tray
│   ├── Tray/         System tray icon service
│   └── Windows/      Mica window, dispatcher helpers
├── Blue.Core/        Core logic, services, view models
└── Blue/Distribución/  Distribution files
```
