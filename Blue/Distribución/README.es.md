# Blue

Blue es un asistente flotante de escritorio para Windows, construido con WinUI y potenciado por un LLM local a través de Ollama.

> Estado actual: alpha/beta personal para compartir con amigos y testers.

## Qué hace Blue

- Compañero flotante de escritorio
- Chat local usando Ollama
- Estados visuales del personaje:
  - Idle
  - Thinking
  - Happy
- Ventana del personaje movible
- Colapso/expansión desde el propio personaje

## Requisitos

- Windows 10 o Windows 11
- Blue compilado o empaquetado
- Ollama instalado localmente
- `phi3:latest` descargado en Ollama

## Dependencia externa

Actualmente Blue depende de:

- Ollama ejecutándose localmente
- endpoint local en `http://localhost:11434`
- modelo `phi3:latest`

Si Ollama no está corriendo, o si `phi3:latest` no existe, Blue mostrará un mensaje de ayuda/configuración en la UI.

## Archivos de assets del personaje

Blue espera exactamente estos archivos:

```text
Clippy/Assets/Dino/Dino.png
Clippy/Assets/Dino/Dino.Idle.png
Clippy/Assets/Dino/Dino.Happy.png
```

Mapa de estados:

- `Dino.png` -> pensando / trabajando
- `Dino.Idle.png` -> idle / relajado
- `Dino.Happy.png` -> feliz / respuesta completada

## Comportamiento de entrada

- `Enter` -> enviar mensaje
- `Shift + Enter` -> insertar salto de línea

## Comportamiento de ocultar / restaurar

En esta versión alpha/beta, Blue **no** depende de un tray icon.

- Botón de ocultar -> colapsa al personaje
- Click en Blue -> reabrir / expandir

Ese es el camino de restauración soportado para testers.

## Configuración rápida para testers

### 1. Instalar Ollama

Instalá Ollama en Windows.

### 2. Descargar el modelo requerido

Abrí PowerShell y ejecutá:

```powershell
ollama pull phi3:latest
```

### 3. Verificar Ollama

Ejecutá:

```powershell
ollama list
```

Deberías ver `phi3:latest`.

Chequeos opcionales:

```powershell
ollama run phi3:latest "Say hello in one short sentence."
curl http://localhost:11434/api/tags
```

### 4. Abrir Blue

Abrí Blue desde el paquete instalado o ejecutalo desde Visual Studio si lo vas a compilar localmente.

## Si Blue no responde

Blue puede mostrar mensajes como:

- Ollama no es accesible en `http://localhost:11434`
- falta `phi3:latest`
- falló una petición local a Ollama

La mayoría de las veces, la solución es:

```powershell
ollama pull phi3:latest
```

y asegurarse de que Ollama esté corriendo.

## Limitaciones conocidas

- Todavía no existe un flujo real de tray icon
- Esta es una build alpha/beta
- Quedan algunas warnings de build, pero el runtime está estable

## Objetivos actuales del proyecto

- Validar uso real de escritorio
- Recopilar feedback de testers de confianza
- Estabilizar empaquetado/distribución
