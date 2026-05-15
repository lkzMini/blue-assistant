# Blue - Quick Start for Testers

## Requirements

- Windows 10/11
- Blue installed
- Ollama installed locally

## Step 1 - Install Ollama

Install Ollama for Windows.

## Step 2 - Pull the required model

Open PowerShell and run:

```powershell
ollama pull phi3:latest
```

## Step 3 - Verify Ollama

Run:

```powershell
ollama list
```

Expected result includes:

```text
phi3:latest
```

Optional checks:

```powershell
ollama run phi3:latest "Say hello in one short sentence."
curl http://localhost:11434/api/tags
```

## Step 4 - Open Blue

Launch Blue normally.

## How to use

- `Enter` -> send
- `Shift + Enter` -> newline
- Drag Blue directly to move him
- Click Blue to expand/collapse
- Hide button collapses Blue to the character
- Click Blue again to reopen

## If Blue does not respond

Check:

1. Is Ollama installed?
2. Is Ollama running?
3. Does `ollama list` show `phi3:latest`?

If not, run:

```powershell
ollama pull phi3:latest
```
