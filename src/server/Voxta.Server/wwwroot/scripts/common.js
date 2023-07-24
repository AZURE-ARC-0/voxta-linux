(function () {
    if (window.chrome && window.chrome.webview) {
        document.getRootNode().addEventListener('keyup', function (e) {
            if (e.key === 'F2') {
                window.chrome.webview.postMessage(
                    JSON.stringify({
                        command: 'switchToTerminal'
                    })
                );
            }
        });
    }
})();
