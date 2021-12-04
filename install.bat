reg add HKEY_LOCAL_MACHINE\SOFTWARE\Google\Chrome\NativeMessagingHosts\com.asmejkal.youtubedownloader /f /d "%~dp0native_manifest.json"
dotnet build -c Release "%~dp0/NativeHost/NativeHost.csproj"