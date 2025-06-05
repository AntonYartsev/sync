window.clipboardInterop = {
    copyText: async function(text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            try {
                await navigator.clipboard.writeText(text);
                return;
            } catch (err) {
                console.error('Clipboard API failed, falling back to execCommand', err);
            }
        }

        var textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.style.position = 'fixed';
        document.body.appendChild(textarea);
        textarea.focus();
        textarea.select();
        try { document.execCommand('copy'); } catch (err) {
            console.error('execCommand fallback failed', err);
        }
        document.body.removeChild(textarea);
    }
};
