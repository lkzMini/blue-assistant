# Blue - Inicio rápido para testers

## Requisitos

- Windows 10/11
- Blue instalado
- Ollama instalado localmente

## Paso 1 - Instalar Ollama

Instalá Ollama para Windows.

## Paso 2 - Descargar el modelo requerido

Abrí PowerShell y ejecutá:

```powershell
ollama pull phi3:latest
```

## Paso 3 - Verificar Ollama

Ejecutá:

```powershell
ollama list
```

El resultado esperado incluye:

```text
phi3:latest
```

Chequeos opcionales:

```powershell
ollama run phi3:latest "Say hello in one short sentence."
curl http://localhost:11434/api/tags
```

## Paso 4 - Abrir Blue

Ejecutá Blue normalmente.

## Cómo usarlo

- `Enter` -> enviar
- `Shift + Enter` -> salto de línea
- Arrastrá a Blue directamente para moverlo
- Click en Blue para expandir/colapsar
- El botón de ocultar colapsa Blue al personaje
- Click en Blue nuevamente para reabrir

## Si Blue no responde

Verificá:

1. ¿Ollama está instalado?
2. ¿Ollama está corriendo?
3. ¿`ollama list` muestra `phi3:latest`?

Si no, ejecutá:

```powershell
ollama pull phi3:latest
```
