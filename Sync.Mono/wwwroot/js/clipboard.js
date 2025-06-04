window.clipboardInterop = {
    copyText: function(text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text);
        } else {
            var textarea = document.createElement('textarea');
            textarea.value = text;
            textarea.style.position = 'fixed';
            document.body.appendChild(textarea);
            textarea.focus();
            textarea.select();
            try { document.execCommand('copy'); } catch (err) {}
            document.body.removeChild(textarea);
        }
    }
};
