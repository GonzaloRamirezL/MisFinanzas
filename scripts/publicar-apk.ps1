# Genera el instalador .apk firmado de Mis Finanzas.
# Uso (desde la carpeta del proyecto):
#   powershell -ExecutionPolicy Bypass -File scripts\publicar-apk.ps1
# Requiere haber creado la llave una vez (ver docs/publicar-apk.md).

param(
    [string]$Keystore = "$env:USERPROFILE\llaves\misfinanzas.keystore",
    [string]$Alias = "misfinanzas"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Keystore)) {
    Write-Error "No encuentro la llave en '$Keystore'. Créala primero (ver docs/publicar-apk.md, paso 1)."
}

$proyecto = Join-Path $PSScriptRoot "..\MisFinanzas.App\MisFinanzas.App.csproj"
$salida = Join-Path $PSScriptRoot "..\MisFinanzas.App\bin\Release\net10.0-android\publish"

# La contraseña se pide sin mostrarla y solo vive en una variable de entorno mientras dura la publicación.
$segura = Read-Host "Contraseña de la llave" -AsSecureString
$env:MISFINANZAS_CLAVE = [System.Net.NetworkCredential]::new("", $segura).Password

try {
    dotnet publish $proyecto -f net10.0-android -c Release `
        -p:AndroidKeyStore=true `
        -p:AndroidSigningKeyStore="$Keystore" `
        -p:AndroidSigningKeyAlias=$Alias `
        -p:AndroidSigningKeyPass=env:MISFINANZAS_CLAVE `
        -p:AndroidSigningStorePass=env:MISFINANZAS_CLAVE

    if ($LASTEXITCODE -ne 0) { throw "La publicación falló. Revisa los mensajes de arriba." }
}
finally {
    Remove-Item Env:MISFINANZAS_CLAVE -ErrorAction SilentlyContinue
}

$apk = Get-ChildItem $salida -Filter "*-Signed.apk" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $apk) { throw "No se encontró el .apk firmado en '$salida'." }

Write-Host ""
Write-Host "Listo: $($apk.FullName)" -ForegroundColor Green
Start-Process explorer.exe "/select,`"$($apk.FullName)`""
