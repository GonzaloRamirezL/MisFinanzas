# Publicar un instalador .apk

Para instalar Mis Finanzas en otro teléfono sin Visual Studio ni Google Play.

> **Ojo:** el `.apk` que aparece en `bin\Debug` **no sirve** para otro teléfono. Es una versión de depuración que depende de Visual Studio para funcionar. Hay que generar uno en modo **Release** y firmado, como se explica aquí.

> En Visual Studio 2026 el asistente gráfico de "Distribución Ad Hoc" no aparece (es un problema conocido). Por eso este proceso usa la terminal, con un script que lo deja en un solo comando.

---

## Paso 1 · Crear tu llave de firma (una sola vez)

Android solo instala apps firmadas. La llave es un archivo `.keystore` con una contraseña.

En **PowerShell**:

```powershell
mkdir $env:USERPROFILE\llaves -Force
$keytool = (Get-ChildItem "C:\Program Files\Microsoft\jdk-*\bin\keytool.exe" | Select-Object -First 1).FullName
& $keytool -genkeypair -v -keystore "$env:USERPROFILE\llaves\misfinanzas.keystore" -alias misfinanzas -keyalg RSA -keysize 2048 -validity 10000
```

Te va a pedir una contraseña y algunos datos (nombre, ciudad, país `CL`). Puedes dejar en blanco los que no quieras completar.

> Si no encuentra `keytool.exe`, búscalo con `Get-ChildItem C:\ -Recurse -Filter keytool.exe -ErrorAction SilentlyContinue | Select -First 1`.

### ⚠️ Guarda bien la llave y la contraseña

- **Sin la misma llave no puedes actualizar la app.** Tendrías que desinstalarla y **perderías los datos** del teléfono.
- Respáldala fuera del PC (por ejemplo, en tu Drive personal) y anota la contraseña en tu gestor de contraseñas.
- **Nunca la subas a GitHub.** El `.gitignore` ya ignora `*.keystore` y `*.jks` por si acaso, y por eso se guarda fuera del proyecto, en `C:\Users\<tú>\llaves\`.

## Paso 2 · Generar el .apk

Desde la carpeta del proyecto (`C:\Users\gonza\source\MisFinanzas`):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publicar-apk.ps1
```

1. Te pide la contraseña de la llave (no se muestra al escribir).
2. Compila en modo Release y firma. La primera vez tarda unos minutos.
3. Al terminar, abre el Explorador con el archivo `com.cami.misfinanzas-Signed.apk` seleccionado.

<details>
<summary>Sin el script (comando completo)</summary>

```powershell
dotnet publish MisFinanzas.App\MisFinanzas.App.csproj -f net10.0-android -c Release `
  -p:AndroidKeyStore=true `
  -p:AndroidSigningKeyStore="$env:USERPROFILE\llaves\misfinanzas.keystore" `
  -p:AndroidSigningKeyAlias=misfinanzas `
  -p:AndroidSigningKeyPass=env:MISFINANZAS_CLAVE `
  -p:AndroidSigningStorePass=env:MISFINANZAS_CLAVE
```

(Antes define la contraseña con `$env:MISFINANZAS_CLAVE = "..."`.)
</details>

## Paso 3 · Instalar en el otro teléfono

1. Pasa el `.apk` al teléfono (Drive, WhatsApp a ti misma, cable USB…).
2. Ábrelo desde la app **Archivos** (o Drive).
3. Android pedirá permiso para **"Instalar apps desconocidas"** desde esa app → actívalo.
4. Si aparece **Play Protect** con "App no reconocida", toca **Más detalles → Instalar de todas formas**. Pasa con cualquier app que no viene de Google Play.

## Actualizar a una nueva versión

1. En `MisFinanzas.App.csproj`, sube el número de versión:
   ```xml
   <ApplicationDisplayVersion>1.1</ApplicationDisplayVersion>  <!-- lo que ve el usuario -->
   <ApplicationVersion>2</ApplicationVersion>                  <!-- siempre +1 -->
   ```
2. Ejecuta el script de nuevo, **con la misma llave**.
3. Instala el nuevo `.apk` encima del anterior: los datos se conservan.

## Qué cambió en el proyecto

- `MisFinanzas.App.csproj`:
  - En Release se genera directamente un **APK**. Por defecto .NET genera también un AAB, el formato de Google Play, que aquí no se necesita.
  - Se protegen `MisFinanzas.Core`, `MisFinanzas.Datos` y `SQLite-net` del **recorte de código** de Release. sqlite-net lee las propiedades de los modelos en tiempo de ejecución, y si el recorte las quitara, la base de datos fallaría solo en la versión instalada.
- `scripts/publicar-apk.ps1`: el script de publicación.
- `.gitignore`: ignora las llaves de firma.

## Importante sobre los datos

Cada teléfono tiene **su propia base de datos**: instalar la app en otro teléfono no copia tus movimientos. Para pasar los datos de un teléfono a otro haría falta la función de **respaldo y restauración** (pendiente).
