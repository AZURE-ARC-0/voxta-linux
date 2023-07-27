const webView = window.chrome && window.chrome.webview;

if (webView) {
    document.addEventListener('DOMContentLoaded', function () {
        const logo = document.getElementById('logo');
        if(logo) {
            logo.style.display = 'none';
        }
    });

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

window.initJsonEditor = (form, input, useDefaults) => {
    const container = document.getElementById("jsoneditor")
    if(!container || !input || !form || !useDefaults) {
        console.error("Missing form components");
    }
    
    if (!window.JSONEditor) {
        console.error("Missing json editor library");
        container.innerText = `<div class=text-danger>Could not load the editor.</div>`;
        return;
    }

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

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        const json = editor.get();
        input.value = JSON.stringify(json);
        this.submit();
    });
}