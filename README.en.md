# Blue

Blue is a floating desktop assistant for Windows built with WinUI and powered by a local LLM through Ollama.

> Current status: personal alpha/beta for friend/tester distribution.

## What Blue does

- Floating desktop companion
- Local chat using Ollama
- Character states:
  - Idle
  - Thinking
  - Happy
- Draggable character window
- Collapse/expand from the character itself

## Requirements

- Windows 10 or Windows 11
- Blue package/build
- Ollama installed locally
- `phi3:latest` downloaded in Ollama

## External dependency

Blue currently depends on:

- Ollama running locally
- local endpoint at `http://localhost:11434`
- model `phi3:latest`

If Ollama is not running, or if `phi3:latest` is missing, Blue will show a setup/help message in the UI.

## Character asset files

Blue expects these exact files:

```text
Clippy/Assets/Dino/Dino.png
Clippy/Assets/Dino/Dino.Idle.png
Clippy/Assets/Dino/Dino.Happy.png
```

State mapping:

- `Dino.png` -> thinking / working
- `Dino.Idle.png` -> idle / relaxed
- `Dino.Happy.png` -> happy / response complete

## Input behavior

- `Enter` -> send message
- `Shift + Enter` -> insert newline

## Hide / restore behavior

For this alpha/beta version, Blue does **not** depend on a tray icon.

- Hide button -> collapse to character
- Click Blue -> reopen / expand

This is the supported restore path for testers.

## Quick setup for testers

### 1. Install Ollama

Install Ollama for Windows.

### 2. Pull the required model

Open PowerShell and run:

```powershell
ollama pull phi3:latest
```

### 3. Verify Ollama

Run:

```powershell
ollama list
```

You should see `phi3:latest`.

Optional checks:

```powershell
ollama run phi3:latest "Say hello in one short sentence."
curl http://localhost:11434/api/tags
```

### 4. Launch Blue

Open Blue from the installed package, or launch it from Visual Studio if building locally.

## If Blue cannot answer

Blue may show messages like:

- Ollama is not reachable at `http://localhost:11434`
- `phi3:latest` is missing
- local Ollama request failed

Most of the time, the fix is:

```powershell
ollama pull phi3:latest
```

and making sure Ollama is running.

## Known limitations

- No real tray icon workflow yet
- This is an alpha/beta build
- Some build warnings remain, but runtime is stable

## Current project goals

- Validate real-world desktop usage
- Gather feedback from trusted testers
- Stabilize packaging/distribution
