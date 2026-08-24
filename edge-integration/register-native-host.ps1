$ErrorActionPreference = "Stop"

$hostName = "com.devspacengrokfoot.launcher"
$extensionId = "pomlpmhgnbemhbdmefpjpmccehfmcafl"
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$hostExe = Join-Path $projectRoot "DevspaceNgrokFoot.NativeHost.exe"
$manifestPath = Join-Path $PSScriptRoot "native-host-manifest.json"

if (-not (Test-Path $hostExe)) {
    throw "Native host not found: $hostExe. Run build-native-host.cmd first."
}

$manifest = [ordered]@{
    name = $hostName
    description = "Starts DevspaceNgrokFoot for selected ChatGPT pages"
    path = $hostExe
    type = "stdio"
    allowed_origins = @("chrome-extension://$extensionId/")
}

$json = $manifest | ConvertTo-Json -Depth 4
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($manifestPath, $json + [Environment]::NewLine, $utf8NoBom)

$registryPath = "HKCU\Software\Microsoft\Edge\NativeMessagingHosts\$hostName"
& reg.exe add $registryPath /ve /t REG_SZ /d $manifestPath /f | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Failed to register Edge native messaging host."
}

Write-Host "Registered native host: $hostName"
Write-Host "Extension ID: $extensionId"
Write-Host "Extension folder: $(Join-Path $projectRoot 'edge-extension')"
