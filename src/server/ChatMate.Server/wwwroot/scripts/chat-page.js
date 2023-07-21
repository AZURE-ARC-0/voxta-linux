import {ChatMateClient} from "/scripts/chatmate-client.js";
import {AudioVisualizer} from "/scripts/audio-visualizer.js";
import {Notifications} from "/scripts/notifications.js";

const canvas = document.getElementById('audioVisualizer');
const audioVisualizer = new AudioVisualizer(canvas);
const characterButtons = document.getElementById('characterButtons');
const messageBox = document.getElementById('message');
const notifications = new Notifications(document.getElementById('notification'));

let thinkingSpeechUrls = [];
const playThinkingSpeech = () => {
    if (thinkingSpeechUrls.length) {
        const audioUrl = thinkingSpeechUrls[Math.floor(Math.random() * thinkingSpeechUrls.length)];
        audioVisualizer.play(audioUrl, () => {
        }, () => {
        });
    }
}

const chatMateClient = new ChatMateClient('ws://127.0.0.1:5384/ws');

chatMateClient.addEventListener('onopen', (evt) => {
    notifications.notify('Connected', 'success');
});
chatMateClient.addEventListener('onclose', (evt) => {
    notifications.notify('Disconnected', 'danger');
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
    chatMateClient.startChat(evt.detail.character);
});
chatMateClient.addEventListener('ready', (evt) => {
    thinkingSpeechUrls = evt.detail.thinkingSpeechUrls || [];
    audioVisualizer.idle();
    canvas.style.opacity = '1';
});
chatMateClient.addEventListener('reply', (evt) => {
    messageBox.style.opacity = '1';
    messageBox.innerText = evt.detail.text;
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
    messageBox.innerText = evt.detail.text;
});
chatMateClient.addEventListener('speechRecognitionEnd', (evt) => {
    playThinkingSpeech();
    messageBox.innerText = evt.detail.text;
    audioVisualizer.think();
    chatMateClient.send(
        evt.detail.text,
        "Chatting with speech and no webcam.",
        ['happy', 'sad', 'angry', 'confused']
    );
});
chatMateClient.addEventListener('error', (evt) => {
    notifications.notify('Server error: ' + evt.detail.message, 'danger');
});

chatMateClient.connect();
