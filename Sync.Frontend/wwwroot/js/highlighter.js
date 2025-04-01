/**
 * Редактор кода с подсветкой синтаксиса (упрощенная версия)
 */

// Глобальная переменная для хранения элемента активной строки
let activeLineElement = null;

// Флаг, указывающий на то, что процесс обновления уже идет
let isUpdating = false;

// WebSocket connection
let ws = null;

/**
 * Обновляет подсветку кода в указанном контейнере
 */
function updateHighlightedEditor(containerId, code, language) {
    // Предотвращаем рекурсивные или одновременные вызовы
    if (isUpdating) return;
    isUpdating = true;
    
    try {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        // Очищаем контейнер полностью перед добавлением новых элементов
        while (container.firstChild) {
            container.removeChild(container.firstChild);
        }
        
        // Удаляем предыдущий элемент активной строки
        if (activeLineElement && activeLineElement.parentNode) {
            activeLineElement.parentNode.removeChild(activeLineElement);
            activeLineElement = null;
        }
        
        // Создаем элементы с точными стилями
        const pre = document.createElement('pre');
        pre.style.margin = '0';
        pre.style.padding = '0';
        pre.style.backgroundColor = 'transparent';
        pre.style.overflow = 'visible';
        pre.style.position = 'relative';
        
        const codeElement = document.createElement('code');
        codeElement.className = `language-${language || 'plaintext'}`;
        codeElement.textContent = code;
        codeElement.style.display = 'block';
        codeElement.style.padding = '16px';
        codeElement.style.margin = '0';
        codeElement.style.fontFamily = "'Consolas', 'Monaco', 'Courier New', monospace";
        codeElement.style.fontSize = '14px';
        codeElement.style.lineHeight = '1.6';
        codeElement.style.tabSize = '4';
        codeElement.style.whiteSpace = 'pre';
        codeElement.style.backgroundColor = 'transparent';
        
        // Сначала применяем подсветку, затем добавляем в DOM
        pre.appendChild(codeElement);
        hljs.highlightElement(codeElement);
        
        // Добавляем только после подсветки
        container.appendChild(pre);
        
        // Синхронизируем прокрутку и стили
        syncScrollPosition();
        syncEditorSizes();
        
        // Добавляем активную строку после того, как все элементы на месте
        setTimeout(highlightActiveLine, 0);
    } finally {
        isUpdating = false;
    }
}

/**
 * Синхронизирует прокрутку между редакторами
 */
function syncScrollPosition() {
    const textArea = document.querySelector('.code-editor');
    const highlightArea = document.getElementById('highlight-editor');
    
    if (!textArea || !highlightArea) return;
    
    // Синхронизируем позиции прокрутки
    highlightArea.scrollTop = textArea.scrollTop;
    highlightArea.scrollLeft = textArea.scrollLeft;
}

/**
 * Подсвечивает активную строку
 */
function highlightActiveLine() {
    const textArea = document.querySelector('.code-editor');
    const highlightArea = document.getElementById('highlight-editor');
    
    if (!textArea || !highlightArea) return;
    
    // Удаляем все активные строки перед созданием новой
    const existingActiveLines = document.querySelectorAll('.active-line');
    existingActiveLines.forEach(element => {
        if (element && element.parentNode) {
            element.parentNode.removeChild(element);
        }
    });
    
    // Получаем позицию курсора
    const cursorPos = textArea.selectionStart;
    const text = textArea.value;
    
    // Находим текущую строку
    let lineStart = text.lastIndexOf('\n', cursorPos - 1) + 1;
    if (lineStart < 0) lineStart = 0;
    
    // Вычисляем номер строки (начиная с 1)
    const lineNumber = text.substring(0, lineStart).split('\n').length;
    
    // Получаем вычисленные стили текстового поля
    const computedStyle = getComputedStyle(textArea);
    const paddingTop = parseFloat(computedStyle.paddingTop) || 0;
    const lineHeight = parseFloat(computedStyle.lineHeight) || 
                      (parseFloat(computedStyle.fontSize) * 1.2);
    
    // Создаем элемент активной строки
    const lineElement = document.createElement('div');
    lineElement.className = 'active-line';
    
    // Устанавливаем точное положение и размеры
    lineElement.style.top = ((lineNumber - 1) * lineHeight + paddingTop) + 'px';
    lineElement.style.height = lineHeight + 'px';
    
    // Добавляем в контейнер подсветки
    highlightArea.appendChild(lineElement);
    
    // Обновляем информацию о позиции курсора
    updateCursorInfo(lineNumber, cursorPos - lineStart + 1);
}

/**
 * Обновляет информацию о позиции курсора
 */
function updateCursorInfo(line, column) {
    let infoElement = document.querySelector('.cursor-info');
    
    if (!infoElement) {
        infoElement = document.createElement('div');
        infoElement.className = 'cursor-info';
        //document.body.appendChild(infoElement);
    }
    
    infoElement.textContent = `Line: ${line}, Column: ${column}`;
}

/**
 * Устанавливает обработчики событий
 */
function setupScrollSync() {
    const textArea = document.querySelector('.code-editor');
    const highlightArea = document.getElementById('highlight-editor');
    
    if (!textArea || !highlightArea) return;
    
    // Удаляем предыдущие обработчики, чтобы избежать дублирования
    textArea.removeEventListener('scroll', syncScrollPosition);
    
    // Добавляем обработчик прокрутки с флагом passive для лучшей производительности
    textArea.addEventListener('scroll', syncScrollPosition, { passive: true });
    
    // Начальная синхронизация
    syncScrollPosition();
    highlightActiveLine();
    
    console.log('Scroll sync setup completed');
}

/**
 * Синхронизирует размеры между textarea и слоем подсветки
 */
function syncEditorSizes() {
    const textArea = document.querySelector('.code-editor');
    const highlightArea = document.getElementById('highlight-editor');
    
    if (!textArea || !highlightArea) return;
    
    // Получаем вычисленные стили текстового поля
    const textAreaStyles = window.getComputedStyle(textArea);
    
    // Находим элементы для стилизации
    const preElement = highlightArea.querySelector('pre');
    const codeElement = highlightArea.querySelector('code');
    
    if (!preElement || !codeElement) return;
    
    // Копируем все стилевые свойства для точного отображения
    const stylesToCopy = [
        'fontSize', 'fontFamily', 'lineHeight', 
        'padding', 'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
        'margin', 'marginTop', 'marginRight', 'marginBottom', 'marginLeft',
        'tabSize', 'whiteSpace', 'wordWrap'
    ];
    
    // Применяем стили к элементу code
    stylesToCopy.forEach(style => {
        if (textAreaStyles[style]) {
            codeElement.style[style] = textAreaStyles[style];
        }
    });
    
    // Обеспечиваем правильную прокрутку
    highlightArea.scrollTop = textArea.scrollTop;
    highlightArea.scrollLeft = textArea.scrollLeft;
}

/**
 * Вспомогательная функция измерения ширины символа
 */
function getCharWidth(fontFamily, fontSize) {
    const span = document.createElement('span');
    span.style.fontFamily = fontFamily;
    span.style.fontSize = fontSize;
    span.style.position = 'absolute';
    span.style.visibility = 'hidden';
    span.textContent = 'X';
    document.body.appendChild(span);
    const width = span.getBoundingClientRect().width;
    document.body.removeChild(span);
    return width;
}

// WebSocket connection
function connectWebSocket(editorId, userId) {
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.hostname}:5001/ws/${editorId}/${userId}`;
    
    ws = new WebSocket(wsUrl);
    
    ws.onopen = () => {
        console.log('WebSocket connected');
        // Add connection status indicator
        const status = document.createElement('div');
        status.className = 'connection-status connected';
        status.textContent = 'Connected';
        document.body.appendChild(status);
    };
    
    ws.onmessage = (event) => {
        const message = JSON.parse(event.data);
        if (message.type === 'contentUpdate') {
            // Update the textarea content
            const textArea = document.querySelector('.code-editor');
            if (textArea) {
                textArea.value = message.content;
                // Update the highlighted editor
                updateHighlightedEditor();
            }
        }
    };
    
    ws.onerror = (error) => {
        console.error('WebSocket error:', error);
        // Update connection status
        const status = document.querySelector('.connection-status');
        if (status) {
            status.className = 'connection-status disconnected';
            status.textContent = 'Disconnected';
        }
    };
    
    ws.onclose = () => {
        console.log('WebSocket disconnected');
        // Update connection status
        const status = document.querySelector('.connection-status');
        if (status) {
            status.className = 'connection-status disconnected';
            status.textContent = 'Disconnected';
        }
        // Attempt to reconnect after a delay
        setTimeout(() => connectWebSocket(editorId, userId), 5000);
    };
}

// Update the textarea event listener to send updates
function setupTextAreaEvents(textArea, editorId, userId) {
    let debounceTimeout;
    
    textArea.addEventListener('input', () => {
        clearTimeout(debounceTimeout);
        debounceTimeout = setTimeout(() => {
            if (ws && ws.readyState === WebSocket.OPEN) {
                const content = textArea.value;
                ws.send(JSON.stringify({
                    Type: "contentUpdate",
                    Content: content
                }));
            }
        }, 300); // Debounce for 300ms
    });
}

// Update the initialization code
document.addEventListener('DOMContentLoaded', () => {
    const textArea = document.querySelector('.code-editor');
    if (textArea) {
        const editorId = textArea.getAttribute('data-editor-id');
        const userId = textArea.getAttribute('data-user-id');
        
        if (editorId && userId) {
            connectWebSocket(editorId, userId);
            setupTextAreaEvents(textArea, editorId, userId);
        }
    }
}); 