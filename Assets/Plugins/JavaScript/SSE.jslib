mergeInto(LibraryManager.library, {
  StartPolling: function (url) {
	  url = Pointer_stringify(url);
      window.alert(url);
	  unityInstance.SendMessage('ServerConnection', 'OnServerSentEvent', url);
  },
});