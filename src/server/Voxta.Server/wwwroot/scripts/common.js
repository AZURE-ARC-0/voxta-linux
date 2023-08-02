// Integration with Voxta Desktop
const webView = window.chrome && window.chrome.webview;

if (webView) {
    document.getRootNode().addEventListener('keyup', function (e) {
        let msg;
        if (e.key === 'F1') {
            msg = { command: 'help' };
        } else if (e.key === 'F2') {
            msg = { command: 'switchToTerminal' };
        } else if (e.key === 'F11') {
            msg = { command: 'toggleFullScreen' };
        }
        
        if(msg)
        {
            webView.postMessage(JSON.stringify(msg));
        }
    });
}
