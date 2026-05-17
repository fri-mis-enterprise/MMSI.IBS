class TimelineManager {
    constructor(config) {
        this.container = document.getElementById(config.containerId);
        this.rowsContainer = document.getElementById(config.rowsContainerId);
        this.header = document.getElementById(config.headerId);
        this.searchEl = document.getElementById(config.searchId);
        this.sidebarSearchEl = document.getElementById(config.sidebarSearchId);
        this.scheduleToggleEl = document.getElementById(config.scheduleToggleId);
        this.checklistEl = document.getElementById(config.checklistId);
        
        this.hourWidth = 40; 
        this.headerWidth = 180;
        this.nowLine = document.getElementById('now-line');
        this.currentDate = new Date();
        this.fullData = [];
        this.selectedTugboatIds = new Set();
        
        // Drag state
        this.isDragging = false;
        this.startX = 0;
        this.startY = 0;
        this.scrollLeft = 0;
        this.scrollTop = 0;

        this.setupListeners();
        this.setupSignalR();
        this.updateCSSVariables();
    }

    setupListeners() {
        this.searchEl.addEventListener('input', () => this.applyFilters());
        this.sidebarSearchEl.addEventListener('input', () => this.renderChecklist());
        this.scheduleToggleEl.addEventListener('change', () => {
            this.syncSelectionToScheduleToggle();
            this.renderChecklist();
            this.applyFilters();
        });
        
        this.checklistEl.addEventListener('change', (e) => {
            if (e.target.classList.contains('tug-check')) {
                const id = parseInt(e.target.value);
                if (e.target.checked) this.selectedTugboatIds.add(id);
                else this.selectedTugboatIds.delete(id);
                this.applyFilters();
            }
        });

        // Dragging logic
        this.container.addEventListener('mousedown', (e) => {
            if (e.button !== 0) return; // Only left click
            this.isDragging = true;
            this.container.classList.add('dragging');
            this.startX = e.pageX - this.container.offsetLeft;
            this.startY = e.pageY - this.container.offsetTop;
            this.scrollLeft = this.container.scrollLeft;
            this.scrollTop = this.container.scrollTop;
        });

        window.addEventListener('mousemove', (e) => {
            if (!this.isDragging) return;
            e.preventDefault();
            const x = e.pageX - this.container.offsetLeft;
            const y = e.pageY - this.container.offsetTop;
            const walkX = (x - this.startX);
            const walkY = (y - this.startY);
            this.container.scrollLeft = this.scrollLeft - walkX;
            this.container.scrollTop = this.scrollTop - walkY;
        });

        window.addEventListener('mouseup', () => {
            this.isDragging = false;
            this.container.classList.remove('dragging');
        });

        // Zooming logic (Ctrl + Wheel)
        this.container.addEventListener('wheel', (e) => {
            if (e.ctrlKey) {
                e.preventDefault();
                const delta = e.deltaY > 0 ? -4 : 4;
                this.zoom(delta, e.pageX);
            }
        }, { passive: false });
    }

    zoom(delta, mouseX) {
        const oldHourWidth = this.hourWidth;
        this.hourWidth = Math.min(Math.max(this.hourWidth + delta, 10), 200);
        
        if (oldHourWidth !== this.hourWidth) {
            const timelineX = mouseX - this.container.offsetLeft + this.container.scrollLeft - this.headerWidth;
            const ratio = this.hourWidth / oldHourWidth;
            const newScrollLeft = (timelineX * ratio) - (mouseX - this.container.offsetLeft) + this.headerWidth;
            
            this.updateCSSVariables();
            this.container.scrollLeft = newScrollLeft;
            this.updateNowLine();
        }
    }

    updateCSSVariables() {
        this.container.style.setProperty('--hour-width', `${this.hourWidth}px`);
        if (this.daysInMonth) {
            this.container.style.setProperty('--days-in-month', this.daysInMonth);
        }
    }

    setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/tugboatHub")
            .withAutomaticReconnect()
            .build();

        this.connection.on("TimelineChanged", () => {
            const dateVal = document.getElementById('targetDate').value;
            this.load(dateVal + "-01");
        });

        this.connection.start().catch(err => console.error('SignalR Connection Error: ', err));
    }

    async load(dateString) {
        try {
            const response = await fetch(`/User/TugboatMonitoring/GetData?date=${dateString}`);
            const data = await response.json();
            this.initData(dateString, data);
        } catch (error) {
            console.error('Failed to load timeline data:', error);
        }
    }

    initData(dateString, data) {
        this.currentDate = new Date(dateString);
        this.daysInMonth = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 0).getDate();
        this.fullData = data;
        this.updateCSSVariables();
        
        // Reset selection on new data load if needed
        this.syncSelectionToScheduleToggle();
        
        this.renderChecklist();
        this.applyFilters();
    }

    syncSelectionToScheduleToggle() {
        const onlyWithSchedules = this.scheduleToggleEl.checked;
        this.selectedTugboatIds.clear();
        this.fullData.forEach(t => {
            if (!onlyWithSchedules || t.blocks.length > 0) {
                this.selectedTugboatIds.add(t.tugboatId);
            }
        });
    }

    applyFilters() {
        const searchTerm = this.searchEl.value.toLowerCase();
        const onlyWithSchedules = this.scheduleToggleEl.checked;
        
        const filteredData = this.fullData.filter(tugboat => {
            const matchesSearch = tugboat.tugboatName.toLowerCase().includes(searchTerm);
            const matchesSchedule = !onlyWithSchedules || tugboat.blocks.length > 0;
            const isSelected = this.selectedTugboatIds.has(tugboat.tugboatId);
            
            return matchesSearch && matchesSchedule && isSelected;
        });

        this.renderTimeline(filteredData);
    }

    renderChecklist() {
        const searchTerm = this.sidebarSearchEl.value.toLowerCase();
        
        this.checklistEl.innerHTML = '';
        this.fullData.forEach(t => {
            if (searchTerm && !t.tugboatName.toLowerCase().includes(searchTerm)) return;

            const label = document.createElement('label');
            label.className = 'list-group-item d-flex justify-content-between align-items-center py-1 px-3 small cursor-pointer';
            const badge = t.blocks.length > 0 ? `<span class="badge bg-primary rounded-pill">${t.blocks.length}</span>` : '';
            
            label.innerHTML = `
                <div>
                    <input class="form-check-input me-2 tug-check" type="checkbox" value="${t.tugboatId}" ${this.selectedTugboatIds.has(t.tugboatId) ? 'checked' : ''}>
                    ${t.tugboatName}
                </div>
                ${badge}
            `;
            this.checklistEl.appendChild(label);
        });
    }

    toggleAll(checked) {
        // Only toggle visible ones in the checklist
        this.checklistEl.querySelectorAll('.tug-check').forEach(cb => {
            cb.checked = checked;
            const id = parseInt(cb.value);
            if (checked) this.selectedTugboatIds.add(id);
            else this.selectedTugboatIds.delete(id);
        });
        this.applyFilters();
    }

    renderTimeline(data) {
        this.renderHeader();
        this.renderRows(data);
        this.updateNowLine();
    }

    renderHeader() {
        this.header.innerHTML = '';
        const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        
        for (let d = 1; d <= this.daysInMonth; d++) {
            const date = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth(), d);
            const dayName = days[date.getDay()];
            
            const div = document.createElement('div');
            div.className = 'day-slot';
            div.innerHTML = `<div class="day-label border-bottom"><strong>${d}</strong> ${dayName}</div>
                             <div class="hours-sub-header">
                                ${Array.from({length: 24}, (_, i) => `<div class="hour-tick">${i}</div>`).join('')}
                             </div>`;
            this.header.appendChild(div);
        }
    }

    renderRows(data) {
        this.rowsContainer.innerHTML = '';
        const fragment = document.createDocumentFragment();
        
        data.forEach(tugboat => {
            const row = document.createElement('div');
            row.className = 'tugboat-row';
            
            const nameCol = document.createElement('div');
            nameCol.className = 'tugboat-name';
            nameCol.textContent = tugboat.tugboatName;
            row.appendChild(nameCol);
            
            const blocksCol = document.createElement('div');
            blocksCol.className = 'blocks-container';
            
            tugboat.blocks.forEach(block => {
                const blockEl = this.createBlockElement(block);
                if (blockEl) blocksCol.appendChild(blockEl);
            });
            
            row.appendChild(blocksCol);
            fragment.appendChild(row);
        });
        
        this.rowsContainer.appendChild(fragment);
    }

    createBlockElement(block) {
        const start = new Date(block.start);
        const end = new Date(block.end);
        
        const monthStart = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth(), 1);
        const monthEnd = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 1);
        
        if (end < monthStart || start >= monthEnd) return null;

        const startOffset = Math.max(0, (start - monthStart) / (1000 * 60));
        const durationMinutes = (Math.min(end, monthEnd) - Math.max(start, monthStart)) / (1000 * 60);
        
        const div = document.createElement('div');
        div.className = `timeline-block block-${block.status.toLowerCase().replace(' ', '-')}`;
        if (block.isConflict) div.classList.add('block-conflict');
        
        div.style.setProperty('--block-start', (startOffset / 60).toFixed(4));
        div.style.setProperty('--block-duration', (durationMinutes / 60).toFixed(4));
        
        div.textContent = block.title;
        div.title = `${block.title} (${block.status})`;
        
        div.onclick = (e) => {
            e.stopPropagation();
            this.showDetails(block);
        };
        
        return div;
    }

    showDetails(block) {
        const start = new Date(block.start);
        const end = new Date(block.end);
        
        $('#modalTitle').text(block.id);
        $('#modalCustomer').text(block.customerName || '-');
        $('#modalVessel').text(block.title);
        $('#modalTime').text(`${start.toLocaleString()} - ${end.toLocaleString()}`);
        $('#modalPort').text(block.portTerminal || '-');
        
        if (block.isConflict) $('#modalConflict').removeClass('d-none');
        else $('#modalConflict').addClass('d-none');
        
        const link = document.getElementById('modalLink');
        if (block.linkUrl) {
            link.href = block.linkUrl;
            link.style.display = 'block';
        } else {
            link.style.display = 'none';
        }
        
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('blockDetailsModal'));
        modal.show();
    }

    updateNowLine() {
        const now = new Date();
        if (now.getFullYear() === this.currentDate.getFullYear() && now.getMonth() === this.currentDate.getMonth()) {
            const monthStart = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth(), 1);
            const minutes = (now - monthStart) / (1000 * 60);
            const left = (minutes / 60) * this.hourWidth + this.headerWidth;
            this.nowLine.style.left = left + 'px';
            this.nowLine.style.display = 'block';
            
            if (!this.hasScrolled) {
                const scrollPos = left - (window.innerWidth / 2);
                this.container.scrollLeft = scrollPos;
                this.hasScrolled = true;
            }
        } else {
            this.nowLine.style.display = 'none';
        }
    }
}
