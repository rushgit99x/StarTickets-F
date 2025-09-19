document.addEventListener('DOMContentLoaded', function () {
    let currentEventId = null;

    // Initialize Bootstrap modals
    const deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'), {
        keyboard: false
    });

    const approvalModal = new bootstrap.Modal(document.getElementById('approvalModal'), {
        keyboard: false
    });

    // Function to show alerts
    function showAlert(type, message) {
        const alertElement = document.getElementById(`${type}Alert`);
        const messageElement = document.getElementById(`${type}Message`);
        messageElement.textContent = message;
        alertElement.style.display = 'flex';
        setTimeout(() => {
            alertElement.style.display = 'none';
        }, 5000);
    }

    // Delete event function
    window.deleteEvent = function (eventId) {
        currentEventId = eventId;
        deleteModal.show();
    };

    // Submit for approval function
    window.submitForApproval = function (eventId) {
        currentEventId = eventId;
        approvalModal.show();
    };

    // Confirm delete event
    document.getElementById('confirmDeleteBtn').addEventListener('click', function () {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!tokenInput) {
            console.error('CSRF token not found');
            showAlert('error', 'CSRF token is missing. Please refresh the page and try again.');
            deleteModal.hide();
            return;
        }

        fetch(`/EventOrganizer/DeleteEvent/${currentEventId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': tokenInput.value
            }
        })
            .then(response => response.json())
            .then(data => {
                deleteModal.hide();
                if (data.success) {
                    showAlert('success', data.message || 'Event deleted successfully');
                    const eventCard = document.querySelector(`.event-card[data-event-id="${currentEventId}"]`);
                    if (eventCard) {
                        eventCard.remove();
                    }
                    // Update event count
                    const eventCountElement = document.querySelector('.event-count');
                    const currentCount = parseInt(eventCountElement.textContent);
                    eventCountElement.textContent = `${currentCount - 1} events found`;
                } else {
                    showAlert('error', data.message || 'Failed to delete event');
                }
            })
            .catch(error => {
                deleteModal.hide();
                showAlert('error', 'An error occurred while deleting the event');
                console.error('Error:', error);
            });
    });

    // Confirm submit for approval
    document.getElementById('confirmApprovalBtn').addEventListener('click', function () {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!tokenInput) {
            console.error('CSRF token not found');
            showAlert('error', 'CSRF token is missing. Please refresh the page and try again.');
            approvalModal.hide();
            return;
        }

        fetch(`/EventOrganizer/SubmitForApproval/${currentEventId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': tokenInput.value
            }
        })
            .then(response => response.json())
            .then(data => {
                approvalModal.hide();
                if (data.success) {
                    showAlert('success', data.message || 'Event submitted for approval');
                    const eventCard = document.querySelector(`.event-card[data-event-id="${currentEventId}"]`);
                    if (eventCard) {
                        const statusBadge = eventCard.querySelector('.status-badge');
                        statusBadge.classList.remove('status-draft');
                        statusBadge.classList.add('status-published');
                        statusBadge.innerHTML = '<i class="fas fa-check-circle"></i><span>Published</span>';
                        // Remove submit for approval button
                        const submitButton = eventCard.querySelector('button[onclick^="submitForApproval"]');
                        if (submitButton) {
                            submitButton.remove();
                        }
                    }
                } else {
                    showAlert('error', data.message || 'Failed to submit event for approval');
                }
            })
            .catch(error => {
                approvalModal.hide();
                showAlert('error', 'An error occurred while submitting the event');
                console.error('Error:', error);
            });
    });

    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        if (!alert.id.includes('Alert')) { // Only auto-dismiss TempData alerts
            setTimeout(() => {
                alert.style.display = 'none';
            }, 5000);
        }
    });

    // Form submission handling
    document.getElementById('filterForm').addEventListener('submit', function (e) {
        e.preventDefault();
        const form = this;
        const formData = new FormData(form);
        const queryString = new URLSearchParams(formData).toString();
        window.location.href = `/EventOrganizer/MyEvents?${queryString}`;
    });
});