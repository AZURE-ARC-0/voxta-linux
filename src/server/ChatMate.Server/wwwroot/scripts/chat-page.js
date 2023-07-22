import {ChatMateClient} from "/scripts/chatmate-client.js";
import {AudioVisualizer} from "/scripts/audio-visualizer.js";
import {Notifications} from "/scripts/notifications.js";

const canvas = document.getElementById('audioVisualizer');
const audioVisualizer = new AudioVisualizer(canvas);
const characterButtons = document.getElementById('characterButtons');
const messageBox = document.getElementById('message');
const prompt = document.getElementById('prompt');
const notifications = new Notifications(document.getElementById('notification'));
const chatMateClient = new ChatMateClient('ws://127.0.0.1:5384/ws');

let character = { name: '', enableThinkingSpeech: false };
let thinkingSpeechUrls = [];
const playThinkingSpeech = () => {
    if (!character.enableThinkingSpeech) return;
    if (thinkingSpeechUrls.length) {
        const audioUrl = thinkingSpeechUrls[Math.floor(Math.random() * thinkingSpeechUrls.length)];
        audioVisualizer.play(audioUrl, () => {
        }, () => {
        });
    }
}

const sendMessage = (text) => {
    playThinkingSpeech();
    audioVisualizer.think();
    prompt.disabled = true;
    chatMateClient.send(
        text,
        "Chatting with speech and no webcam.",
        ['happy', 'sad', 'angry', 'confused']
    );
}

const reset  = () => {
    messageBox.innerText = '';
    messageBox.style.opacity = '0';
    prompt.value = '';
    prompt.disabled = true;
    prompt.style.opacity = '0';
    audioVisualizer.idle();
    canvas.style.opacity = '0';
}

chatMateClient.addEventListener('onopen', (evt) => {
    notifications.notify('Connected', 'success');
});
chatMateClient.addEventListener('onclose', (evt) => {
    notifications.notify('Disconnected', 'danger');
    reset();
});
chatMateClient.addEventListener('onerror', (evt) => {
    notifications.notify('Error: ' + evt.detail.message, 'danger');
});

chatMateClient.addEventListener('welcome', (evt) => {
    while (characterButtons.firstChild) {
        characterButtons.removeChild(characterButtons.firstChild);
    }
    evt.detail.characters.forEach(character => {
        const button = document.createElement('button');
        button.className = 'btn btn-secondary';
        const charName = document.createElement('b');
        charName.textContent = character.name;
        button.appendChild(charName);
        const charDesc = document.createElement('div');
        charDesc.className = 'small';
        charDesc.textContent = character.description;
        button.appendChild(charDesc);
        button.onclick = () => {
            chatMateClient.loadCharacter(character.id);
            characterButtons.style.opacity = '0';
        };
        characterButtons.appendChild(button);
        characterButtons.style.opacity = '1';
    });
    setTimeout(() => {
        characterButtons.style.opacity = '1';
    }, 100);
});
chatMateClient.addEventListener('characterLoaded', (evt) => {
    character = evt.detail.character;
    chatMateClient.startChat(evt.detail.character);
});
chatMateClient.addEventListener('ready', (evt) => {
    thinkingSpeechUrls = evt.detail.thinkingSpeechUrls || [];
    audioVisualizer.idle();
    canvas.style.opacity = '1';
    prompt.style.opacity = '1';
});
chatMateClient.addEventListener('reply', (evt) => {
    messageBox.style.opacity = '1';
    messageBox.innerText = evt.detail.text;
    prompt.disabled = false;
});
chatMateClient.addEventListener('speech', (evt) => {
    audioVisualizer.play(
        evt.detail.url,
        (duration) => chatMateClient.speechPlaybackStart(duration),
        () => chatMateClient.speechPlaybackComplete()
    );
});
chatMateClient.addEventListener('action', (evt) => {
    // TODO: Change color (evt.detail.value);
});
chatMateClient.addEventListener('speechRecognitionStart', (evt) => {
    audioVisualizer.stop();
    audioVisualizer.listen();
});
chatMateClient.addEventListener('speechRecognitionPartial', (evt) => {
    prompt.value = evt.detail.text;
});
chatMateClient.addEventListener('speechRecognitionEnd', (evt) => {
    sendMessage(evt.detail.text);
    prompt.value = evt.detail.text;
    prompt.disabled = true;
});
chatMateClient.addEventListener('error', (evt) => {
    notifications.notify('Server error: ' + evt.detail.message, 'danger');
});

chatMateClient.connect();

prompt.addEventListener('keydown', (evt) => {
    if (evt.key === 'Enter') {
        evt.preventDefault();
        if(!prompt.disabled) {
            sendMessage(prompt.value);
            prompt.value = '';
        }
    }
});