# Blue - Packaging Notes

## Recommended package format

For this version, use:

- **MSIX**

Do not spend time on MSI or a classic EXE installer yet.

## Why MSIX

Blue is currently structured as a packaged WinUI / Windows App SDK application, so MSIX is the most natural packaging path for this alpha/beta version.

## Build configuration

Recommended in Visual Studio:

- Configuration: `Release`
- Platform: `x64`

## Packaging flow

Suggested flow:

1. Clean Solution
2. Restore NuGet Packages
3. Rebuild Solution
4. Create the MSIX package from Visual Studio

## Signing note

To share outside your own machine, you may need:

- a test certificate for friends/testers
or
- a trusted certificate for broader distribution

## What to distribute to testers

Minimum package set:

- Blue MSIX package
- installation instructions
- tester quick start guide
- Ollama setup instructions

## Important

Blue is **not** standalone yet.

Testers still need:

- Ollama installed locally
- `phi3:latest` pulled locally
- Windows 10/11
