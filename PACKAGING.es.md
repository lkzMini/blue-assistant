# Blue - Notas de empaquetado

## Formato de paquete recomendado

Para esta versión, usar:

- **MSIX**

No inviertas tiempo todavía en MSI ni en un instalador EXE clásico.

## Por qué MSIX

Blue hoy está estructurado como una aplicación empaquetada con WinUI / Windows App SDK, así que MSIX es el camino más natural para esta alpha/beta.

## Configuración de build

Recomendado en Visual Studio:

- Configuration: `Release`
- Platform: `x64`

## Flujo de empaquetado

Flujo sugerido:

1. Clean Solution
2. Restore NuGet Packages
3. Rebuild Solution
4. Crear el paquete MSIX desde Visual Studio

## Nota sobre firma

Para compartirlo fuera de tu propia máquina, probablemente necesites:

- un certificado de prueba para amigos/testers
o
- un certificado confiable para distribución más amplia

## Qué distribuir a los testers

Paquete mínimo:

- paquete MSIX de Blue
- instrucciones de instalación
- guía rápida para testers
- instrucciones de setup de Ollama

## Importante

Blue todavía **no** es standalone.

Los testers también necesitan:

- Ollama instalado localmente
- `phi3:latest` descargado localmente
- Windows 10/11
