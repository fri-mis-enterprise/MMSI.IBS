"use strict";

class VesselPlanningManager {
    constructor() {
        this.dashboard = document.getElementById('dashboardContainer');
        this.missionList = document.getElementById('missionList');
        this.fleetBoard = document.getElementById('fleetBoard');
        this.portFilter = document.getElementById('portFilter');
        this.refreshBtn = document.getElementById('refreshBtn');
        this.pendingCount = document.getElementById('pendingCount');

        this.assignModal = new bootstrap.Modal(document.getElementById('assignModal'));
        this.confirmBtn = document.getElementById('confirmAssignBtn');

        this.fullData = null;
        this.selectedMission = null;
        this.isUnassignOperation = false;

        this.init();
    }

    init() {
        this.setupEventListeners();
        this.setupSignalR();
        this.load();
    }

    setupEventListeners() {
        this.portFilter.addEventListener('change', () => this.load());
        this.refreshBtn.addEventListener('click', () => this.load());
        this.confirmBtn.addEventListener('click', () => {
            if (this.isUnassignOperation) {
                this.executeUnassignment();
            } else {
                this.executeAssignment();
            }
        });
    }

    setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/planningHub")
            .withAutomaticReconnect()
            .build();

        this.connection.on("OnPlanUpdated", (portId) => {
            const selectedPort = this.portFilter.value;
            if (!selectedPort || selectedPort == portId) {
                this.load();
            }
        });
        this.connection.start().catch(err => console.error(err.toString()));
    }

    async load() {
        const portId = this.portFilter.value;
        try {
            const response = await fetch(`/User/VesselPlanning/GetData?portId=${portId}`);
            const data = await response.json();
            this.fullData = data;
            this.render();
        } catch (error) {
            console.error('Failed to load dashboard:', error);
        }
    }

    render() {
        // Keep selectedMission in sync with fresh data after a reload
        if (this.selectedMission) {
            const refreshed = this.fullData.pendingJobs.find(j => j.jobOrderId === this.selectedMission.jobOrderId);
            if (refreshed) {
                this.selectedMission = refreshed;
            } else {
                // Mission was fulfilled – deactivate
                this.selectedMission = null;
                this.dashboard.classList.remove('mission-active');
            }
        }
        this.renderMissions();
        this.renderFleet();
    }

    renderMissions() {
        this.missionList.innerHTML = '';
        this.pendingCount.textContent = `${this.fullData.pendingJobs.length} Jobs`;

        if (this.fullData.pendingJobs.length === 0) {
            this.missionList.innerHTML = '<div class="text-center py-5 text-muted small">No pending missions</div>';
            return;
        }

        this.fullData.pendingJobs.forEach(job => {
            const card = document.createElement('div');
            card.className = `mission-card ${this.selectedMission?.jobOrderId === job.jobOrderId ? 'active' : ''}`;
            
            const start = new Date(job.start);
            const pct = job.requiredTugs > 0 ? Math.min(100, Math.round((job.assignedTugs / job.requiredTugs) * 100)) : 0;
            const isFull = job.assignedTugs >= job.requiredTugs;
            const barColor = isFull ? 'var(--secondary)' : (job.assignedTugs > 0 ? '#f6c23e' : '#cbd5e1');

            // Build tug slot pips
            let slotPips = '';
            for (let i = 0; i < job.requiredTugs; i++) {
                const filled = i < job.assignedTugs;
                slotPips += `<span style="display:inline-block;width:12px;height:12px;border-radius:50%;border:2px solid ${barColor};background:${filled ? barColor : 'transparent'};margin-right:3px;transition:all 0.2s;"></span>`;
            }

            card.innerHTML = `
                <div class="d-flex justify-content-between align-items-center mb-1">
                    <span class="modern-value-lg">${job.vesselName}</span>
                    <span class="modern-badge ${isFull ? 'bg-success' : 'bg-warning'}">${job.assignedTugs}/${job.requiredTugs} Tugs</span>
                </div>
                <div class="modern-label m-0" style="font-size: 10px;">${job.portName} &rsaquo; ${job.terminalName}</div>
                <div class="text-muted small mb-2"><i class="bi bi-clock me-1"></i>${start.toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'})} &middot; ${start.toLocaleDateString()}</div>
                <div class="d-flex align-items-center gap-1">${slotPips}</div>
            `;

            card.onclick = () => {
                if (this.selectedMission?.jobOrderId === job.jobOrderId) {
                    this.selectedMission = null;
                    this.dashboard.classList.remove('mission-active');
                } else {
                    this.selectedMission = job;
                    this.dashboard.classList.add('mission-active');
                }
                this.renderMissions();
                this.renderFleet();
            };
            this.missionList.appendChild(card);
        });
    }

    renderFleet() {
        this.fleetBoard.innerHTML = '';
        this.fullData.ports.forEach(port => {
            const section = document.createElement('section');
            section.className = 'port-section';
            
            section.innerHTML = `
                <div class="port-header">
                    <div class="d-flex align-items-baseline gap-2">
                        <h2 class="modern-headline-lg m-0">${port.portName}</h2>
                        <span class="modern-label">Fleet Load: ${port.activeOwned}/${port.totalOwned} Owned</span>
                    </div>
                    ${port.outsourcedInUse > 0 ? `<span class="modern-badge bg-info text-white">${port.outsourcedInUse} Outsourced</span>` : ''}
                </div>
                <div class="tug-grid" id="port-grid-${port.portId}"></div>
            `;

            this.fleetBoard.appendChild(section);
            const grid = document.getElementById(`port-grid-${port.portId}`);

            // Render Owned Tugs
            port.ownedTugboats.forEach(tug => {
                grid.appendChild(this.createTugCard(tug, port.portId, port.portName));
            });

            // Render Outsourced Tugs
            port.outsourcedTugboats.forEach(tug => {
                grid.appendChild(this.createTugCard(tug, port.portId, port.portName));
            });
        });
    }

    createTugCard(tug, portId, portName) {
        const div = document.createElement('div');
        
        let isAssigned = false;
        let isMismatch = false;
        let btnText = "Assign Here";
        if (this.selectedMission) {
            if (this.selectedMission.assignedTugboatIds) {
                isAssigned = this.selectedMission.assignedTugboatIds.includes(tug.tugboatId);
            }
            if (isAssigned) {
                btnText = "Remove Assignment";
            } else {
                isMismatch = portId !== this.selectedMission.portId;
                if (isMismatch) {
                    btnText = "Assign (Cross-Port)";
                }
            }
        }

        div.className = `tug-card status-${tug.status.toLowerCase()} ${!tug.isCompanyOwned ? 'outsourced' : ''} ${isMismatch ? 'port-mismatch' : ''} ${isAssigned ? 'assigned' : ''}`;
        
        div.innerHTML = `
            <div class="d-flex justify-content-between align-items-start">
                <span class="fw-bold">${tug.tugboatName}</span>
                <span class="status-dot dot-${tug.status.toLowerCase()}"></span>
            </div>
            ${!tug.isCompanyOwned ? `<div class="modern-label" style="font-size: 9px;">${tug.providerName}</div>` : ''}
            
            <div class="flex-grow-1 py-2">
                ${tug.status === 'Working' ? `
                    <div class="small text-secondary fw-bold">${tug.currentVessel}</div>
                    <div class="text-muted" style="font-size: 10px;">Engaged</div>
                ` : `
                    <div class="text-muted small">Available</div>
                    ${tug.until ? `<div class="text-info mt-1" style="font-size: 10px;">Next: ${tug.currentVessel} at ${new Date(tug.until).toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'})}</div>` : ''}
                `}
            </div>

            <div class="selectable-overlay">
                <span class="modern-btn-primary btn-sm">${btnText}</span>
            </div>
        `;

        if (this.selectedMission) {
            const overlay = div.querySelector('.selectable-overlay');
            overlay.onclick = (e) => {
                e.stopPropagation();
                tug.portId = portId;
                this.promptAssignment(tug, portName, isAssigned);
            };
        }

        return div;
    }

    promptAssignment(tug, tugPortName, isAssigned) {
        document.getElementById('modalVesselName').textContent = this.selectedMission.vesselName;
        document.getElementById('modalTugName').textContent = tug.tugboatName;
        this.pendingAssignmentTugId = tug.tugboatId;
        this.isUnassignOperation = isAssigned;

        const warningEl = document.getElementById('modalPortWarning');
        const modalTitle = document.getElementById('modalTitle');
        const modalVesselLabel = document.getElementById('modalVesselLabel');
        const modalTugLabel = document.getElementById('modalTugLabel');
        const confirmBtn = this.confirmBtn;

        warningEl.classList.add('d-none');

        if (isAssigned) {
            modalTitle.textContent = "Confirm Resource Deallocation";
            modalVesselLabel.textContent = "Unassigning Vessel";
            modalTugLabel.textContent = "From Tugboat";
            confirmBtn.textContent = "Remove Resource";
            confirmBtn.className = "btn btn-danger px-4";
        } else {
            modalTitle.textContent = "Confirm Resource Allocation";
            modalVesselLabel.textContent = "Assigning Vessel";
            modalTugLabel.textContent = "To Tugboat";
            confirmBtn.textContent = "Assign Resource";
            confirmBtn.className = "modern-btn-primary";

            if (tug.portId !== this.selectedMission.portId) {
                document.getElementById('modalJobPort').textContent = this.selectedMission.portName;
                document.getElementById('modalTugPort').textContent = tugPortName;
                warningEl.classList.remove('d-none');
            }
        }

        this.assignModal.show();
    }

    async executeAssignment() {
        const btn = this.confirmBtn;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Assigning...';

        try {
            const response = await fetch('/User/JobOrder/AssignTugboat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({
                    jobOrderId: this.selectedMission.jobOrderId,
                    tugboatId: this.pendingAssignmentTugId
                })
            });

            const result = await response.json();
            if (result.success) {
                this.assignModal.hide();
                this.selectedMission = null;
                this.dashboard.classList.remove('mission-active');
                this.load();
            } else {
                alert(result.message || "Assignment failed.");
            }
        } catch (error) {
            console.error('Assignment error:', error);
        } finally {
            btn.disabled = false;
            btn.textContent = 'Assign Resource';
            btn.className = 'modern-btn-primary';
        }
    }

    async executeUnassignment() {
        const btn = this.confirmBtn;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Removing...';

        try {
            const response = await fetch('/User/JobOrder/UnassignTugboat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({
                    jobOrderId: this.selectedMission.jobOrderId,
                    tugboatId: this.pendingAssignmentTugId
                })
            });

            const result = await response.json();
            if (result.success) {
                this.assignModal.hide();
                this.selectedMission = null;
                this.dashboard.classList.remove('mission-active');
                this.load();
            } else {
                alert(result.message || "Removal failed.");
            }
        } catch (error) {
            console.error('Removal error:', error);
        } finally {
            btn.disabled = false;
            btn.textContent = 'Remove Resource';
            btn.className = 'btn btn-danger px-4';
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.vesselPlanning = new VesselPlanningManager();
});
