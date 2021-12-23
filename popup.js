chrome.storage.local.get(['from', 'to', 'ytDlpPath', 'ffmpegPath'], function(result) {
	document.getElementById('fromSpan').textContent = getTimespanString(result.from);
	document.getElementById('toSpan').textContent = getTimespanString(result.to);
	
	if (!result.ytDlpPath || !result.ffmpegPath) {
		chrome.runtime.openOptionsPage();
	}
});

downloadBtn.addEventListener('click', () => {
	chrome.runtime.sendMessage({ type: 'download', startDownload: true });
	window.close();
});

downloadAsBtn.addEventListener('click', () => {
	document.body.classList.toggle('loading');
	chrome.runtime.sendMessage({ type: 'download', startDownload: false }, (response) => {
		console.log('Starting download as... of ' + response.url);
		
		chrome.downloads.download({ url: response.url, saveAs: true }, (_) => {
			document.body.classList.toggle('loading');
		});
	});
});

downloadSegmentBtn.addEventListener('click', () => {	
	chrome.runtime.sendMessage({ type: 'downloadSegment', startDownload: true });
	window.close();
});

setFromBtn.addEventListener('click', () => {
	chrome.runtime.sendMessage({ type: 'setFrom' }, (response) => {		
		document.getElementById('fromSpan').textContent = getTimespanString(response.from);
	});
});

setToBtn.addEventListener('click', async () => {
	chrome.runtime.sendMessage({ type: 'setTo' }, (response) => {		
		document.getElementById('toSpan').textContent = getTimespanString(response.to);
	});
});

function getTimespanString(seconds) {
	if (seconds == undefined)
		return 'unset';
		
	var base = Math.floor(seconds);
	var fraction = seconds - base;
	
	var date = new Date(0);
	date.setSeconds(base, fraction * 1000); 
	return date.toISOString().substr(11, 12);
}