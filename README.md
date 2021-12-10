# Simple YouTube downloader Chrome extension
Runs youtube-dl and ffmpeg for you to download the current video or a specified segment.

Google bans extensions that download YouTube videos from the store. It also doesn't allow you to install extensions outside of the Chrome Web Store, so the extension needs to be installed manually.

## How to install
- Install these prerequisites: youtube-dl, ffmpeg, and dotnet 6.0 Windows SDK
- Clone or download the repository
- Go to the chrome extensions page at `chrome://extensions`
- Turn on Developer Mode
- Click Load Unpacked and select the cloned folder
- Run install.ps1 in PowerShell

## Uninstall
- Run uninstall.ps1 in PowerShell
- Go to the chrome extensions page at `chrome://extensions`
- Remove the extension