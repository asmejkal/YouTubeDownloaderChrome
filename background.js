chrome.runtime.onInstalled.addListener(() => {
  chrome.action.disable();

  chrome.declarativeContent.onPageChanged.removeRules(undefined, () => {
    let exampleRule = {
      conditions: [
        new chrome.declarativeContent.PageStateMatcher({
          pageUrl: { hostSuffix: 'www.youtube.com', pathEquals: '/watch' },
        })
      ],
      actions: [new chrome.declarativeContent.ShowAction()],
    };
	
    let rules = [exampleRule];
    chrome.declarativeContent.onPageChanged.addRules(rules);
  });
});

chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.type == 'download') {
		download(sendResponse, request.startDownload);
	}
	else if (request.type == 'downloadSegment') {
		downloadSegment(sendResponse, request.startDownload);
	}
	
	return true;
});

function download(sendResponse, startDownload) {
	chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
		console.log('Downloading ' + tabs[0].url);
		
		chrome.storage.local.get(function(result) {
			chrome.runtime.sendNativeMessage('com.asmejkal.youtubedownloader', { 
					CommandType: 'Download', 
					Url: tabs[0].url, 
					YtDlpPath: result.ytDlpPath, 
					FfmpegPath: result.ffmpegPath,
					YtDlpArguments: result.ytDlpArguments,
					FileNameTemplate: result.fileNameTemplate, 
					EnableLogs: result.enableLogs },
				(response) => {
					handleResponse(response, sendResponse, startDownload);
				});
		});
	});
}

function downloadSegment(sendResponse, startDownload) {
	chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
		console.log('Downloading segment of ' + tabs[0].url);
		
		chrome.storage.local.get(function(result) {		
			chrome.runtime.sendNativeMessage('com.asmejkal.youtubedownloader', { 
					CommandType: 'DownloadSegment', 
					FromSeconds: result.from, 
					ToSeconds: result.to, 
					Url: tabs[0].url, 
					YtDlpPath: result.ytDlpPath, 
					FfmpegPath: result.ffmpegPath, 
					FfmpegSegmentArguments: result.ffmpegSegmentArguments,
					FileNameTemplate: result.fileNameTemplate, 
					SegmentFileNameTemplate: result.segmentFileNameTemplate, 
					EnableLogs: result.enableLogs },
				(response) => {
					handleResponse(response, sendResponse, startDownload);
				});
		});
	});
}

function handleResponse(response, sendResponse, startDownload) {
	console.log('Response: ' + response.code + ' Data: ' + JSON.stringify(response.data));
					
	if (response.code == 0) {
		if (startDownload) {
			chrome.downloads.download({ url: response.data.url }, (_) => {
				sendResponse({ url: response.data.url });
			});	
		}
		else {
			sendResponse({ url: response.data.url });
		}
	}
}