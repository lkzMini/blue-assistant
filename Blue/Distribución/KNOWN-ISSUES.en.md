# Blue - Known Issues

## Current status

Blue is in alpha/beta testing.

## Known limitations

### 1. Ollama dependency
Blue requires:
- Ollama installed locally
- `phi3:latest` downloaded
- local endpoint `http://localhost:11434`

### 2. Tray icon
Blue currently uses a collapse-to-character restore flow instead of a real tray icon workflow.

### 3. Packaging/signing
Packaging/signing for broader distribution is still being formalized.

### 4. Remaining warnings
The project still has warnings related to:
- packaging/signing
- deprecated UI elements
- publish/profile workflow
- broader nullable cleanup

These do not currently block normal use.

## Future work

- Optional real tray behavior
- Further warning cleanup
- UI modernization where it is truly worth it
- Possible internal cleanup of old `Clippy` identifiers
