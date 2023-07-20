export class ChatMateClient extends EventTarget {
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
            this.dispatchEvent(new CustomEvent('onerror', error));
        };
    }

    onEvent(event) {
        console.log('ws', event.data)
        const data = JSON.parse(event.data);
        switch (data.$type) {
            case 'welcome':
                this.dispatchEvent(new CustomEvent('welcome', {
                    detail: {characters: data.characters}
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

    loadCharacter(characterId) {
        const msg = JSON.stringify({
            $type: "loadCharacter",
            characterId
        });
        this.socket.send(msg);
    }

    startChat(character) {
        const msg = JSON.stringify({
            ...character,
            $type: "startChat",
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