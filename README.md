# Blue Assistant

A floating desktop assistant for Windows powered by a local LLM through Ollama.

> Current status: personal alpha/beta build shared for testing.

## What is Blue?

Blue is a lightweight desktop companion built with WinUI.
It stays on screen as a movable character and lets you chat with a local model running through Ollama.

### Current features

- Floating desktop companion
- Local chat with Ollama
- Character visual states:
  - Idle
  - Thinking
  - Happy
- Movable character window
- Collapse / expand directly from the character UI
- Local-first usage

## Current requirements

- Windows 10 or Windows 11
- Ollama installed locally
- `phi3:latest` available in Ollama
- Local Ollama endpoint available at:

```text
http://localhost:11434
```

If Ollama is not running, or if `phi3:latest` is missing, Blue will show a helpful message in the UI.

## Quick start

### 1) Install Ollama

Install Ollama on Windows.

### 2) Pull the required model

Open PowerShell and run:

```powershell
ollama pull phi3:latest
```

### 3) Verify Ollama

```powershell
ollama list
```

You should see `phi3:latest`.

Optional checks:

```powershell
ollama run phi3:latest "Say hello in one short sentence."
curl http://localhost:11434/api/tags
```

### 4) Run Blue

Open Blue from the packaged build, or run it from Visual Studio if you are building locally.

## Character assets

Blue currently expects these files:

```text
Clippy/Assets/Dino/Dino.png
Clippy/Assets/Dino/Dino.Idle.png
Clippy/Assets/Dino/Dino.Happy.png
```

State mapping:

- `Dino.png` → thinking / working
- `Dino.Idle.png` → idle / relaxed
- `Dino.Happy.png` → happy / completed response

## Input behavior

- `Enter` → send message
- `Shift + Enter` → insert line break

## Hide / restore behavior

In this alpha/beta version, Blue does not depend on a tray icon.

- Hide button → collapses to the character
- Click on Blue → reopens / expands it

That is the currently supported restore path for testers.

## If Blue does not respond

Common causes:

- Ollama is not reachable at `http://localhost:11434`
- `phi3:latest` is missing
- A local request to Ollama failed

Most of the time, the fix is:

```powershell
ollama pull phi3:latest
```

and making sure Ollama is running.

## Known limitations

- No full tray workflow yet
- This is still an alpha/beta build
- Packaging/distribution is still being stabilized

## Project goals

- Validate real desktop usage
- Gather feedback from trusted testers
- Stabilize packaging and distribution
- Improve UX and system-tray behavior

## Documentation

- [README in Spanish](./README.es.md)
- [README in English](./README.en.md)
- [Tester Quickstart (ES)](./TESTER-QUICKSTART.es.md)
- [Tester Quickstart (EN)](./TESTER-QUICKSTART.en.md)
- [Known Issues (ES)](./KNOWN-ISSUES.es.md)
- [Known Issues (EN)](./KNOWN-ISSUES.en.md)
- [Packaging Notes (ES)](./PACKAGING.es.md)
- [Packaging Notes (EN)](./PACKAGING.en.md)

## Repository structure

```text
Clippy.Core/    Core logic
Clippy/         Main desktop app
CubeKit.UI/     UI support library
```

## License

This project is licensed under the GNU GPLv3.

You are free to use, study, modify, and share this project.
If you distribute modified versions, you must also provide them under GPLv3 and keep the source available under the same license.
