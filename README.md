# Frutería: Proyecto M7

**Erik Agreda (EA), 2º DAM**

Aplicación para gestionar una frutería: productos, stock, ventas e informes.
La aplicación de escritorio (C# WinForms) trabaja con una API REST (Python FastAPI).

GitHub: https://github.com/erikagreda215-del/ProyectoFruteria

## Antes de empezar (solo una vez)

1. Instala **Python**: https://www.python.org/downloads/. Durante la instalación, marca **«Add python.exe to PATH»**.
2. Instala el **SDK de .NET 10**: https://dotnet.microsoft.com/download. Si ya tienes Visual Studio con .NET 10, este paso no hace falta.

## Cómo usarlo

1. Descomprime el ZIP.
2. Haz doble clic en **`INICIAR.bat`**.

Se abren dos ventanas:

- **API Fruteria**: es el servidor. Déjala abierta mientras uses la aplicación.
- **Frutería**: es la aplicación. Desde el menú se abren el catálogo, el stock, las ventas y los informes.

La primera vez tarda 1 o 2 minutos, porque instala lo necesario y necesita internet. Las siguientes veces arranca en unos segundos.

## Si algo falla

| Problema | Solución |
|----------|----------|
| Dice que falta Python o .NET | Instálalo (ver «Antes de empezar») y vuelve a ejecutar `INICIAR.bat` |
| La aplicación dice «Sin conexión con la API» | La ventana «API Fruteria» se ha cerrado. Ejecuta `INICIAR.bat` otra vez. |

## Contenido

- `INICIAR.bat`: arranca todo.
- `FruteriaAPI/`: la API (Python).
- `FruteriaApp/`: la aplicación (C# WinForms); se abre en Visual Studio con `ProyectoFruteria.slnx`.
- `Documentacion/`: guía detallada, pruebas de funcionamiento y test de repaso.
