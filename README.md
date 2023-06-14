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

## Starting the server

You'll need .NET SDK 7. You can go to `ChatMate.Server` and run `dotnet run`.

## Testing the server

There is a test chat page at http://127.0.0.1:5384/chat

## Using in Virt-A-Mate

You need to open `whitelist_domains.json` to allow playing speech:

```json
{
  "sites": [
    // Add this line below (not this comment):
    "127.0.0.1:5384",
    // Leave the rest as is
  ]
}
```

You also need to go in the Security Settings and enable Web and Web Audio.

Add `ChatMate.cslist` to a Person atom.

You can open the plugin custom UI to see if the server is connected.

To drive animations, you'll need to use events, and something like `Scripter` to decide how to act on those events. You can also start from a demo scene.

## Known issues

- Speech recognition sometimes stops working out of nowhere
- The chat memory is only valid for the current connection
- There is no correct token counting, so after some time you will get errors
- We download the NovelAI audio to the disk, which adds a small delay. This is required because NAudio doesn't support loading a webm from a stream

## License

[GNU GPLv3](LICENSE.md)
