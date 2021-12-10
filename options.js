chrome.storage.local.get(function(result) {
	document.getElementById('ytDlpPathInput').value = result.ytDlpPath ?? '';
	document.getElementById('ffmpegPathInput').value = result.ffmpegPath ?? '';
	document.getElementById('enableLogsInput').checked = result.enableLogs ?? false;
	document.getElementById('ytDlpArgumentsInput').value = result.ytDlpArguments ?? '';
	document.getElementById('ffmpegSegmentArgumentsInput').value = result.ffmpegSegmentArguments ?? '';
	document.getElementById('fileNameTemplateInput').value = result.fileNameTemplate ?? '';
	document.getElementById('segmentFileNameTemplateInput').value = result.segmentFileNameTemplate ?? '';
});

optionsForm.addEventListener('submit', e => {
	let ytDlpPath = document.getElementById('ytDlpPathInput').value;
	let ffmpegPath = document.getElementById('ffmpegPathInput').value;
	let enableLogs = document.getElementById('enableLogsInput').checked;
	let ytDlpArguments = document.getElementById('ytDlpArgumentsInput').value;
	let ffmpegSegmentArguments = document.getElementById('ffmpegSegmentArgumentsInput').value;
	let fileNameTemplate = document.getElementById('fileNameTemplateInput').value;
	let segmentFileNameTemplate = document.getElementById('segmentFileNameTemplateInput').value;
	chrome.storage.local.set({ 
			ytDlpPath: ytDlpPath, 
			ffmpegPath: ffmpegPath, 
			enableLogs: enableLogs, 
			ytDlpArguments: ytDlpArguments, 
			ffmpegSegmentArguments: ffmpegSegmentArguments, 
			fileNameTemplate: fileNameTemplate,
			segmentFileNameTemplate: segmentFileNameTemplate 
		}, function() {
		console.log('Set ytDlpPath: ' + ytDlpPath);
		console.log('Set ffmpegPath: ' + ffmpegPath);
		console.log('Set enableLogs: ' + enableLogs);
		console.log('Set ytDlpArguments: ' + ytDlpArguments);
		console.log('Set ffmpegSegmentArguments: ' + ffmpegSegmentArguments);
		console.log('Set fileNameTemplate: ' + fileNameTemplate);
		console.log('Set segmentFileNameTemplate: ' + segmentFileNameTemplate);
		
		document.getElementById('saveBtn').innerText = 'Saved!';
		
		setTimeout(function() {
			document.getElementById('saveBtn').innerText = "Save";
		}, 3000);

	});
	
	e.preventDefault();
});