chrome.storage.local.get(['from', 'to', 'downloadFolder'], function(result) {
	document.getElementById('fromSpan').textContent = getTimespanString(result.from);
	document.getElementById('toSpan').textContent = getTimespanString(result.to);
	
	if (result.downloadFolder == undefined) {
		chrome.runtime.openOptionsPage();
	}
});

downloadBtn.addEventListener('click', async () => {
	let [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  
	chrome.storage.local.get(['downloadFolder'], function(result) {
		chrome.runtime.sendNativeMessage('com.asmejkal.youtubedownloader', 
			{ cmd: 'download', url: tab.url, downloadFolder: result.downloadFolder },
			(response) => {
				console.log('Response: ' + response.code + ' Data: ' + response.data);
			});
	});
});

setFromBtn.addEventListener('click', async () => {
	let [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  
	chrome.scripting.executeScript({
			target: { tabId: tab.id },
			function: getPlayerCurrentTime,
		},
		(results) => {
			let from = results[0].result;
			chrome.storage.local.set({from: from}, function() {
				console.log('Set from: ' + from);
			});
			
			document.getElementById('fromSpan').textContent = getTimespanString(from);
		});
});

setToBtn.addEventListener('click', async () => {
	let [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  
	chrome.scripting.executeScript({
			target: { tabId: tab.id },
			function: getPlayerCurrentTime,
		},
		(results) => {
			let to = results[0].result;
			chrome.storage.local.set({to: to}, function() {
				console.log('Set to: ' + to);
			});
			
			document.getElementById('toSpan').textContent = getTimespanString(to);
		});
});

downloadSegmentBtn.addEventListener('click', async () => {	
	let [tab] = await chrome.tabs.query({ active: true, currentWindow: true });

	chrome.storage.local.get(['from', 'to', 'downloadFolder'], function(result) {
		chrome.runtime.sendNativeMessage('com.asmejkal.youtubedownloader', 
			{ cmd: 'downloadSegment', from: result.from, to: result.to, url: tab.url, downloadFolder: result.downloadFolder },
			(response) => {
				console.log('Response: ' + response.code + ' Data: ' + response.data);
			});
	});
});

function getPlayerCurrentTime() {
	return document.querySelector('video').currentTime;
}

function getTimespanString(seconds) {
	if (seconds == undefined)
		return 'unset';
		
	var base = Math.floor(seconds);
	var fraction = seconds - base;
	
	var date = new Date(0);
	date.setSeconds(base, fraction * 1000); 
	return date.toISOString().substr(11, 12);
}