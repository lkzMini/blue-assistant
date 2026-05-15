# Blue - Problemas conocidos

## Estado actual

Blue está en testing alpha/beta.

## Limitaciones conocidas

### 1. Dependencia de Ollama
Blue requiere:
- Ollama instalado localmente
- `phi3:latest` descargado
- endpoint local `http://localhost:11434`

### 2. Tray icon
Blue actualmente usa un flujo de restauración por colapso al personaje en lugar de un tray icon real.

### 3. Empaquetado/firma
El empaquetado y la firma para distribución más amplia todavía están en proceso de formalización.

### 4. Warnings restantes
El proyecto todavía tiene warnings relacionados con:
- packaging/firma
- elementos de UI deprecated
- flujo de publish/profile
- limpieza más amplia de nullable

Actualmente no bloquean el uso normal.

## Trabajo futuro

- Tray real opcional
- Más limpieza de warnings
- Modernización de UI donde realmente valga la pena
- Posible limpieza interna de identificadores viejos de `Clippy`
