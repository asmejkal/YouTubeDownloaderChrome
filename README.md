# Simple YouTube downloader Chrome extension
Runs youtube-dl and ffmpeg for you to download the current video or a specified segment.

Google bans extensions that download YouTube videos from the store. It also doesn't allow you to install extensions outside of the Chrome Web Store, so the extension needs to be installed manually.

## How to install
- install these prerequisites and place them into PATH: youtube-dl, ffmpeg, and dotnet 5.0 Windows SDK
- clone the repository
- run install.bat
- go to [chrome://extensions](the chrome extensions page)
- turn on Developer Mode
- click Load Unpacked and select the cloned folder
- copy the loaded extension's ID and insert it into native_manifest.json