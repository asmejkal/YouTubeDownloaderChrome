# Simple YouTube downloader Chrome extension
Runs youtube-dl and ffmpeg for you to download the current video or a specified segment.

Google bans extensions that download YouTube videos from the store. It also doesn't allow you to install extensions outside of the Chrome Web Store, so the extension needs to be installed manually.

## How to install
- Install these prerequisites: youtube-dl, ffmpeg, and dotnet 6.0 Windows SDK
- Clone or download the repository
- Go to the Chrome Extensions page at `chrome://extensions`
- Turn on Developer Mode
- Click Load Unpacked and select the cloned folder
- Run install.ps1 with PowerShell

## Uninstall
- Run uninstall.ps1 with PowerShell
- Go to the Chrome Extensions page at `chrome://extensions`
- Remove the extension

## Troubleshoot
1. Run `reg query HKEY_LOCAL_MACHINE\SOFTWARE\Google\Chrome\NativeMessagingHosts\com.asmejkal.youtubedownloader`
2. Verify that it returns a path to a `native_manifest.json` and that the `native_manifest.json` exists in that path
3. Verify that there is a `NativeHost\bin\Release\net6.0\NativeHost.exe` relative to the json
4. Verify that the `chrome-extension://` entry in `native_manifest.json` contains your extension's ID from the Chrome Extensions page