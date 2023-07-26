const isWebView = window.isWebView = window.chrome && window.chrome.webview;

// When the document is loaded, hide the logo
document.addEventListener('DOMContentLoaded', function () {
    if(isWebView) {
        const logo = document.getElementById('logo');
        logo.style.display = 'none';
    }
});

(function () {
    if (isWebView) {
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

window.initJsonEditor = (form, input, useDefaults) => {
    if(!window.JSONEditor) return;
    
    // create the editor
    const container = document.getElementById("jsoneditor")
    const options = {
        mainMenuBar: false,
        navigationBar: false,
        statusBar: false,
        colorPicker: false,
        language: 'en',
        mode: 'form',
        enableSort: false,
        enableTransform: false,
        onChange: () => {
            useDefaults.checked = false;
        }
    }
    const editor = new JSONEditor(container, options)

    editor.set(JSON.parse(input.value))

    form.addEventListener("submit", function(e) {
        e.preventDefault();

        const json = editor.get();
        input.value = JSON.stringify(json);
        this.submit();
    });
}