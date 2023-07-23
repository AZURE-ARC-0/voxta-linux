# Voxta

This is an early prototype of a virtual chat companion backend using Large Language Models, Text To Speech and Speech To Text.

## How to install the server

Download the latest `Voxta.Server.VERSION.zip` release from the [releases page](https://github.com/voxta-ai/voxta-server/releases/). Extract it somewhere, and run the server by double-clicking on the .exe, or running it in the terminal (preferred). You'll need to configure it first.

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

#### Text Generation Web UI

Similar to KoboldAI.

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

### Your first chat

1. Open <http://127.0.0.1:5384/chat>
2. Choose a character
3. Talk, the character should be listening!

## Built-in characters

- Melly: She's using OpenAI for text generation, and NovelAI for TTS. She's a bit more coherent than the other characters, but she's not as good at nsfw content. If you want to have productive conversations, she's the one to go to.
- Kate: She's using NovelAI for both text generation and TTS. She's not as coherent as Melly, but she's better at nsfw content. If you want to have fun and very adult conversations, she's the one to go to.
- Kally: She's using KoboldAI for text generation, and NovelAI for TTS. She's subservient and her persona will greatly depend on which backend you use.

## How to build from source

You'll need [.NET SDK 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).

- In a terminal, go to `src/server/Voxta.Server`
- Execute `dotnet run`

## License

[GNU GPLv3](LICENSE.md)
