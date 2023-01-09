mergeInto(LibraryManager.library, {
	StartPolling : function (url) {
		url = UTF8ToString(url);
		
		window.SSE = {
			eventSource : "test",
			start : function(url) {
				this.eventSource = new EventSource(url);
					
				this.eventSource.addEventListener("error", this.onError, false);		
				this.eventSource.addEventListener("message", this.onMessage, false);
				
				this.eventSource.addEventListener("start", this.onMessage, false);
				this.eventSource.addEventListener("spawn", this.onMessage, false);
				this.eventSource.addEventListener("update", this.onMessage, false);
				this.eventSource.addEventListener("create-dig", this.onMessage, false);
				this.eventSource.addEventListener("remove-dig", this.onMessage, false);
				this.eventSource.addEventListener("world-state", this.onMessage, false);
			},
			onError : function(eve) {
				window.alert(eve.message);
			},
			onMessage : function(eve) {
				if (eve.data) {
					eve = {
						id : eve.id,
						type : eve.type,
						data : eve.data,
					};
					eve = JSON.stringify(eve);
					
					SendMessage('ServerConnection', 'OnServerSentEvent', eve);
				}
			},
		};
		
		window.SSE.start(url);
	},
});