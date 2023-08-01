export class VoxtaClient extends EventTarget {
    wsUrl;
    socket;

    constructor(wsUrl) {
        super();
        this.wsUrl = wsUrl;
    }

    connect() {
        this.socket = new WebSocket(this.wsUrl);

        this.socket.onmessage = (event) => this.onEvent(event);

        this.socket.onopen = (event) => {
            this.dispatchEvent(new CustomEvent('onopen'));
        };

        this.socket.onclose = (event) => {
            this.dispatchEvent(new CustomEvent('onclose'));
            const that = this;
            setTimeout(() => {
                that.connect();
            }, 5000);
        };

        this.socket.onerror = (error) => {
            this.dispatchEvent(new CustomEvent('onerror', {detail: error}));
        };
    }

    onEvent(event) {
        console.log('ws', event.data)
        const data = JSON.parse(event.data);
        switch (data.$type) {
            case 'welcome':
                this.dispatchEvent(new CustomEvent('welcome', {
                    detail: {username: data.username}
                }));
                break;
            case 'charactersListLoaded':
                this.dispatchEvent(new CustomEvent('charactersListLoaded', {
                    detail: {characters: data.characters}
                }));
                break;
            case 'chatsListLoaded':
                this.dispatchEvent(new CustomEvent('chatsListLoaded', {
                    detail: {chats: data.chats}
                }));
                break;
            case 'characterLoaded':
                this.dispatchEvent(new CustomEvent('characterLoaded', {
                    detail: {character: data}
                }));
                break;
            case 'ready':
                this.dispatchEvent(new CustomEvent('ready', {
                    detail: {
                        chatId: data.chatId,
                        thinkingSpeechUrls: data.thinkingSpeechUrls
                    }
                }));
                break;
            case 'reply':
                this.dispatchEvent(new CustomEvent('reply', {
                    detail: {text: data.text}
                }));
                break;
            case 'speech':
                this.dispatchEvent(new CustomEvent('speech', {
                    detail: {url: data.url}
                }));
                break;
            case 'speechRecognitionStart':
                this.dispatchEvent(new CustomEvent('speechRecognitionStart'));
                break;
            case 'speechRecognitionPartial':
                this.dispatchEvent(new CustomEvent('speechRecognitionPartial', {
                    detail: {text: data.text}
                }));
                break;
            case 'speechRecognitionEnd':
                this.dispatchEvent(new CustomEvent('speechRecognitionEnd', {
                    detail: {text: data.text}
                }));
                break;
            case 'error':
                this.dispatchEvent(new CustomEvent('error', {
                    detail: {message: data.message}
                }));
                break;
            case 'action':
                this.dispatchEvent(new CustomEvent('action', {
                    detail: {value: data.value}
                }));
                break;
            default:
                console.error('unknown message type', data)
        }
    };

    loadCharactersList() {
        const msg = JSON.stringify({
            $type: "loadCharactersList"
        });
        this.socket.send(msg);
    }

    loadChatsList(characterId) {
        const msg = JSON.stringify({
            $type: "loadChatsList",
            characterId
        });
        this.socket.send(msg);
    }

    loadCharacter(characterId) {
        const msg = JSON.stringify({
            $type: "loadCharacter",
            characterId
        });
        this.socket.send(msg);
    }

    newChat(params) {
        const msg = JSON.stringify({
            $type: "newChat",
            characterId: params.characterId,
            clearExistingChats: params.clearExistingChats,
            useServerSpeechRecognition: true,
            acceptedAudioContentTypes: ["audio/x-wav", "audio/mpeg"],
        });
        this.socket.send(msg);
    }

    resumeChat(chatId) {
        const msg = JSON.stringify({
            $type: "resumeChat",
            chatId: chatId,
            useServerSpeechRecognition: true,
            acceptedAudioContentTypes: ["audio/x-wav", "audio/mpeg"],
        });
        this.socket.send(msg);
    }

    send(text, context, actions) {
        const msg = JSON.stringify({
            $type: "send",
            text: text,
            context: context,
            actions: actions
        });
        this.socket.send(msg);
    }

    speechPlaybackStart(duration) {
        this.socket.send(JSON.stringify({$type: "speechPlaybackStart", duration: duration}))
    }

    speechPlaybackComplete() {
        this.socket.send(JSON.stringify({$type: "speechPlaybackComplete"}))
    }
}