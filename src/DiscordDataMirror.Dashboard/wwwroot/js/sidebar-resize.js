// Sidebar resize functionality
(function() {
    let isResizing = false;
    let startX = 0;
    let startWidth = 0;
    let drawer = null;
    let handle = null;

    function initSidebarResize() {
        drawer = document.querySelector('.mud-drawer');
        if (!drawer) {
            // Retry later if drawer not found yet
            setTimeout(initSidebarResize, 500);
            return;
        }
        
        if (drawer.dataset.resizeInit === 'true') return;
        
        // Create resize handle
        handle = document.createElement('div');
        handle.className = 'sidebar-resize-handle';
        handle.innerHTML = '<div class="resize-line"></div>';
        drawer.appendChild(handle);
        drawer.dataset.resizeInit = 'true';
        
        // Mouse events
        handle.addEventListener('mousedown', startResize);
        document.addEventListener('mousemove', resize);
        document.addEventListener('mouseup', stopResize);
        
        // Load saved width
        const savedWidth = localStorage.getItem('sidebar-width');
        if (savedWidth && parseInt(savedWidth) >= 200 && parseInt(savedWidth) <= 400) {
            drawer.style.width = savedWidth + 'px';
        }
        
        console.log('Sidebar resize initialized');
    }

    function startResize(e) {
        e.preventDefault();
        isResizing = true;
        startX = e.clientX;
        startWidth = drawer.offsetWidth;
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none';
        handle.classList.add('active');
    }

    function resize(e) {
        if (!isResizing) return;
        
        const diff = e.clientX - startX;
        const newWidth = Math.min(Math.max(startWidth + diff, 200), 400);
        drawer.style.width = newWidth + 'px';
    }

    function stopResize() {
        if (!isResizing) return;
        
        isResizing = false;
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        handle.classList.remove('active');
        
        // Save width
        localStorage.setItem('sidebar-width', drawer.offsetWidth);
    }

    // Initialize when ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => setTimeout(initSidebarResize, 100));
    } else {
        setTimeout(initSidebarResize, 100);
    }
})();
