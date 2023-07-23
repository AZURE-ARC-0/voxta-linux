import {VoxtaClient} from "/scripts/voxta-client.js";
import {AudioVisualizer} from "/scripts/audio-visualizer.js";
import {Notifications} from "/scripts/notifications.js";

const canvas = document.getElementById('audioVisualizer');
const audioVisualizer = new AudioVisualizer(canvas);
const characterButtons = document.getElementById('characterButtons');
const messageBox = document.getElementById('message');
const prompt = document.getElementById('prompt');
const notifications = new Notifications(document.getElementById('notification'));
const voxtaClient = new VoxtaClient('ws://127.0.0.1:5384/ws');

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
    voxtaClient.send(
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

voxtaClient.addEventListener('onopen', (evt) => {
    notifications.notify('Connected', 'success');
});
voxtaClient.addEventListener('onclose', (evt) => {
    notifications.notify('Disconnected', 'danger');
    reset();
});
voxtaClient.addEventListener('onerror', (evt) => {
    notifications.notify('Error: ' + evt.detail.message, 'danger');
});

voxtaClient.addEventListener('welcome', (evt) => {
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
            voxtaClient.loadCharacter(character.id);
            characterButtons.style.opacity = '0';
        };
        characterButtons.appendChild(button);
        characterButtons.style.opacity = '1';
    });
    setTimeout(() => {
        characterButtons.style.opacity = '1';
    }, 100);
});
voxtaClient.addEventListener('characterLoaded', (evt) => {
    character = evt.detail.character;
    voxtaClient.startChat(evt.detail.character);
});
voxtaClient.addEventListener('ready', (evt) => {
    thinkingSpeechUrls = evt.detail.thinkingSpeechUrls || [];
    audioVisualizer.idle();
    canvas.style.opacity = '1';
    prompt.style.opacity = '1';
});
voxtaClient.addEventListener('reply', (evt) => {
    messageBox.style.opacity = '1';
    messageBox.innerText = evt.detail.text;
    prompt.disabled = false;
});
voxtaClient.addEventListener('speech', (evt) => {
    audioVisualizer.play(
        evt.detail.url,
        (duration) => voxtaClient.speechPlaybackStart(duration),
        () => voxtaClient.speechPlaybackComplete()
    );
});
voxtaClient.addEventListener('action', (evt) => {
    // TODO: Change color (evt.detail.value);
});
voxtaClient.addEventListener('speechRecognitionStart', (evt) => {
    audioVisualizer.stop();
    audioVisualizer.listen();
});
voxtaClient.addEventListener('speechRecognitionPartial', (evt) => {
    prompt.value = evt.detail.text;
});
voxtaClient.addEventListener('speechRecognitionEnd', (evt) => {
    sendMessage(evt.detail.text);
    prompt.value = evt.detail.text;
    prompt.disabled = true;
});
voxtaClient.addEventListener('error', (evt) => {
    notifications.notify('Server error: ' + evt.detail.message, 'danger');
});

voxtaClient.connect();

prompt.addEventListener('keydown', (evt) => {
    if (evt.key === 'Enter') {
        evt.preventDefault();
        if(!prompt.disabled) {
            sendMessage(prompt.value);
            prompt.value = '';
        }
    }
});