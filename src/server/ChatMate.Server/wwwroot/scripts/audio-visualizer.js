export class AudioVisualizer {
    audioContext;
    analyser;
    canvas;
    canvasContext;
    currentSource;
    currentAudio;
    gradient;
    active = false;
    botState = 'idle';  // 'idle', 'listen', 'think', 'talk'
    easing = 0;
    constructor(canvas) {
        this.canvas = canvas;
        this.canvasContext = canvas.getContext('2d');
        this.drawFrame = this.drawFrame.bind(this);
        this.setColor('rgb(222,215,234)');
    }
    
    setColor(color) {
        const gradient = this.canvasContext.createLinearGradient(0, 0, this.canvas.width, 0);
        gradient.addColorStop(0, color.replace(')', ', 0)'));
        gradient.addColorStop(0.1, color);
        gradient.addColorStop(0.9, color);
        gradient.addColorStop(1, color.replace(')', ', 0)'));
        this.gradient = gradient;
    }
    
    tryInitialize() {
        if(this.audioContext) return;
        const audioContextClass = window.AudioContext || window.webkitAudioContext;
        if (audioContextClass) {
            this.audioContext = new (audioContextClass)();
            this.analyser = this.audioContext.createAnalyser();
        }
    }
    
    stop() {
        if (this.currentAudio) {
            this.currentAudio.pause();
            this.currentAudio = null;
        }
        if (this.currentSource) {
            this.currentSource.stop();
            this.currentSource = null;
        }
    }

    play(url, onStart, onComplete) {
        this.tryInitialize();
        if (this.audioContext) {
            this.playAnalyzerAudio(url, onStart, onComplete);
        } else {
            this.playStandardAudio(url, onStart, onComplete);
        }
    }

    playStandardAudio(url, onStart, onComplete) {
        if (this.currentAudio) {
            this.currentAudio.pause();
            this.currentAudio = null;
        }
        this.currentAudio = new Audio(url);
        this.currentAudio.addEventListener('playing', () => {
            this.talk();
            if(onStart) onStart(this.currentAudio.duration);
        });
        this.currentAudio.addEventListener('ended', () => {
            this.idle();
            if(onComplete) onComplete();
        });
        this.currentAudio.play();
    }

    playAnalyzerAudio(url, onStart, onComplete) {
        if (this.currentSource) {
            this.currentSource.stop();
            this.currentSource = null;
        }
        fetch(url)
            .then(response => response.arrayBuffer())
            .then(buffer => this.audioContext.decodeAudioData(buffer), error => this.handleError(error))
            .then(audioBuffer => {
                if(!audioBuffer) return;
                const source = this.audioContext.createBufferSource();
                source.buffer = audioBuffer;
                source.connect(this.analyser);
                this.analyser.connect(this.audioContext.destination);
                this.talk();
                if(onStart) onStart(audioBuffer.duration);
                source.onended = () => {
                    this.idle();
                    if(onComplete) onComplete();
                };
                source.start(0);
            }, error => this.handleError(error));
    }

    drawFrame() {
        this.canvasContext.fillStyle = 'rgb(34, 34, 34)';
        this.canvasContext.fillRect(0, 0, this.canvas.width, this.canvas.height);

        this.canvasContext.lineWidth = 0.25;
        this.canvasContext.strokeStyle = this.gradient;

        this.canvasContext.beginPath();

        let x = 0;

        if (this.botState === 'idle') {
            this.canvasContext.moveTo(0, this.canvas.height / 2);
            this.canvasContext.lineTo(this.canvas.width, this.canvas.height / 2);
        } else if (this.botState === 'listen') {
            // Pulse effect
            const amplitude = this.canvas.height / 64 * (1 + Math.sin(Date.now() / 200));
            this.canvasContext.moveTo(0, this.canvas.height / 2 - amplitude);
            this.canvasContext.lineTo(this.canvas.width, this.canvas.height / 2 - amplitude);
        } else if (this.botState === 'think') {
            if(this.easing < 1)
                this.easing = Math.min(1, this.easing + 0.006);
            const amplitude = (this.canvas.height / 16) * this.easing;
            const frequency = 3 * Math.PI / this.canvas.width;
            const offset = Date.now() / 10
            for (let x = 0; x < this.canvas.width; x++) {
                const y = amplitude * Math.sin(frequency * (x + offset)) + this.canvas.height / 2;
                this.canvasContext.lineTo(x, y);
            }
        } else if (this.botState === 'talk') {
            const sliceWidth = this.canvas.width * 1.0 / this.analyser.frequencyBinCount;
            const dataArray = new Uint8Array(this.analyser.frequencyBinCount);
            this.analyser.getByteTimeDomainData(dataArray);
            for (let i = 0; i < this.analyser.frequencyBinCount; i++) {
                const v = dataArray[i] / 128.0;
                const y = v * this.canvas.height / 2;

                if (i === 0) {
                    this.canvasContext.moveTo(x, y);
                } else {
                    this.canvasContext.lineTo(x, y);
                }

                x += sliceWidth;
            }
            this.canvasContext.lineTo(this.canvas.width, this.canvas.height / 2);
        }
        
        this.canvasContext.stroke();

        if(this.botState !== 'idle') {
            requestAnimationFrame(this.drawFrame);
        } else {
            this.active = false;
        }
    }

    idle() {
        this.botState = 'idle';
        this.easing = 0;
        this.startDraw();
    }

    listen() {
        this.botState = 'listen';
        this.easing = 0;
        this.startDraw();
    }

    think() {
        this.botState = 'think';
        this.easing = 0;
        this.startDraw();
    }

    talk() {
        this.botState = 'talk';
        this.startDraw();
    }
    
    startDraw() {
        if(this.active) return;
        this.active = true;
        this.drawFrame();
    }
    
    handleError(error) {
        console.error(error);
        this.idle();
    }
}