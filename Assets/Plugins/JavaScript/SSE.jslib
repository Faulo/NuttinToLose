var eventSource;

mergeInto(LibraryManager.library, {
	StartPolling: function (url) {
		url = UTF8ToString(url);
		
		eventSource = new EventSource(url);
		
		eventSource.addEventListener(
			"message",
			(eve) => {
				window.alert(eve.type + "\n" + eve.id + "\n" + eve.data);
				// SendMessage('ServerConnection', 'OnServerSentEvent', url);
			},
			false
		);
	},
});