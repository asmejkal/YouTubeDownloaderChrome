import IconAnimator from './modules/iconAnimator.js';
var iconAnimator = new IconAnimator('img/loader', 'gif', 8, 'img/icon.png', 200, 20000);

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
	else if (request.type == 'setFrom') {
		setFrom(sendResponse);
	}
	else if (request.type == 'setTo') {
		setTo(sendResponse);
	}
	
	return true;
});

chrome.commands.onCommand.addListener((command) => {
	if (command == 'download') {
		download(null, true);
	}
	else if (command == 'downloadSegment') {
		downloadSegment(null, true);
	}
	else if (command == 'setFrom') {
		setFrom();
	}
	else if (command == 'setTo') {
		setTo();
	}
});

function download(sendResponse, startDownload) {
	let animationJobId = startDownload ? iconAnimator.start() : null;
	
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
					handleResponse(response, sendResponse, startDownload, animationJobId);
				});
		});
	});
}

function downloadSegment(sendResponse, startDownload) {
	let animationJobId = startDownload ? iconAnimator.start() : null;
	
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
					handleResponse(response, sendResponse, startDownload, animationJobId);
				});
		});
	});
}

function setFrom(sendResponse) {
	chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
		console.log('Getting current time of ' + tabs[0].url);
		
		chrome.scripting.executeScript({ target: { tabId: tabs[0].id }, function: getPlayerCurrentTime }, (results) => {
			let from = results[0].result;
			chrome.storage.local.set({ from: from }, function() {
				console.log('Set from: ' + from);
				
				if (sendResponse)
					sendResponse({ from: from });
			});
		});
	});
}

function setTo(sendResponse) {
	chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
		console.log('Getting current time of ' + tabs[0].url);
		
		chrome.scripting.executeScript({ target: { tabId: tabs[0].id }, function: getPlayerCurrentTime }, (results) => {
			let to = results[0].result;
			chrome.storage.local.set({ to: to }, function() {
				console.log('Set to: ' + to);
				
				if (sendResponse)
					sendResponse({ to: to });
			});
		});
	});
}

function handleResponse(response, sendResponse, startDownload, animationJobId) {
	console.log('Response: ' + response.code + ' Data: ' + JSON.stringify(response.data));
	
	if (response.code == 0) {
		if (startDownload) {
			chrome.downloads.download({ url: response.data.url }, (_) => {
				
				if (sendResponse)
					sendResponse({ url: response.data.url });
					
				iconAnimator.stop(animationJobId);
			});	
		}
		else {
			sendResponse({ url: response.data.url });
		}
	}
}

function getPlayerCurrentTime() {
	return document.querySelector('video').currentTime;
}