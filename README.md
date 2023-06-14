# Virt-A-Mate Artificial Intelligence

This is an early prototype of using Large Language Models, Text To Speech and Speech To Text inside Virt-A-Mate.

## Generating your keys

Open the `ChatMate.Server/appsettings.Local.json.template` and copy it to `ChatMate.Server/appsettings.Local.json`.

### OpenAI

OpenAI has Text To Speech and Animation Selection.

Get the Organization ID and create an API Key in the OpenAI dashboard.

### NovelAI

NovelAI has both Text To Speech and Text Generation.

To get the key, login to Novel AI, open the developer console and type:

```js
console.log(JSON.parse(localStorage.getItem('session')).auth_token)
```

## License

[GNU GPLv3](LICENSE.md)
