export class AudioVisualizer {
    audioContext;
    analyser;
    canvas;
    canvasContext;
    currentSource;
    currentAudio;

    constructor(canvas) {
        this.canvas = canvas;
        this.canvasContext = canvas.getContext('2d');
        this.draw = this.draw.bind(this);
    }
    
    tryInitialize() {
        if(this.audioContext) return;
        const audioContextClass = window.AudioContext || window.webkitAudioContext;
        if (audioContextClass) {
            this.audioContext = new (audioContextClass)();
            this.analyser = this.audioContext.createAnalyser();
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
        if (onStart) this.currentAudio.addEventListener('playing', () => onStart(this.currentAudio.duration));
        if (onComplete) this.currentAudio.addEventListener('ended', onComplete);
        this.currentAudio.play();
    }

    playAnalyzerAudio(url, onStart, onComplete) {
        if (this.currentSource) {
            this.currentSource.stop();
            this.currentSource = null;
        }
        fetch(url)
            .then(response => response.arrayBuffer())
            .then(buffer => this.audioContext.decodeAudioData(buffer))
            .then(audioBuffer => {
                const source = this.audioContext.createBufferSource();
                source.buffer = audioBuffer;
                source.connect(this.analyser);
                this.analyser.connect(this.audioContext.destination);
                if (onStart) onStart(audioBuffer.duration);
                if (onComplete) source.onended = onComplete;
                source.start(0);
                this.draw();
            });
    }

    draw() {
        const dataArray = new Uint8Array(this.analyser.frequencyBinCount);
        this.analyser.getByteTimeDomainData(dataArray);

        this.canvasContext.fillStyle = 'rgb(34, 34, 34)';
        this.canvasContext.fillRect(0, 0, this.canvas.width, this.canvas.height);

        this.canvasContext.lineWidth = 1;
        const gradient = this.canvasContext.createLinearGradient(0, 0, this.canvas.width, 0);
        gradient.addColorStop(0, 'rgba(119,111,135, 0)');
        gradient.addColorStop(0.1, 'rgba(164,153,183, 1)');
        gradient.addColorStop(0.9, 'rgba(164,153,183, 1)');
        gradient.addColorStop(1, 'rgba(119,111,135, 0)');
        this.canvasContext.strokeStyle = gradient;

        this.canvasContext.beginPath();

        const sliceWidth = this.canvas.width * 1.0 / this.analyser.frequencyBinCount;
        let x = 0;

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
        this.canvasContext.stroke();

        requestAnimationFrame(this.draw);
    }
}