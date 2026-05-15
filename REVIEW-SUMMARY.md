# Blue Assistant — Session Review

## Resumen

Implementación de tray icon + limpieza de dependencias heredadas de CommunityToolkit.

## Cambios

### ♻️ Dependency cleanup
| Archivo | Cambio |
|---------|--------|
| `CubeKit.UI/CubeKit.UI.csproj` | Removido `CommunityToolkit.WinUI.UI.Controls` (v7.1.2), removido `CommunityToolkit.WinUI.UI.Behaviors`, agregado `CommunityToolkit.WinUI.UI.Animations` como dependencia directa |
| `Blue/Blue.csproj` | Removido `CommunityToolkit.WinUI.UI.Behaviors` |
| `Blue/Controls/APIBox.xaml` | Removido `xmlns:behaviors` no usado |
| `Blue/Controls/Messages/AssistantMessage.xaml.cs` | Removido `using CommunityToolkit.WinUI.UI.Controls` |

**Retenido**: `CommunityToolkit.WinUI.UI.Controls.Markdown` (sigue en Blue.csproj — necesario para `MarkdownTextBlock` en ShineUITextblock). `CommunityToolkit.WinUI.UI.Media` se mantiene en CubeKit.UI para `BackdropBlurBrush`.

### 🖼️ DropShadowPanel migration
| Archivo | Cambio |
|---------|--------|
| `CubeKit.UI/CubeKit.UI.csproj` | Incluida carpeta `Controls\Toolkit\` en la compilación (estaba excluida) |
| `CubeKit.UI/Themes/Generic.xaml` | Agregado default style para DropShadowPanel |
| `Blue/App.xaml` | Namespace `toolkitControls` → `CubeKit.UI.Controls.Toolkit` |
| `Blue/MainWindow.xaml` | Mismo cambio de namespace |

DropShadowPanel ahora usa `Microsoft.UI.Composition.DropShadow` nativo (Composition API) en vez del de CommunityToolkit.

### 🎯 Tray icon service (Shell_NotifyIconW)
| Archivo | Cambio |
|---------|--------|
| `Blue/Tray/TrayService.cs` | **Nuevo**: implementación con P/Invoke directo sobre `shell32.dll` + `user32.dll` |
| `Blue/App.xaml.cs` | Crea e inicializa `TrayService` tras crear MainWindow. Handler `OnTrayLeftClick` → Activate + Show + BringToFront |
| `Blue/MainWindow.xaml.cs` | Forward de mensajes de ventana a `TrayService.HandleWindowMessage` |
| `Blue/NativeMethods.txt` | Agregados: `Shell_NotifyIconW`, `NOTIFYICONDATAW`, `LoadIconW`, `DestroyIcon`, `LoadImageW` |

### 🛠️ Misc
| Archivo | Cambio |
|---------|--------|
| `.engram/config.json` | Registro de proyecto blue-assistant para Engram |

## Decisiones técnicas

| Decisión | Alternativa | Por qué |
|----------|------------|---------|
| **P/Invoke directo** sobre `Shell_NotifyIconW` | CsWin32 bindings | CsWin32 no genera `NOTIFY_ICONDATA_FLAGS` ni `LoadIconW` con este SDK. Raw P/Invoke evita problemas de tooling. |
| **`LoadImageW` + `LR_SHARED`** en vez de `LoadIconW` | `LoadIconW` no disponible en CsWin32 | API equivalente que sí funciona. `LR_SHARED` significa que el sistema es dueño del handle — NO llamar a `DestroyIcon`. |
| **Callback en `WM_APP + 1`** (0x8000) | Rango de mensajes de sistema | El rango `0x8000`-`0xBFFF` es privado para aplicaciones, no choca con mensajes del sistema. |
| **Icono default** (`IDI_APPLICATION`) | Icono custom | Temporal hasta tener un .ico propio de Blue. |

## Build & Smoke test

```
Build: 0 errors, 2 warnings (CS8632 — nullable, pre-existing)
Runtime: App runs 3+ seconds without crash
```

## Riesgos / Pendientes

1. **Icono genérico** — IDI_APPLICATION se ve como ventana. Necesita .ico custom
2. **Menú contextual** — falta handler para `WM_RBUTTONUP`
3. **P/Invoke duplicados** — TerraFX.Interop.Windows y CsWin32 conviven, se puede limpiar después
4. **Engram** — necesita arrancar desde `blue-assistant/` para sesiones futuras (`.engram/config.json` ya creado)
5. **Visual bug**: Blue recortado al expandir chat, parte inferior del chat tiene problemas — preexistente, no de esta sesión
6. **SettingsService** migrado a JSON file (`%LOCALAPPDATA%\Blue\settings.json`) pero no probado a fondo — reemplazo rápido del `ApplicationData.Current.LocalSettings` que no funciona en unpackaged
7. **`AppInstance.GetActivatedEventArgs()`** tira COMException en unpackaged — manejado con try-catch, siempre muestra la ventana
