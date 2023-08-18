import {VoxtaClient} from "../../services/voxta-client.js";
import {AudioVisualizer} from "../../components/audio-visualizer.js";
import {Notifications} from "../../components/notifications.js";

const getId = id => document.getElementById(id);
const canvas = getId('audioVisualizer');
const splash = getId('splash');
const selectCharacterButton = getId('selectCharacterButton');
const characterButtons = getId('characterButtons');
const chatButtons = getId('chatButtons');
const messageBox = getId('message');
const promptBox = getId('promptBox');
const prompt = getId('prompt');

const audioVisualizer = new AudioVisualizer(canvas);
const notifications = new Notifications(getId('notification'));
const voxtaClient = new VoxtaClient('ws://127.0.0.1:5384/ws');

let selectedCharacter = {name: ''};
let selectedChatId = null;
let thinkingSpeechUrls = [];
const playThinkingSpeech = () => {
    if (!thinkingSpeechUrls.length) return;
    const audioUrl = thinkingSpeechUrls[Math.floor(Math.random() * thinkingSpeechUrls.length)];
    audioVisualizer.play(audioUrl, () => {
        // Start
    }, () => {
        audioVisualizer.think();
    });
}

const sendChatMessage = text => {
    playThinkingSpeech();
    audioVisualizer.think();
    prompt.disabled = true;
    voxtaClient.send(
        text,
        "Chatting with speech and no webcam.",
        ['normal', 'happy', 'intense_love', 'sad', 'angry', 'confused']
    );
}

const resetUI  = () => {
    messageBox.innerText = '';
    prompt.value = '';
    prompt.disabled = true;
    
    messageBox.classList.remove('voxta_show');
    promptBox.classList.remove('voxta_show');
    canvas.classList.remove('voxta_show');
    characterButtons.classList.remove('voxta_show');
    chatButtons.classList.remove('voxta_show');
    splash.classList.remove('voxta_show');

    audioVisualizer.idle();
}

const removeAllChildNodes = parent => {
    while (parent.firstChild) parent.removeChild(parent.firstChild);
};

const createElement = (parent, tagName, className, textContent) => {
    const el = document.createElement(tagName);
    el.className = className;
    el.textContent = textContent;
    parent.appendChild(el);
    return el;
};

const createButton = (parent, className, textContent, onClick) => {
    const button = createElement(parent, 'button', className, textContent);
    button.onclick = onClick;
    return button;
};

voxtaClient.addEventListener('onopen', () => {
    notifications.notify('Connected', 'success');
});

voxtaClient.addEventListener('onclose', () => {
    notifications.notify('Disconnected', 'danger');
    resetUI();
});

voxtaClient.addEventListener('onerror', evt => {
    notifications.notify('Error: ' + evt.detail.message, 'danger');
});

voxtaClient.addEventListener('welcome', evt => {
    getId('username').textContent = evt.detail.username;
    if(selectedChatId) {
        audioVisualizer.think();
        voxtaClient.resumeChat(selectedChatId);
    } else {
        splash.classList.add('voxta_show');
    }
});

voxtaClient.addEventListener('charactersListLoaded', evt => {
    removeAllChildNodes(characterButtons);
    if (evt.detail.characters.length === 0) {
        createElement(characterButtons, 'p', 'text-center text-muted', 'No characters found');
    }
    
    const charactersContainer = document.createElement('div');
    charactersContainer.className = 'charactersContainer';
    characterButtons.appendChild(charactersContainer);
    
    evt.detail.characters.forEach(character => {
        const characterDiv = document.createElement('div');
        characterDiv.className = 'character';
        characterDiv.onclick = () => {
            removeAllChildNodes(chatButtons);
            selectedCharacter = character;
            voxtaClient.loadChatsList(character.id);
            characterButtons.classList.remove('voxta_show');
            chatButtons.classList.add('voxta_show');
        };

        const avatarDiv = document.createElement('div');
        avatarDiv.className = 'avatar';
        avatarDiv.style.backgroundImage = `url('${character.avatarUrl}')`;
        characterDiv.appendChild(avatarDiv);

        const nameDiv = document.createElement('div');
        nameDiv.className = 'name';
        nameDiv.textContent = character.name;
        characterDiv.appendChild(nameDiv);

        charactersContainer.appendChild(characterDiv);
    });

    createButton(characterButtons, 'btn btn-secondary', 'Back', () => {
        characterButtons.classList.remove('voxta_show');
        splash.classList.add('voxta_show');
    });
});

voxtaClient.addEventListener('chatsListLoaded', evt => {
    removeAllChildNodes(chatButtons);
    createElement(chatButtons, 'h2', 'text-center', selectedCharacter.name);

    const avatarDiv = document.createElement('div');
    avatarDiv.className = 'avatar';
    avatarDiv.style.backgroundImage = `url('${selectedCharacter.avatarUrl}')`;
    avatarDiv.style.width = '100%';
    avatarDiv.style.height = '180px';
    chatButtons.appendChild(avatarDiv);
    
    if (evt.detail.chats.length === 0) {
        createElement(chatButtons, 'p', 'text-muted text-center', 'No chats found');
        createButton(chatButtons, 'btn btn-secondary', 'New chat', () => {
            audioVisualizer.think();
            voxtaClient.newChat({characterId: selectedCharacter.id});
            chatButtons.classList.remove('voxta_show');
        });
    } else {
        evt.detail.chats.forEach(chat => createButton(chatButtons, 'btn btn-secondary', 'Continue', () => {
            audioVisualizer.think();
            voxtaClient.resumeChat(chat.id);
            chatButtons.classList.remove('voxta_show');
        }));
        createButton(chatButtons, 'btn btn-secondary', 'Clear and reset chat', () => {
            audioVisualizer.think();
            voxtaClient.newChat({characterId: selectedCharacter.id});
            chatButtons.classList.remove('voxta_show');
        });
    }
    createButton(chatButtons, 'btn btn-secondary', 'Back', () => {
        chatButtons.classList.remove('voxta_show');
        splash.classList.add('voxta_show');
    });
});

voxtaClient.addEventListener('ready', evt => {
    selectedChatId = evt.detail.chatId;
    thinkingSpeechUrls = evt.detail.thinkingSpeechUrls || [];
    audioVisualizer.idle();
    canvas.classList.add('voxta_show');
    promptBox.classList.add('voxta_show');
    notifications.notify(`Chat started with text gen ${evt.detail.services.textGen.service?.serviceName}, text to speech ${evt.detail.services.speechGen.service?.serviceName ?? 'none'}, speech to text ${evt.detail.services.speechToText.service?.serviceName ?? 'none'}`, 'success');
});

voxtaClient.addEventListener('reply', evt => {
    messageBox.classList.add('voxta_show');
    messageBox.innerText = evt.detail.text;
    prompt.disabled = false;
});

voxtaClient.addEventListener('speech', evt => {
    audioVisualizer.play(
        evt.detail.url,
            duration => voxtaClient.speechPlaybackStart(duration),
        () => voxtaClient.speechPlaybackComplete()
    );
});

voxtaClient.addEventListener('action', evt => {
    audioVisualizer.setColor({
        'normal': 'rgb(215,234,231)',
        'happy': 'rgb(225,195,231)',
        'intense_love': '#e0678f',
        'sad': '#87b8de',
        'angry': '#fa2f2a',
        'confused': '#c27ec7',
    }[evt.detail.value] || '#778085')
});

voxtaClient.addEventListener('speechRecognitionStart', () => {
    audioVisualizer.stop();
    audioVisualizer.listen();
});

voxtaClient.addEventListener('speechRecognitionPartial', evt => {
    prompt.value = evt.detail.text;
});

voxtaClient.addEventListener('speechRecognitionEnd', evt => {
    if(evt.detail.text) {
        sendChatMessage(evt.detail.text);
    } else {
        audioVisualizer.idle();
    }
    prompt.value = evt.detail.text;
    prompt.disabled = true;
});

voxtaClient.addEventListener('error', evt => {
    notifications.notify('Server error: ' + evt.detail.message, 'danger');
    audioVisualizer.stop();
    audioVisualizer.idle();
    voxtaClient.speechPlaybackComplete();
});

voxtaClient.connect();

prompt.addEventListener('keydown', evt => {
    if (evt.key === 'Enter') {
        evt.preventDefault();
        if (!prompt.disabled) {
            sendChatMessage(prompt.value);
            prompt.value = '';
        }
    }
});

selectCharacterButton.addEventListener('click', () => {
    splash.classList.remove('voxta_show');
    characterButtons.classList.add('voxta_show');
    voxtaClient.loadCharactersList();
});
