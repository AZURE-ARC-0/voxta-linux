import {VoxtaClient} from "/scripts/voxta-client.js";
import {AudioVisualizer} from "/scripts/audio-visualizer.js";
import {Notifications} from "/scripts/notifications.js";

const canvas = document.getElementById('audioVisualizer');
const audioVisualizer = new AudioVisualizer(canvas);
const splash = document.getElementById('splash');
const selectCharacterButton = document.getElementById('selectCharacterButton');
const characterButtons = document.getElementById('characterButtons');
const chatButtons = document.getElementById('chatButtons');
const messageBox = document.getElementById('message');
const promptBox = document.getElementById('promptBox');
const prompt = document.getElementById('prompt');
const notifications = new Notifications(document.getElementById('notification'));
const voxtaClient = new VoxtaClient('ws://127.0.0.1:5384/ws');

let selectedCharacter = { name: '', enableThinkingSpeech: false };
let thinkingSpeechUrls = [];
const playThinkingSpeech = () => {
    if (!selectedCharacter.enableThinkingSpeech) return;
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
        ['happy', 'intense_love', 'sad', 'angry', 'confused']
    );
}

const reset  = () => {
    messageBox.innerText = '';
    messageBox.classList.remove('voxta_show');
    prompt.value = '';
    prompt.disabled = true;
    promptBox.classList.remove('voxta_show');
    audioVisualizer.idle();
    canvas.classList.remove('voxta_show');
    characterButtons.classList.remove('voxta_show');
    chatButtons.classList.remove('voxta_show');
    splash.classList.remove('voxta_show');
}

voxtaClient.addEventListener('onopen', (evt) => {
    notifications.notify('Connected', 'success');
    splash.classList.add('voxta_show');
});
voxtaClient.addEventListener('onclose', (evt) => {
    notifications.notify('Disconnected', 'danger');
    reset();
});
voxtaClient.addEventListener('onerror', (evt) => {
    notifications.notify('Error: ' + evt.detail.message, 'danger');
});

voxtaClient.addEventListener('welcome', (evt) => {
    const username = document.getElementById('username');
    username.textContent = evt.detail.username;
    splash.classList.add('voxta_show');
});
voxtaClient.addEventListener('charactersListLoaded', (evt) => {
    // TODO: Split the UI logic
    while (characterButtons.firstChild) {
        characterButtons.removeChild(characterButtons.firstChild);
    }
    if(evt.detail.characters.length === 0) {
        const info = document.createElement('p');
        info.className = 'text-muted';
        info.textContent = 'No characters found';
        characterButtons.appendChild(info);
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
            while (chatButtons.firstChild) {
                chatButtons.removeChild(chatButtons.firstChild);
            }
            selectedCharacter = character;
            voxtaClient.loadChatsList(character.id);
            characterButtons.classList.remove('voxta_show');
            chatButtons.classList.add('voxta_show');
        };
        characterButtons.appendChild(button);
    });
    {
        const back = document.createElement('button');
        back.className = 'btn btn-secondary colspan';
        back.textContent = 'Back';
        back.onclick = () => {
            characterButtons.classList.remove('voxta_show');
            splash.classList.add('voxta_show');
        };
        characterButtons.appendChild(back);
    }
});
voxtaClient.addEventListener('chatsListLoaded', (evt) => {
    // TODO: Split the UI logic
    while (chatButtons.firstChild) {
        chatButtons.removeChild(chatButtons.firstChild);
    }

    const title = document.createElement('h2');
    title.className = 'text-center colspan';
    title.textContent = selectedCharacter.name;
    chatButtons.appendChild(title);
    
    if(evt.detail.chats.length === 0) {
        const info = document.createElement('p');
        info.className = 'text-muted text-center colspan';
        info.textContent = 'No chats found';
        chatButtons.appendChild(info);
    }
    
    evt.detail.chats.forEach(chat => {
        const button = document.createElement('button');
        button.className = 'btn btn-secondary';
        button.textContent = `Continue`;
        button.onclick = () => {
            voxtaClient.resumeChat(chat.id);
            chatButtons.classList.remove('voxta_show');
        };
        chatButtons.appendChild(button);
    });

    {
        const newChatButton = document.createElement('button');
        newChatButton.className = 'btn btn-secondary colspan';
        newChatButton.textContent = 'New chat';
        newChatButton.onclick = () => {
            voxtaClient.newChat(selectedCharacter.id);
            chatButtons.classList.remove('voxta_show');
        };
        chatButtons.appendChild(newChatButton);
    }

    {
        const back = document.createElement('button');
        back.className = 'btn btn-secondary colspan';
        back.textContent = 'Back';
        back.onclick = () => {
            chatButtons.classList.remove('voxta_show');
            splash.classList.add('voxta_show');
        };
        chatButtons.appendChild(back);
    }
});
voxtaClient.addEventListener('ready', (evt) => {
    thinkingSpeechUrls = evt.detail.thinkingSpeechUrls || [];
    audioVisualizer.idle();
    canvas.classList.add('voxta_show');
    promptBox.classList.add('voxta_show');
});
voxtaClient.addEventListener('reply', (evt) => {
    messageBox.classList.add('voxta_show');
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
    switch (evt.detail.value) {
        case 'happy':
            audioVisualizer.setColor('rgb(215,234,231)');
            break;
        case 'intense_love':
            // pink
            audioVisualizer.setColor('#e186a4');
            break;
        case 'sad':
            // blue
            audioVisualizer.setColor('#6d899f');
            break;
        case 'angry':
            // red
            audioVisualizer.setColor('#fa2f2a');
            break;
        case 'confused':
            // purple
            audioVisualizer.setColor('#a774ad');
            break;
        default:
            audioVisualizer.setColor('#afbcc7');
            break;
    }
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
    audioVisualizer.stop();
    audioVisualizer.idle();
    voxtaClient.speechPlaybackComplete();
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

selectCharacterButton.addEventListener('click', () => {
    splash.classList.remove('voxta_show');
    characterButtons.classList.add('voxta_show');
    voxtaClient.loadCharactersList();
});
