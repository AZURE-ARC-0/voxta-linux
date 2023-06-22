# Virt-A-Mate ChatMate

This is an early prototype of a chat companion using Large Language Models, Text To Speech and Speech To Text inside Virt-A-Mate.

You'll need:

- The ChatMate Server (`AcidBubbles.ChatMate.Server.VERSION.zip`)
- The ChatMate Virt-A-Mate Plugin (`AcidBubbles.ChatMate.VERSION.var`)
- A [NovelAI](https://novelai.net/) account (I think you can use free for a little while, supports nsfw, used for AI and TTS)
- An [OpenAI](https://openai.com/) platform account (you'll need to enter a phone number and it will avoid nsfw, used for AI and animation selection)

## How to install the server

Download the latest `AcidBubbles.ChatMate.Server.VERSION.zip` release from the [releases page](https://github.com/acidbubbles/vam-chatmate/releases/). Extract it somewhere, and run the server by double-clicking on the .exe, or running it in the terminal (preferred). You'll need to configure it first.

### Entering your API keys

Open the `appsettings.Local.json.template` and copy it to `appsettings.Local.json`.

#### OpenAI

[OpenAI](https://openai.com/) has excellent reasoning abilities, and can be used to drive emotion and state animations. It does not allow for nsfw content however, and may refer to itself as an AI.

1. Go to [https://platform.openai.com/account/api-keys]
2. Create a new API key, and copy it to `appsettings.Local.json` in `ChatMate.Services:OpenAI:ApiKey`
3. Go to Settings to find your `Organization ID`, and add it in  in `ChatMate.Services:OpenAI:OrganizationId`

#### NovelAI

[NovelAI](https://novelai.net/) has both Text To Speech and Text Generation. It is not as coherent as OpenAI, but it can be used for nsfw content.

1. Login to [NovelAI](https://novelai.net/)
2. Open the developer console (Ctrl+Shift+I in Chrome) and type:

   ```js
   console.log(JSON.parse(localStorage.getItem('session')).auth_token)
   ```

3. Use copy the resulting string and paste it into `appsettings.Local.json` under `ChatMate.Services:NovelAI:Token`.

### Starting the server

Normally you can simply double-click on the executable, and it should work. If it doesn't, you can see the error message by running it from a terminal.

## Using the browser chat interface

This UI is mostly for testing, but it's great way to make sure things work.

1. Open <http://127.0.0.1:5384/chat>
2. Choose a character in the drop down on the top left
3. Type something in the text box and press enter or use the microphone button to speak

## How to install the plugin in Virt-A-Mate

### Permissions

You'll need to allow a few things o the plugin can run.

Before launching vam, open `whitelist_domains.json`  in the root of Virt-A-Mate's folder. This is required to allow playing speech from the ChatMate server:

```json
{
  "sites": [
    // Add this line below (not this comment):
    "127.0.0.1:5384",
    // Leave the rest as is
  ]
}
```

You can now launch Virt-A-Mate, open the main menu and go to User Preferences. From there, go to the Security tab. You need to enable:

- Enable Plugins (for ChatMate itself)
  - Allow Plugins Network Access (To allow ChatMate to connect to the server)
- Enable Web Images and Audio (To allow playing audio from the server)
- Enable Package Downloader (optional, if you want to use the demo scene dependencies)
- Enable Hub (optional, if you want to auto-download demo scene dependencies)

### Using the demo scenes

If you download a scene that uses ChatMate, and the previous steps are done correctly, you can simply open the scene and it should work. You can validate that the plugin is running by opening the plugin's custom UI (go in Edit mode, select the Person atom, go to Plugins, find ChatMate, click on Open Custom UI), and you should see "Connected". If you have a Bot selected, the State should be idle.

### Adding ChatMate to a scene

You can add `ChatMate.cslist` to a Person atom, and in the custom UI, select a Bot. Note that no animations will be played; to play animations, you can use something like Scripter and use On state changed and On animation changed to run animations in Timeline. You can also use the demo scenes as a starting point.

You may also want to enable lip sync in the Person atom, in Auto Behaviors, Lip Sync, Enabled.

Here are a few things to help make the character more alive:

- Build quality Timeline animations
- Use plugins like Glance for eye movements

## Built-in bots

- Melly: She's using OpenAI for text generation, and NovelAI for TTS. She's a bit more coherent than the other bots, but she's not as good at nsfw content. If you want to have productive conversations, she's the one to go to.
- Kate: She's using NovelAI for both text generation and TTS. She's not as coherent as Melly, but she's better at nsfw content. If you want to have fun and very adult conversations, she's the one to go to.

## How to build from source

You'll need [.NET SDK 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).

- In a terminal, go to `ChatMate.Server`
- Execute `dotnet run`

## Known issues

- Speech recognition issues
  - Speech recognition sometimes stops working because "it's already in progress". You need to restart Virt-A-Mate.
  - Speech recognition will actually censor stuff you say.
  - Speech recognition will not forward exclamation marks, question marks, etc. which means that the AI may not fully understand the tone of what you say.
- Chat
  - The chat memory is only valid for the current connection. Once you exit or reconnect, the memory is lost.
- NovelAI
  - I did not implement token counting, so past a certain point you will get errors.
- There is a small freeze when loading audio, and a preloader. I didn't find a way to get rid of it.

## Improvements

I'd love to get contributions on this project. Here are a few things that could be done:

- Find ways to fix the known issues
- Connect to other AI backends, STT and TTS
- Connect to a more powerful AI backend like Agnaistic (there is an API pull request that could be useful)

## License

[GNU GPLv3](LICENSE.md)
