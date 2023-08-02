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
        mode: useDefaults.checked ? 'view' : 'form',
        enableSort: false,
        enableTransform: false,
    }
    const editor = new JSONEditor(container, options)

    editor.set(JSON.parse(input.value))

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        const json = editor.get();
        input.value = JSON.stringify(json);
        this.submit();
    });

    useDefaults.addEventListener("change", function () {
        editor.setMode(useDefaults.checked ? 'view' : 'form');
    });
}