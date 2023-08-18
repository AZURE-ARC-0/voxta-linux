export class Notifications{
    constructor(elem) {
        this.elem = elem;
    }

    notify(message, type) {
        this.elem.innerText = message;
        this.elem.className = `text-${type}`;

        this.elem.style.opacity = '1';
        this.elem.style.transform = 'translateY(0)';

        if (type === 'success') {
            setTimeout(() => {
                this.elem.style.opacity = '0';
                this.elem.style.transform = 'translateY(50px)';
            }, 10000);
        }
    }
}