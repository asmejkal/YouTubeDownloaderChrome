Remove-Item '.\native_manifest.json'
& reg delete HKEY_LOCAL_MACHINE\SOFTWARE\Google\Chrome\NativeMessagingHosts\com.asmejkal.youtubedownloader /f
& pause