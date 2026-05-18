"use strict";

class VesselPlanningManager {
    constructor() {
        this.container = document.getElementById('timeline-container');
        this.rowsContainer = document.getElementById('terminal-rows-container');
        this.header = document.getElementById('hours-header');
        this.heatmapContainer = document.getElementById('heatmapContainer');
        this.vesselSearchEl = document.getElementById('vesselSearch');
        this.scheduleToggleEl = document.getElementById('onlyWithSchedules');
        this.portSelectorEl = document.getElementById('portSelector');
        this.targetDateEl = document.getElementById('targetDate');
        this.refreshBtn = document.getElementById('refreshBtn');
        this.nowLine = document.getElementById('now-line');
        this.listBody = document.getElementById('planningListBody');
        this.listCountEl = document.getElementById('listCount');

        // Sidebar elements
        this.sidebarEl = document.getElementById('planningSidebar');
        this.sidebar = new bootstrap.Offcanvas(this.sidebarEl);
        this.editForm = document.getElementById('editPlanningForm');
        this.sidebarConflict = document.getElementById('sidebarConflict');

        this.hourWidth = 80;
        this.headerWidth = 200;
        this.currentDate = new Date();
        this.fullData = null;
        this.currentPortId = null;

        // Panning state
        this.isPanning = false;
        this.startX = 0;
        this.scrollLeft = 0;

        this.init();
    }

    init() {
        this.setupEventListeners();
        this.setupSignalR();

        // Initial load
        const portId = this.portSelectorEl.value;
        if (portId) {
            this.currentPortId = parseInt(portId);
            this.load();
        }
    }

    setupEventListeners() {
        this.portSelectorEl.addEventListener('change', () => {
            const oldPortId = this.currentPortId;
            this.currentPortId = this.portSelectorEl.value ? parseInt(this.portSelectorEl.value) : null;

            if (oldPortId && this.connection && this.connection.state === "Connected") {
                this.connection.invoke("LeavePortGroup", oldPortId);
            }

            if (this.currentPortId) {
                if (this.connection && this.connection.state === "Connected") {
                    this.connection.invoke("JoinPortGroup", this.currentPortId);
                }
                this.load();
            } else {
                this.clear();
            }
        });

        this.targetDateEl.addEventListener('change', () => this.load());
        this.refreshBtn.addEventListener('click', () => this.load());
        this.vesselSearchEl.addEventListener('input', () => this.applyFilters());
        this.scheduleToggleEl.addEventListener('change', () => this.applyFilters());

        // Form submission
        this.editForm.addEventListener('submit', (e) => {
            e.preventDefault();
            this.saveChanges();
        });

        // Sync heatmap scroll with timeline
        this.container.addEventListener('scroll', () => {
            this.heatmapContainer.parentElement.scrollLeft = this.container.scrollLeft;
        });

        // Panning Logic
        this.container.addEventListener('mousedown', (e) => {
            const block = e.target.closest('.timeline-block');
            if (block) return; // Allow clicking blocks without panning
            
            this.isPanning = true;
            this.container.classList.add('dragging');
            this.startX = e.pageX - this.container.offsetLeft;
            this.scrollLeft = this.container.scrollLeft;
        });

        document.addEventListener('mousemove', (e) => {
            if (!this.isPanning) return;
            e.preventDefault();
            const x = e.pageX - this.container.offsetLeft;
            const walk = (x - this.startX);
            this.container.scrollLeft = this.scrollLeft - walk;
        });

        document.addEventListener('mouseup', () => {
            this.isPanning = false;
            this.container.classList.remove('dragging');
        });

        // Zooming logic (Ctrl + Wheel)
        this.container.addEventListener('wheel', (e) => {
            if (e.ctrlKey) {
                e.preventDefault();
                const delta = e.deltaY > 0 ? -10 : 10;
                this.zoom(delta, e.pageX);
            }
        }, { passive: false });
    }

    setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/planningHub")
            .withAutomaticReconnect()
            .build();

        this.connection.on("OnPlanUpdated", (portId) => {
            if (portId == this.currentPortId) {
                this.load();
            }
        });

        this.connection.start().catch(err => console.error(err.toString()));
    }

    async load() {
        if (!this.currentPortId) return;

        const date = this.targetDateEl.value;
        try {
            const response = await fetch(`/User/VesselPlanning/GetData?portId=${this.currentPortId}&date=${date}`);
            const data = await response.json();
            this.fullData = data;
            this.initTimeline(date, data);
        } catch (error) {
            console.error('Failed to load planning data:', error);
            toastr?.error("Failed to load data.");
        }
    }

    initTimeline(dateString, data) {
        this.currentDate = new Date(dateString + "-01");
        this.daysInMonth = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 0).getDate();
        
        this.updateCSSVariables();
        this.renderHeatmap(data.capacityHeatmap);
        this.applyFilters();
        this.updateNowLine();
    }

    updateCSSVariables() {
        document.documentElement.style.setProperty('--hour-width', `${this.hourWidth}px`);
        document.documentElement.style.setProperty('--days-in-month', this.daysInMonth);
    }

    applyFilters() {
        if (!this.fullData) return;

        const searchTerm = this.vesselSearchEl.value.toLowerCase();
        const onlyWithSchedules = this.scheduleToggleEl.checked;

        const filteredTerminals = this.fullData.terminals.map(terminal => {
            return {
                ...terminal,
                blocks: terminal.blocks.filter(block => {
                    const matchesSearch = block.vesselName.toLowerCase().includes(searchTerm) || 
                                          (block.customerName && block.customerName.toLowerCase().includes(searchTerm));
                    return matchesSearch;
                })
            };
        }).filter(terminal => !onlyWithSchedules || terminal.blocks.length > 0);

        this.renderTimeline(filteredTerminals);
        this.renderList(filteredTerminals);
    }

    renderHeader() {
        this.header.innerHTML = '';
        const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        
        for (let d = 1; d <= this.daysInMonth; d++) {
            const date = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth(), d);
            const dayName = days[date.getDay()];
            
            const div = document.createElement('div');
            div.className = 'day-slot';
            div.innerHTML = `
                <div class="day-label border-bottom"><strong>${d}</strong> ${dayName}</div>
                <div class="hours-sub-header">
                    ${Array.from({length: 24}, (_, i) => `<div class="hour-tick">${i}</div>`).join('')}
                </div>`;
            this.header.appendChild(div);
        }
    }

    renderTimeline(terminals) {
        this.renderHeader();
        this.rowsContainer.innerHTML = '';
        
        terminals.forEach(terminal => {
            const row = document.createElement('div');
            row.className = 'timeline-header-row';
            
            const labelCol = document.createElement('div');
            labelCol.className = 'terminal-name-col';
            labelCol.textContent = terminal.terminalName;
            row.appendChild(labelCol);
            
            const blocksContainer = document.createElement('div');
            blocksContainer.className = 'blocks-container';
            
            terminal.blocks.forEach(block => {
                const blockEl = this.createBlockElement(block);
                blocksContainer.appendChild(blockEl);
            });
            
            row.appendChild(blocksContainer);
            this.rowsContainer.appendChild(row);
        });
    }

    createBlockElement(block) {
        const start = new Date(block.start);
        const end = new Date(block.end);
        
        const startOffsetHours = (start - this.currentDate) / (1000 * 60 * 60);
        const durationHours = (end - start) / (1000 * 60 * 60);
        
        const div = document.createElement('div');
        const statusClass = block.status.toLowerCase().replace(/\s+/g, '-');
        div.className = `timeline-block block-${statusClass}`;
        if (block.isCapacityConflict) div.classList.add('block-conflict');
        
        div.style.left = `${startOffsetHours * this.hourWidth}px`;
        div.style.width = `${Math.max(durationHours * this.hourWidth, 40)}px`;
        
        const timeStr = `${start.toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'})} - ${end.toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'})}`;
        
        let conflictHtml = '';
        if (block.isCapacityConflict && block.conflictingVessels && block.conflictingVessels.length > 0) {
            conflictHtml = `
                <div class="mt-2 pt-1 border-top border-light opacity-75">
                    <div class="fw-bold small text-warning"><i class="bi bi-exclamation-triangle-fill"></i> Conflicting With:</div>
                    <ul class="ps-3 mb-0 small" style="font-size: 0.7rem;">
                        ${block.conflictingVessels.map(v => `<li>${v}</li>`).join('')}
                    </ul>
                </div>`;
        }

        div.innerHTML = `
            <div class="block-main-content">
                <div class="block-title fw-bold">${block.vesselName}</div>
                <div class="block-details">
                    <div class="detail-item"><strong>Client:</strong> ${block.customerName || '-'}</div>
                    <div class="detail-item"><strong>Service:</strong> ${block.serviceType || '-'}</div>
                    <div class="detail-item"><strong>Time:</strong> ${timeStr}</div>
                    ${conflictHtml}
                </div>
            </div>`;
            
        div.title = `${block.vesselName} (${block.serviceType})`;
        
        div.addEventListener('click', () => this.openSidebar(block));

        return div;
    }

    renderList(terminals) {
        this.listBody.innerHTML = '';
        let allBlocks = terminals.flatMap(t => t.blocks.map(b => ({ ...b, terminalName: t.terminalName })));
        
        allBlocks.sort((a, b) => new Date(a.start) - new Date(b.start));
        this.listCountEl.textContent = `${allBlocks.length} items`;

        if (allBlocks.length === 0) {
            this.listBody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">No matching plans found.</td></tr>';
            return;
        }

        allBlocks.forEach(block => {
            const tr = document.createElement('tr');
            if (block.isCapacityConflict) tr.className = 'row-conflict';
            
            const start = new Date(block.start);
            const end = new Date(block.end);
            const statusClass = block.status.toLowerCase().replace(/\s+/g, '-');

            tr.innerHTML = `
                <td><span class="status-dot block-${statusClass}"></span> ${block.status}</td>
                <td class="fw-bold">${block.vesselName}</td>
                <td>${block.customerName || '-'}</td>
                <td>${block.serviceType || '-'}</td>
                <td>${block.terminalName}</td>
                <td>${start.toLocaleString()}</td>
                <td>${end.toLocaleString()}</td>
                <td class="text-center">${block.requiredTugs}</td>
                <td class="text-center">
                    <button class="btn btn-xs btn-primary edit-btn"><i class="bi bi-pencil"></i></button>
                </td>
            `;

            tr.querySelector('.edit-btn').onclick = () => this.openSidebar(block);
            this.listBody.appendChild(tr);
        });
    }

    renderHeatmap(heatmapData) {
        this.heatmapContainer.innerHTML = '';
        if (!heatmapData) return;

        heatmapData.forEach(slot => {
            const utilization = slot.totalTugs > 0 ? (slot.busyTugs / slot.totalTugs) : 0;
            let bgColor = "#1cc88a"; 
            if (utilization > 1) bgColor = "#e74a3b"; 
            else if (utilization > 0.8) bgColor = "#f6c23e"; 

            const div = document.createElement('div');
            div.className = 'heatmap-slot';
            div.style.backgroundColor = bgColor;
            div.title = `${new Date(slot.time).toLocaleTimeString()}: ${slot.busyTugs}/${slot.totalTugs} Tugs`;
            this.heatmapContainer.appendChild(div);
        });
    }

    openSidebar(block) {
        const idParts = block.id.split('-');
        document.getElementById('editId').value = idParts[1];
        document.getElementById('editType').value = idParts[0];
        document.getElementById('editVesselName').value = block.vesselName;
        document.getElementById('editCustomerName').value = block.customerName || '';
        document.getElementById('editTugs').value = block.requiredTugs;
        document.getElementById('editRemarks').value = block.remarks || '';
        document.getElementById('editLink').href = block.linkUrl;

        // Set date inputs
        document.getElementById('editStart').value = this.toLocalDatetimeString(new Date(block.start));
        document.getElementById('editEnd').value = this.toLocalDatetimeString(new Date(block.end));

        // Conflict indicator
        if (block.isCapacityConflict) this.sidebarConflict.classList.remove('d-none');
        else this.sidebarConflict.classList.add('d-none');

        // Disable Save for non-Planned jobs
        document.getElementById('btnSavePlanning').disabled = block.status !== "Planned";

        this.sidebar.show();
    }

    async saveChanges() {
        const id = document.getElementById('editId').value;
        const type = document.getElementById('editType').value;
        const start = new Date(document.getElementById('editStart').value);
        const end = new Date(document.getElementById('editEnd').value);
        const requiredTugs = document.getElementById('editTugs').value;

        const btn = document.getElementById('btnSavePlanning');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Saving...';

        try {
            const response = await fetch('/User/VesselPlanning/UpdatePlannedTime', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({
                    id: id,
                    type: type,
                    start: this.getLocalIsoString(start),
                    end: this.getLocalIsoString(end),
                    requiredTugs: requiredTugs
                })
            });
            const result = await response.json();
            if (result.success) {
                this.sidebar.hide();
                toastr?.success("Schedule updated successfully.");
                // SignalR will trigger reload
            } else {
                toastr?.error(result.message || "Update failed.");
            }
        } catch (error) {
            console.error('Update error:', error);
            toastr?.error("An error occurred while saving.");
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-save me-1"></i> Save Changes';
        }
    }

    toLocalDatetimeString(date) {
        const pad = (n) => n.toString().padStart(2, '0');
        return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
    }

    getLocalIsoString(date) {
        const tzo = -date.getTimezoneOffset();
        const dif = tzo >= 0 ? '+' : '-';
        const pad = (num) => (num < 10 ? '0' : '') + num;
        
        return date.getFullYear() +
            '-' + pad(date.getMonth() + 1) +
            '-' + pad(date.getDate()) +
            'T' + pad(date.getHours()) +
            ':' + pad(date.getMinutes()) +
            ':' + pad(date.getSeconds()) +
            dif + pad(Math.floor(Math.abs(tzo) / 60)) +
            ':' + pad(Math.abs(tzo) % 60);
    }

    zoom(delta, mouseX) {
        const oldHourWidth = this.hourWidth;
        this.hourWidth = Math.min(Math.max(this.hourWidth + delta, 20), 400);
        
        if (oldHourWidth !== this.hourWidth) {
            const timelineX = mouseX - this.container.offsetLeft + this.container.scrollLeft - this.headerWidth;
            const ratio = this.hourWidth / oldHourWidth;
            const newScrollLeft = (timelineX * ratio) - (mouseX - this.container.offsetLeft) + this.headerWidth;
            
            this.updateCSSVariables();
            this.container.scrollLeft = newScrollLeft;
            this.renderTimeline(this.fullData.terminals);
            this.updateNowLine();
        }
    }

    updateNowLine() {
        const now = new Date();
        if (now.getFullYear() === this.currentDate.getFullYear() && now.getMonth() === this.currentDate.getMonth()) {
            const minutes = (now - this.currentDate) / (1000 * 60);
            this.nowLine.style.left = (minutes / 60) * this.hourWidth + 'px';
            this.nowLine.style.display = 'block';
        } else {
            this.nowLine.style.display = 'none';
        }
    }

    toggleFullScreen(id) {
        const element = document.getElementById(id);
        if (!element) return;

        if (element.classList.contains('fullscreen-section')) {
            element.classList.remove('fullscreen-section');
            if (document.fullscreenElement) document.exitFullscreen();
        } else {
            element.classList.add('fullscreen-section');
            // Try native fullscreen if possible
            if (element.requestFullscreen) {
                element.requestFullscreen().catch(err => {
                    console.warn(`Fullscreen error: ${err.message}`);
                });
            }
        }
        
        // Listen for escape or fullscreen change to clean up
        const cleanup = () => {
            if (!document.fullscreenElement) {
                element.classList.remove('fullscreen-section');
                document.removeEventListener('fullscreenchange', cleanup);
            }
        };
        document.addEventListener('fullscreenchange', cleanup);
    }

    clear() {
        this.rowsContainer.innerHTML = '';
        this.heatmapContainer.innerHTML = '';
        this.header.innerHTML = '';
        this.listBody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">Select a port to view planning list.</td></tr>';
        this.listCountEl.textContent = '0 items';
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.vesselPlanning = new VesselPlanningManager();
});
