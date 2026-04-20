$(document).ready(function () {
    $('#ticketsTable').DataTable({
        responsive: true,
        order: [[0, 'desc']],
        language: {
            search: "_INPUT_",
            searchPlaceholder: "Search Tickets..."
        }
    });

    // Apply status badge classes
    $('[data-status]').each(function() {
        const status = $(this).data('status');
        $(this).addClass(getStatusBadgeClass(status));
    });

    // Show TempData messages
    if (PageConfig.messages.error) {
        Swal.fire({
            icon: 'error',
            title: 'Error',
            html: PageConfig.messages.error,
            confirmButtonColor: '#d33'
        });
    }
    if (PageConfig.messages.success) {
        Swal.fire({
            icon: 'success',
            title: 'Success',
            text: PageConfig.messages.success,
            timer: 3000,
            showConfirmButton: false
        });
    }
});

function loadModal(url, contentContainerId, modalId, params = {}) {
    const contentContainer = $(`#${contentContainerId}`);

    contentContainer.html(`
        <div class="text-center py-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>`);

    $.get(url, params, function (data) {
        if (data.indexOf('permission-denied-content') !== -1) {
            $('#permissionDeniedModal .modal-body').html(data);
            new bootstrap.Modal(document.getElementById('permissionDeniedModal')).show();
        } else {
            contentContainer.html(data);
            new bootstrap.Modal(document.getElementById(modalId)).show();

            // Re-initialize Select2 dropdowns after content loads
            if (modalId === 'addTicketModal') {
                contentContainer.find('.js-select2').each(function() {
                    const $select = $(this);
                    if ($select.data('select2')) {
                        $select.select2('destroy');
                    }
                });
            }
        }
    }).fail(function (xhr) {
        if (xhr.responseText && xhr.responseText.indexOf('permission-denied-content') !== -1) {
            $('#permissionDeniedModal .modal-body').html(xhr.responseText);
            new bootstrap.Modal(document.getElementById('permissionDeniedModal')).show();
        } else {
            contentContainer.html('<div class="alert alert-danger"><i class="bi bi-exclamation-triangle me-2"></i>Failed to load. Please try again.</div>');
        }
    });
}

function openDispatchTicketModal(ticketId = null) {
    const title = document.getElementById('addTicketModalLabel');
    const saveBtn = document.getElementById('saveTicketBtn');

    title.innerHTML = ticketId
        ? '<i class="bi bi-pencil-square me-2"></i>Edit Dispatch Ticket'
        : '<i class="bi bi-plus-circle me-2"></i>Add New Dispatch Ticket';
    saveBtn.innerHTML = ticketId
        ? '<i class="bi bi-check-circle me-1"></i> Update Ticket'
        : '<i class="bi bi-save me-1"></i> Save Ticket';

    loadModal(
        PageConfig.urls.getDispatchTicketPartial,
        'addTicketModalBody',
        'addTicketModal',
        { id: ticketId, jobOrderId: PageConfig.jobOrderId }
    );

    setTimeout(function () {
        initializeDurationCalculation();
    }, 200);
}

function openEditJobOrderModal(jobOrderId, hasTickets = false, skipWarning = false) {
    if (!skipWarning && (hasTickets === "true" || hasTickets === true)) {
        Swal.fire({
            icon: 'warning',
            title: 'Edit Job Order with Tickets',
            html: `
                <div class="text-start">
                    <p class="mb-2"><strong>This Job Order has existing dispatch tickets.</strong></p>
                    <p class="small text-muted mb-0">
                        Changing details (especially Port, Terminal, or Vessel) may require you to review
                        and update the affected tickets to ensure consistency.
                    </p>
                </div>
            `,
            showCancelButton: true,
            confirmButtonColor: '#0d6efd',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Continue Editing',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                openEditJobOrderModal(jobOrderId, false, true);
            }
        });
        return;
    }

    loadModal(PageConfig.urls.editModal, 'editJobOrderModalContent', 'editJobOrderModal', { id: jobOrderId });

    setTimeout(function () {
        initializeEditModal();
    }, 100);
}

function submitEditJobOrderForm() {
    const form = $('#editJobOrderForm');
    if (form.length === 0) return;

    if (!form[0].checkValidity()) {
        form[0].reportValidity();
        return;
    }

    Swal.fire({
        title: 'Saving Changes...',
        text: 'Please wait',
        allowOutsideClick: false,
        didOpen: () => { Swal.showLoading(); }
    });

    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: form.serialize(),
        success: function (response) {
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Saved!',
                    text: 'Job Order updated successfully.',
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    window.location.href = response.redirectUrl || window.location.href;
                });
            } else {
                Swal.close();
                $('#editJobOrderModal .modal-content').html(response);
                $('#editJobOrderModal .js-select2').select2({
                    dropdownParent: $('#editJobOrderModal'),
                    width: '100%'
                });
            }
        },
        error: function (xhr, status, error) {
            console.error('Error saving job order:', status, error);
            Swal.fire('Error', 'An error occurred while saving changes.', 'error');
        }
    });
}

function initializeEditModal() {
    $('#editJobOrderModal .js-select2').select2({
        dropdownParent: $('#editJobOrderModal'),
        width: '100%'
    });

    $('#EditPortId').off('change').on('change', function () {
        var portId = $(this).val();
        var terminalSelect = $('#EditTerminalId');

        if (!portId) {
            terminalSelect.empty().append('<option value="">-- Select Terminal --</option>').trigger('change');
            return;
        }

        terminalSelect.empty().append('<option value="">Loading...</option>').trigger('change');

        $.ajax({
            url: PageConfig.urls.changeTerminal,
            type: 'GET',
            data: { portId: portId },
            success: function (data) {
                terminalSelect.empty().append('<option value="">-- Select Terminal --</option>');
                $.each(data, function (i, item) {
                    terminalSelect.append($('<option>', {
                        value: item.Value || item.value,
                        text: item.Text || item.text
                    }));
                });
                terminalSelect.trigger('change');
            },
            error: function () {
                terminalSelect.empty().append('<option value="">Error loading terminals</option>').trigger('change');
            }
        });
    });

    var initialPortId = $('#EditPortId').val();
    if (initialPortId && $('#EditTerminalId').children('option').length <= 1) {
        setTimeout(function () {
            $('#EditPortId').trigger('change');
        }, 200);
    }
}

function openDispatchTicketModalWithWarning(ticketId, status) {
    Swal.fire({
        icon: 'warning',
        title: 'Edit Billed Ticket',
        html: `<p>This ticket has status <strong class="text-primary">${status}</strong>.</p>
               <p class="small text-muted">Editing this ticket may affect billing records. Make sure to update all related information correctly.</p>`,
        showCancelButton: true,
        confirmButtonColor: '#0d6efd',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, edit anyway',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            openDispatchTicketModal(ticketId);
        }
    });
}

function submitAddTicketForm() {
    var form = $('#dispatchTicketForm');
    if (form.length === 0) return;

    if (!form[0].checkValidity()) {
        form[0].reportValidity();
        return;
    }

    Swal.fire({
        title: 'Saving Ticket...',
        text: 'Please wait',
        allowOutsideClick: false,
        didOpen: () => { Swal.showLoading(); }
    });

    form.submit();
}

function initializeDurationCalculation() {
    const checkExist = setInterval(function () {
        let dateLeftInput = $('#ModalDateLeft, [name="DateLeft"]');
        let timeLeftInput = $('#ModalTimeLeft, [name="TimeLeft"]');
        let dateArrivedInput = $('#ModalDateArrived, [name="DateArrived"]');
        let timeArrivedInput = $('#ModalTimeArrived, [name="TimeArrived"]');
        let durationBadge = $('#modalDurationDisplay');
        let durationDisplay = $('#modalTotalHours');

        if (dateLeftInput.length && timeLeftInput.length &&
            dateArrivedInput.length && timeArrivedInput.length &&
            durationBadge.length && durationDisplay.length) {

            clearInterval(checkExist);

            dateLeftInput.add(timeLeftInput).add(dateArrivedInput).add(timeArrivedInput).on('change input', function () {
                calculateDuration();
            });

            calculateDuration();
        }
    }, 100);

    function calculateDuration() {
        let dateLeft    = $('#ModalDateLeft').val()    || $('[name="DateLeft"]').val();
        let timeLeft    = $('#ModalTimeLeft').val()    || $('[name="TimeLeft"]').val();
        let dateArrived = $('#ModalDateArrived').val() || $('[name="DateArrived"]').val();
        let timeArrived = $('#ModalTimeArrived').val() || $('[name="TimeArrived"]').val();
        let durationBadge   = $('#modalDurationDisplay');
        let durationDisplay = $('#modalTotalHours');

        if (!dateLeft || !timeLeft || !dateArrived || !timeArrived) {
            durationDisplay.text('0');
            return;
        }

        const startDateTime = new Date(`${dateLeft}T${timeLeft}`);
        const endDateTime   = new Date(`${dateArrived}T${timeArrived}`);

        if (endDateTime < startDateTime) {
            durationDisplay.text('Invalid (End before Start)');
            durationBadge.removeClass('bg-secondary').addClass('bg-danger');
            return;
        }

        const diffHrs = ((endDateTime - startDateTime) / 3600000).toFixed(2);
        durationDisplay.text(diffHrs);
        durationBadge.removeClass('bg-danger').addClass('bg-secondary');
    }
}

function confirmClose() {
    Swal.fire({
        title: 'Close Job Order?',
        text: "Closing this Job Order will prevent adding new tickets to it.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ffc107',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, close it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $('#closeForm').submit();
        }
    });
}

function confirmCancel() {
    var htmlContent = `
        <div class="text-start">
            <p class="mb-2"><strong>This will:</strong></p>
            <ul class="mb-2">
                <li>Mark Job Order as "Cancelled"</li>
                <li>Prevent adding new dispatch tickets</li>
                <li>Affect existing tickets</li>
            </ul>
            <p class="text-danger small mb-0">
                Admin-only action. Tickets in billing process (For Billing/Billed) will block cancellation.
            </p>
        </div>
    `;

    Swal.fire({
        icon: 'warning',
        title: 'Admin Action: Cancel Job Order?',
        html: htmlContent,
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, cancel it!',
        cancelButtonText: 'Review'
    }).then((result) => {
        if (result.isConfirmed) {
            $('#cancelForm').submit();
        }
    });
}

function showTicketModal(id) {
    const contentContainer = document.getElementById('ticketModalContent');

    contentContainer.innerHTML = `
        <div class="modal-header bg-primary text-white">
            <h5 class="modal-title"><i class="bi bi-eye me-2"></i>Ticket Details</h5>
            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
        </div>
        <div class="modal-body">
            <div class="text-center py-4">
                <div class="spinner-border text-info" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        </div>
    `;

    const modal = new bootstrap.Modal(document.getElementById('ticketModal'));
    modal.show();

    fetch(`${PageConfig.urls.getTicketDetails}/${id}`)
        .then(response => response.json())
        .then(data => {
            contentContainer.innerHTML = `
                <div class="modal-header bg-primary text-white">
                    <h5 class="modal-title"><i class="bi bi-eye me-2"></i>${data.dispatchNumber}</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="row g-3">
                        <div class="col-12">
                            <h6 class="border-bottom pb-2 mb-3"><i class="bi bi-info-circle me-2"></i>Ticket Information</h6>
                        </div>
                        <div class="col-6 col-md-4">
                            <label class="text-muted small mb-0">Dispatch #</label>
                            <div class="fw-bold">${data.dispatchNumber}</div>
                        </div>
                        <div class="col-6 col-md-4">
                            <label class="text-muted small mb-0">Date</label>
                            <div class="fw-bold">${data.date}</div>
                        </div>
                        <div class="col-6 col-md-4">
                            <label class="text-muted small mb-0">Service</label>
                            <div>${data.serviceName}</div>
                        </div>
                        <div class="col-6 col-md-4">
                            <label class="text-muted small mb-0">Tugboat</label>
                            <div>${data.tugboatName}</div>
                        </div>
                        <div class="col-6 col-md-4">
                            <label class="text-muted small mb-0">Tug Master</label>
                            <div>${data.tugMasterName}</div>
                        </div>
                        <div class="col-6 col-md-4">
                            <label class="text-muted small mb-0">Location</label>
                            <div>${data.location}</div>
                        </div>
                        <div class="col-6 col-md-4">
                            <label class="text-muted small mb-0">Time Start</label>
                            <div>${data.timeStart}</div>
                        </div>
                        <div class="col-6 col-md-4">
                            <label class="text-muted small mb-0">Time End</label>
                            <div>${data.timeEnd}</div>
                        </div>
                        <div class="col-6 col-md-4">
                            <label class="text-muted small mb-0">Duration</label>
                            <div class="fw-bold text-primary">${data.totalHours} hrs</div>
                        </div>

                        ${data.dispatchRate && data.dispatchRate !== '-' && data.dispatchRate !== '0.00' ? `
                        <div class="col-12 mt-3">
                            <h6 class="border-bottom pb-2 mb-3"><i class="bi bi-currency-dollar me-2"></i>Tariff Details</h6>
                        </div>
                        <div class="col-6">
                            <div class="card h-100 border-0 bg-light">
                                <div class="card-header bg-primary text-white py-2">
                                    <strong>Dispatch</strong>
                                </div>
                                <div class="card-body p-3">
                                    <div class="row mb-2">
                                        <div class="col-6 small">Rate: ₱ ${data.dispatchRate}/hr</div>
                                        <div class="col-6 small text-end">Disc: ${data.dispatchDiscount}%</div>
                                    </div>
                                    <hr class="my-2">
                                    <div class="text-center fw-bold text-primary">₱ ${data.dispatchBilling}</div>
                                </div>
                            </div>
                        </div>
                        <div class="col-6">
                            <div class="card h-100 border-0 bg-light">
                                <div class="card-header bg-primary text-white py-2">
                                    <strong>BAF</strong>
                                </div>
                                <div class="card-body p-3">
                                    <div class="row mb-2">
                                        <div class="col-6 small">Rate: ₱ ${data.bafRate}/hr</div>
                                        <div class="col-6 small text-end">Disc: ${data.bafDiscount}%</div>
                                    </div>
                                    <hr class="my-2">
                                    <div class="text-center fw-bold text-primary">₱ ${data.bafBilling}</div>
                                </div>
                            </div>
                        </div>
                        <div class="col-12">
                            <div class="card bg-light">
                                <div class="card-body py-3">
                                    <div class="row g-2 align-items-center">
                                        <div class="col-6 text-center border-end">
                                            <div class="text-muted small">Total Billing</div>
                                            <div class="fw-bold">₱ ${data.totalBilling}</div>
                                        </div>
                                        <div class="col-6 text-center">
                                            <div class="text-muted small">Net Revenue</div>
                                            <div class="fw-bold text-success">₱ ${data.totalNetRevenue}</div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        ` : '<div class="col-12"><div class="alert alert-warning mb-0 py-2 small"><i class="bi bi-exclamation-triangle me-1"></i>No tariff details set yet.</div></div>'}

                        <div class="col-12 mt-3">
                            <div class="row">
                                <div class="col-md-8">
                                    <label class="text-muted small mb-1">Remarks</label>
                                    <p class="mb-0 fst-italic small">${data.remarks || 'No remarks'}</p>
                                </div>
                                <div class="col-md-4 text-end">
                                    <label class="text-muted small mb-1">Status</label><br>
                                    <span class="badge rounded-pill ${getStatusBadgeClass(data.status)}">${data.status}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    ${buildTicketModalActions(data)}
                </div>
            `;
        })
        .catch(error => {
            contentContainer.innerHTML = `
                <div class="modal-header bg-danger text-white">
                    <h5 class="modal-title">Error</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="alert alert-danger">Error loading ticket details: ${error}</div>
                </div>
            `;
        });
}

function getStatusBadgeClass(status) {
    const classes = {
        'Pending':      'bg-secondary',
        'For Tariff':   'bg-warning text-dark',
        'For Approval': 'bg-orange',
        'For Billing':  'bg-primary',
        'Billed':       'bg-success',
        'Disapproved':  'bg-danger'
    };
    return classes[status] || 'bg-secondary';
}

function buildTicketModalActions(data) {
    const isJobOrderOpen = PageConfig.status === 'Open';
    const jobOrderStatus = PageConfig.status;
    let buttons = '<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>';

    if (!isJobOrderOpen) {
        buttons += `
            <span class="text-muted small ms-2">
                Job Order is ${jobOrderStatus} — no actions available
            </span>`;
        return buttons;
    }

    switch (data.status) {
        case 'Pending':
        case 'For Tariff':
            buttons = `
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                <button type="button" class="btn btn-primary" onclick="openSetTariffModal(${data.id})">
                    <i class="bi bi-currency-dollar me-1"></i>Set Tariff
                </button>`;
            break;
        case 'For Approval':
            buttons = `
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                <button type="button" class="btn btn-danger" onclick="openDisapproveModal(${data.id})">
                    <i class="bi bi-x-circle me-1"></i>Disapprove
                </button>
                <button type="button" class="btn btn-success" onclick="approveTariff(${data.id})">
                    <i class="bi bi-check-circle me-1"></i>Approve
                </button>`;
            break;
        case 'Disapproved':
            buttons = `
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                <button type="button" class="btn btn-warning" onclick="openEditTariffModal(${data.id})">
                    <i class="bi bi-pencil me-1"></i>Edit Tariff
                </button>`;
            break;
        case 'For Billing':
        case 'Billed':
            buttons = `
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                <button type="button" class="btn btn-primary" onclick="openDispatchTicketModalWithWarning(${data.id}, '${data.status}')">
                    <i class="bi bi-pencil me-1"></i>Edit Ticket
                </button>`;
            break;
    }

    return buttons;
}

function openSetTariffModal(ticketId) {
    loadModal(PageConfig.urls.setTariffModal, 'setTariffModalContent', 'setTariffModal', { id: ticketId });
    setTimeout(function () { initializeTariffCalculation(); }, 100);
}

function openEditTariffModal(ticketId) {
    loadModal(PageConfig.urls.editTariffModal, 'editTariffModalContent', 'editTariffModal', { id: ticketId });
    setTimeout(function () { initializeTariffCalculation(); }, 100);
}

function openApprovalModal(ticketId) {
    loadModal(PageConfig.urls.tariffApprovalModal, 'tariffApprovalModalContent', 'tariffApprovalModal', { id: ticketId });
}

function approveTariff(ticketId) {
    Swal.fire({
        title: 'Approve Tariff?',
        text: "This will move the ticket to 'For Billing' status.",
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#28a745',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, approve it!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Approving...',
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading(); }
            });

            $.post(PageConfig.urls.approveTariff, { id: ticketId }, function (response) {
                if (response.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Approved!',
                        text: 'Tariff approved successfully.',
                        timer: 2000,
                        showConfirmButton: false
                    }).then(() => {
                        bootstrap.Modal.getInstance(document.getElementById('tariffApprovalModal')).hide();
                        location.reload();
                    });
                } else {
                    Swal.fire('Error', response.message, 'error');
                }
            }).fail(function () {
                Swal.fire('Error', 'An error occurred while approving the tariff.', 'error');
            });
        }
    });
}

function submitSetTariffForm() {
    const form = $('#setTariffForm');
    if (!form.valid()) return;

    Swal.fire({
        title: 'Saving Tariff...',
        text: 'Please wait',
        allowOutsideClick: false,
        didOpen: () => { Swal.showLoading(); }
    });

    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: form.serialize(),
        success: function (response) {
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Tariff Saved!',
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    bootstrap.Modal.getInstance(document.getElementById('setTariffModal')).hide();
                    location.reload();
                });
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: response.message });
                form.find('button[type="submit"]').prop('disabled', false).html('<i class="bi bi-save me-1"></i>Save Tariff');
            }
        },
        error: function () {
            Swal.fire('Error', 'An error occurred while saving the tariff.', 'error');
            form.find('button[type="submit"]').prop('disabled', false).html('<i class="bi bi-save me-1"></i>Save Tariff');
        }
    });
}

function submitEditTariffForm() {
    const form = $('#editTariffForm');
    if (!form.valid()) return;

    Swal.fire({
        title: 'Updating Tariff...',
        text: 'Please wait',
        allowOutsideClick: false,
        didOpen: () => { Swal.showLoading(); }
    });

    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: form.serialize(),
        success: function (response) {
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Tariff Updated!',
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    bootstrap.Modal.getInstance(document.getElementById('editTariffModal')).hide();
                    location.reload();
                });
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: response.message });
                form.find('button[type="submit"]').prop('disabled', false).html('<i class="bi bi-pencil me-1"></i>Update Tariff');
            }
        },
        error: function () {
            Swal.fire('Error', 'An error occurred while updating the tariff.', 'error');
            form.find('button[type="submit"]').prop('disabled', false).html('<i class="bi bi-pencil me-1"></i>Update Tariff');
        }
    });
}

function openDisapproveModal(ticketId) {
    $('#disapproveTicketId').val(ticketId);
    $('#disapproveReason').val('').removeClass('is-invalid');

    new bootstrap.Modal(document.getElementById('disapproveModal'), {
        backdrop: true
    }).show();

    document.getElementById('disapproveModal').addEventListener('shown.bs.modal', function () {
        document.getElementById('disapproveReason').focus();
    }, { once: true });
}

function addDisapproveReason(reason) {
    const current = $('#disapproveReason').val();
    $('#disapproveReason').val(current ? current + ' - ' + reason : reason).focus();
}

function submitDisapproval() {
    const ticketId = $('#disapproveTicketId').val();
    const reason   = $('#disapproveReason').val().trim();

    if (reason.length < 10) {
        $('#disapproveReason').addClass('is-invalid');
        return;
    }

    bootstrap.Modal.getInstance(document.getElementById('disapproveModal')).hide();

    Swal.fire({
        title: 'Disapproving...',
        allowOutsideClick: false,
        didOpen: () => { Swal.showLoading(); }
    });

    $.post(PageConfig.urls.disapproveTariff, {
        id: ticketId,
        reason: reason
    }, function (response) {
        if (response.success) {
            Swal.fire({
                icon: 'success',
                title: 'Disapproved!',
                text: 'Tariff disapproved successfully.',
                timer: 2000,
                showConfirmButton: false
            }).then(() => {
                bootstrap.Modal.getInstance(document.getElementById('tariffApprovalModal'))?.hide();
                location.reload();
            });
        } else {
            Swal.fire('Error', response.message, 'error');
        }
    }).fail(function () {
        Swal.fire('Error', 'An error occurred while disapproving the tariff.', 'error');
    });
}

function calculateTariffInModal() {
    const totalHours   = parseFloat($('#TotalHours').val()) || 0;
    const chargeType   = $('input[name="chargeType"]:checked').val() || 'Per hour';

    const dispatchRate     = parseFloat($('#DispatchRate').val()) || 0;
    const dispatchDiscount = parseFloat($('#DispatchDiscount').val()) || 0;
    let dispatchBilling    = chargeType === 'Per hour' ? dispatchRate * totalHours : dispatchRate;
    const dispatchNet      = dispatchBilling - (dispatchBilling * dispatchDiscount / 100);

    $('#dispatchBilling').text('₱ ' + dispatchBilling.toFixed(2));
    $('#dispatchNet').text('₱ ' + dispatchNet.toFixed(2));
    $('#hiddenDispatchBilling').val(dispatchBilling.toFixed(2));
    $('#hiddenDispatchNet').val(dispatchNet.toFixed(2));

    const bafRate     = parseFloat($('#BAFRate').val()) || 0;
    const bafDiscount = parseFloat($('#BAFDiscount').val()) || 0;
    let bafBilling    = chargeType === 'Per hour' ? bafRate * totalHours : bafRate;
    const bafNet      = bafBilling - (bafBilling * bafDiscount / 100);

    $('#bafBilling').text('₱ ' + bafBilling.toFixed(2));
    $('#bafNet').text('₱ ' + bafNet.toFixed(2));
    $('#hiddenBAFBilling').val(bafBilling.toFixed(2));
    $('#hiddenBAFNet').val(bafNet.toFixed(2));

    const apOtherTugs  = parseFloat($('#ApOtherTugs').val()) || 0;
    const totalBilling = dispatchBilling + bafBilling + apOtherTugs;
    const totalNet     = dispatchNet + bafNet + apOtherTugs;

    $('#totalBilling').text('₱ ' + totalBilling.toFixed(2));
    $('#totalNet').text('₱ ' + totalNet.toFixed(2));
    $('#hiddenTotalBilling').val(totalBilling.toFixed(2));
    $('#hiddenTotalNet').val(totalNet.toFixed(2));
}

function updateChargeType() {
    calculateTariffInModal();
}

function initializeTariffCalculation() {
    $('#DispatchRate, #DispatchDiscount, #BAFRate, #BAFDiscount, #ApOtherTugs').on('input', function () {
        calculateTariffInModal();
    });

    $('input[name="chargeType"]').on('change', function () {
        calculateTariffInModal();
    });

    $('#CustomerId').on('change', function () {
        checkForDefaultTariff();
    });

    calculateTariffInModal();

    setTimeout(function () {
        checkForDefaultTariff();
    }, 200);
}

function checkForDefaultTariff() {
    const customerId       = $('#CustomerId').val();
    const dispatchTicketId = $('#DispatchTicketId').val();

    if (!customerId || !dispatchTicketId) return;

    $.ajax({
        url: PageConfig.urls.checkForTariffRate,
        type: 'POST',
        data: { customerId: customerId, dispatchTicketId: dispatchTicketId },
        success: function (result) {
            if (result.exists) {
                $('#DispatchRate').val(result.dispatch);
                $('#BAFRate').val(result.baf);
                $('#DispatchDiscount').val(result.dispatchDiscount);
                $('#BAFDiscount').val(result.bafDiscount);
                calculateTariffInModal();

                Swal.fire({
                    icon: 'success',
                    title: 'Default rates applied!',
                    html: `Dispatch: ₱${result.dispatch}<br>BAF: ₱${result.baf}`,
                    timer: 3000,
                    showConfirmButton: false,
                });
            }
        },
        error: function () {
            console.log('Could not fetch default tariff rates');
        }
    });
}
