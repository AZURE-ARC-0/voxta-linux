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

Open <http://127.0.0.1:5384/settings> and select the backends you want to use. Note that you only need one Text Gen and one Text To Speech backend.

To get started, I recommend using NovelAI for both Text Gen and Text To Speech, and OpenAI for Animation Selection.

#### OpenAI

[OpenAI](https://openai.com/) has excellent reasoning abilities, and can be used to drive emotion and state animations. It does not allow for nsfw content however, and may refer to itself as an AI.

1. Go to [https://platform.openai.com/account/api-keys]
2. Create a new API key, and copy it to `OpenAI Key`

You can technically user another model, but only `gpt-3.5-turbo` is supported at the moment.

#### NovelAI

[NovelAI](https://novelai.net/) has both Text To Speech and Text Generation. It is not as coherent as OpenAI, but it can be used for nsfw content.

1. Login to [NovelAI](https://novelai.net/)
2. Open the developer console (Ctrl+Shift+I in Chrome) and type:

   ```js
   console.log(JSON.parse(localStorage.getItem('session')).auth_token)
   ```

3. Use copy the resulting string and paste it into `NovelAI Token`.

You can technically user another model, but only `clio-v1` is supported at the moment.

#### KoboldAI

KoboldAI can run LLMs locally. You can either use the [henk717 version](https://github.com/henk717/KoboldAI) or [koboldcpp](https://github.com/LostRuins/koboldcpp). You'll need to understand how it works and how to run custom models first.

If you use koboldcpp, the default url (localhost:5001) should work if the server is running.

#### Vosk

Vosk is used to do speech to text outside of Virt-A-Mate (because it's better, faster and uncensored).

When launching for the first time, a model will be downloaded. You can change it in `appsettings.json`; the list can be found here: <https://alphacephei.com/vosk/models>.

| Model                        | Hash                                                             |
| vosk-model-small-en-us-0.15  | 30f26242c4eb449f948e42cb302dd7a686cb29a3423a8367f99ff41780942498 |
| vosk-model-en-us-0.22-lgraph | d9838b4aaa82a75c4a17f5aca300eaca129aaab2a7cbf951bafbb500eb9c4334 |

#### Profile

Choose a name you'd like to be called, and describe how the AI should see you, and what hey know about you.

### Validate your settings

1. Open <http://127.0.0.1:5384/diagnostics>
2. Verify that all backends you plan on using are green.

### Testing the chat

This UI is mostly for testing, but it's great way to make sure things work.

1. Open <http://127.0.0.1:5384/chat>
2. Choose a character in the drop down on the top left
3. Talk, the character should be listening!

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

### Using a demo scenes

If you download a scene that uses ChatMate, and the previous steps are done correctly, you can simply open the scene and it should work. You can validate that the plugin is running by opening the plugin's custom UI (go in Edit mode, select the Person atom, go to Plugins, find ChatMate, click on Open Custom UI), and you should see "Connected". If you have a character selected, the State should be idle.

### Adding ChatMate to a scene

You can add `ChatMate.cslist` to a Person atom, and in the custom UI, select a character. Note that no animations will be played; to play animations, you can use something like Scripter and use On state changed and On animation changed to run animations in Timeline. You can also use the demo scenes as a starting point.

You may also want to enable lip sync in the Person atom, in Auto Behaviors, Lip Sync, Enabled.

Here are a few things to help make the character more alive:

- Build quality Timeline animations
- Use plugins like Glance for eye movements

### Using Scripter

The easiest way to integrate ChatMate with your scene logic is by using Scripter.

`index.js` (your scene logic):
```js
import { scripter, scene } from "vam-scripter";

import { initChatMate } from "./lib1.js";

const person = scene.getAtom("Person");
const timeline = person.getStorable("plugin#0_VamTimeline.AtomPlugin");
const overlays = scene.getAtom("Overlays").getStorable("plugin#0_VAMOverlaysPlugin.VAMOverlays");
var setSubtitles = overlays.getStringParam("Set and show subtitles");
var subtitlesColor = overlays.getColorParam("Subtitles Color");

const chatmate = initChatMate({
    atom: scripter.containingAtom.getStorable("plugin#1_ChatMate")
});

chatmate.onStateChanged = state => {
    console.log('state changed to ' + state);
    timeline.invokeAction("Play cm_state_" + state);

    if(state == 'thinking') {
        subtitlesColor.val = '#90CDE0';
        setSubtitles.val = chatmate.getLastUserMessage();
    } else if(state == 'preparing_speech') {
        subtitlesColor.val = '#E5BEBE';
        setSubtitles.val = chatmate.getLastCharacterMessage();
    }
};
    
chatmate.onAction = action => {
    console.log('action ' + action);
    timeline.invokeAction("Play cm_anim_" + action);
};
```

`lib1.js` (generic chatmate integration script):
```js
import { scene, scripter } from "vam-scripter";

const that = {};

let chatmateState;
let chatmateUserMessage;
let chatmateCharacterMessage;
let chatmateCurrentAction;

export function initChatMate(params) {
  const chatmate = params.atom;
  chatmateState = chatmate.getStringChooserParam("State");
  chatmateUserMessage = chatmate.getStringParam("LastUserMessage");
  chatmateCharacterMessage = chatmate.getStringParam("LastCharacterMessage");
  chatmateCurrentAction = chatmate.getStringParam("CurrentAction");

  scripter.declareAction("OnChatMateStateChanged", () => {
    try {
      if(that.onStateChanged != undefined) that.onStateChanged(chatmateState.val);
    } catch (e) {
      console.log(e);
    }
  });

  scripter.declareAction("OnChatMateAction", () => {
    try {
      if(that.onAction != undefined) that.onAction(chatmateCurrentAction.val);
    } catch (e) {
      console.log(e);
    }
  });

  that.getLastUserMessage = () => {
    return chatmateUserMessage.val;
  };

  that.getLastCharacterMessage = () => {
    return chatmateCharacterMessage.val;
  };

  return that;
}
```

## Built-in characters

- Melly: She's using OpenAI for text generation, and NovelAI for TTS. She's a bit more coherent than the other characters, but she's not as good at nsfw content. If you want to have productive conversations, she's the one to go to.
- Kate: She's using NovelAI for both text generation and TTS. She's not as coherent as Melly, but she's better at nsfw content. If you want to have fun and very adult conversations, she's the one to go to.
- Kally: She's using KoboldAI for text generation, and NovelAI for TTS. She's subservient and her persona will greatly depend on which backend you use.

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
  - I did not implement token counting for NovelAI, so past a certain point you will get errors.
- There is a small freeze when loading audio, and a preloader. I didn't find a way to get rid of it.

## Improvements

I'd love to get contributions on this project. Here are a few things that could be done:

- Find ways to fix the known issues
- Connect to other AI backends, STT and TTS
- Connect to a more powerful AI backend like Agnaistic (there is an API pull request that could be useful)

## License

[GNU GPLv3](LICENSE.md)
