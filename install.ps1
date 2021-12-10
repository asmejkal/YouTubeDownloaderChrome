$manifestExists = Test-Path '.\native-manifest.json'
if ($manifestExists -eq $false)
{
	Write-Host "Insert your extension ID: "
	$id = Read-Host
	$template = Get-Content '.\native_manifest.template.json'
	Set-Content .\native_manifest.json -Value $template.Replace('{EXTENSION_ID}', $id)
}

& reg add HKEY_LOCAL_MACHINE\SOFTWARE\Google\Chrome\NativeMessagingHosts\com.asmejkal.youtubedownloader /f /d "$PSScriptRoot/native_manifest.json"
& dotnet build -c Release "$PSScriptRoot/NativeHost/NativeHost.csproj"
& pause