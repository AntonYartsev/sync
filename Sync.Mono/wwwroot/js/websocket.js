// WebSocket connection handlers
(function() {
    // Check if the module is already initialized
    if (window.syncEditorModule) {
        return;
    }

    // Create a private scope for variables
    let editorWebSocket = null;
    let dotNetReference = null;

    // Assign methods to window object
    window.syncEditorModule = {
        initializeWebSocket: function(dotNetRef, url) {
            if (editorWebSocket && editorWebSocket.readyState === WebSocket.OPEN) {
                editorWebSocket.close();
            }

            dotNetReference = dotNetRef;
            editorWebSocket = new WebSocket(url);

            editorWebSocket.onopen = () => {
                if (dotNetReference) {
                    dotNetReference.invokeMethodAsync('OnWebSocketConnected');
                }
            };

            editorWebSocket.onmessage = (event) => {
                if (dotNetReference) {
                    dotNetReference.invokeMethodAsync('OnWebSocketMessage', event.data);
                }
            };

            editorWebSocket.onclose = (event) => {
                if (dotNetReference) {
                    dotNetReference.invokeMethodAsync('OnWebSocketClosed', event.code, event.reason);
                }
            };

            editorWebSocket.onerror = () => {
                if (dotNetReference) {
                    dotNetReference.invokeMethodAsync('OnWebSocketError');
                }
            };

            return true;
        },

        sendWebSocketMessage: function(message) {
            if (editorWebSocket && editorWebSocket.readyState === WebSocket.OPEN) {
                editorWebSocket.send(message);
                return true;
            }
            return false;
        },

        closeWebSocket: function() {
            if (editorWebSocket) {
                editorWebSocket.close();
                editorWebSocket = null;
            }
            
            if (dotNetReference) {
                dotNetReference = null;
            }
            
            return true;
        }
    };

    // Shorthand aliases for backward compatibility
    window.initializeWebSocket = window.syncEditorModule.initializeWebSocket;
    window.sendWebSocketMessage = window.syncEditorModule.sendWebSocketMessage;
    window.closeWebSocket = window.syncEditorModule.closeWebSocket;
})(); 