/**
 * Редактор кода с подсветкой синтаксиса (упрощенная версия)
 */

// Глобальная переменная для хранения элемента активной строки
let activeLineElement = null;

// Флаг, указывающий на то, что процесс обновления уже идет
let isUpdating = false;

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

// Устанавливаем обработчики событий при загрузке DOM
document.addEventListener('DOMContentLoaded', function() {
    const textArea = document.querySelector('.code-editor');
    const highlightArea = document.getElementById('highlight-editor');
    
    if (!textArea || !highlightArea) return;
    
    // Очищаем все предыдущие активные строки при загрузке страницы
    const existingActiveLines = document.querySelectorAll('.active-line');
    existingActiveLines.forEach(element => {
        if (element && element.parentNode) {
            element.parentNode.removeChild(element);
        }
    });
    
    // Удаляем предыдущие обработчики, если они есть
    textArea.removeEventListener('scroll', syncScrollPosition);
    
    // Синхронизация прокрутки с более высоким приоритетом
    textArea.addEventListener('scroll', function(e) {
        // Используем requestAnimationFrame для лучшей производительности
        window.requestAnimationFrame(function() {
            syncScrollPosition();
        });
    }, { passive: true });
    
    // События изменения позиции курсора (используем debounce для снижения нагрузки)
    let cursorTimeout = null;
    const updateCursor = function() {
        if (cursorTimeout) clearTimeout(cursorTimeout);
        cursorTimeout = setTimeout(highlightActiveLine, 10);
    };
    
    // Обработчики событий для текстового поля
    textArea.addEventListener('click', updateCursor);
    textArea.addEventListener('keyup', updateCursor);
    textArea.addEventListener('input', updateCursor);
    
    // Начальная синхронизация размеров
    syncEditorSizes();
    
    // Обработчик изменения размеров окна
    window.addEventListener('resize', function() {
        syncEditorSizes();
        syncScrollPosition(); // Важно синхронизировать прокрутку после изменения размера
    });
    
    // Начальный фокус и синхронизация
    setTimeout(function() {
        textArea.focus();
        highlightActiveLine();
        syncScrollPosition(); // Явная синхронизация прокрутки при старте
    }, 100);
    
    console.log('Editor initialization completed');
}); 