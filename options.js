chrome.storage.local.get(['downloadFolder'], function(result) {
	document.getElementById('downloadFolderInput').value = result.downloadFolder ?? '';
});

optionsForm.addEventListener('submit', () => {
	let downloadFolder = document.getElementById('downloadFolderInput').value;
	chrome.storage.local.set({ downloadFolder: downloadFolder }, function() {
		console.log('Set downloadFolder: ' + downloadFolder);
	});
});