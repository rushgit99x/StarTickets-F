// Document ready function
$(document).ready(function () {
    initializeVenueManagement();
});

/**
 * Initialize all venue management functionality
 */
function initializeVenueManagement() {
    initializeStatusToggle();
    initializeDeleteConfirmations();
    initializeFormValidation();
    initializeFormEnhancements();
    initializeSearchFunctionality();
    initializeTooltips();
    initializeDataTables();
}

/**
 * Initialize venue status toggle functionality
 */
function initializeStatusToggle() {
    $(document).on('click', '.toggle-status-btn', function () {
        const venueId = $(this).data('venue-id');
        const currentText = $(this).text().trim();
        const isActivating = currentText.includes('Activate');

        const confirmMessage = isActivating
            ? 'Are you sure you want to activate this venue? It will be available for new events.'
            : 'Are you sure you want to deactivate this venue? It will not be available for new events.';

        if (confirm(confirmMessage)) {
            toggleVenueStatus(venueId);
        }
    });
}

/**
 * Toggle venue status
 */
function toggleVenueStatus(venueId) {
    const button = $(`.toggle-status-btn[data-venue-id="${venueId}"]`);
    const originalHtml = button.html();

    // Show loading state
    button.html('<i class="fas fa-spinner fa-spin"></i> Updating...').prop('disabled', true);

    $.ajax({
        url: '/VenueManagement/ToggleStatus',
        type: 'POST',
        data: {
            id: venueId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                showVenueNotification(response.message, 'success');

                // Update the status badge and button
                updateVenueStatusUI(venueId, response.isActive);

                // Reload page after a short delay if on details page
                if (window.location.pathname.includes('/Details')) {
                    setTimeout(() => {
                        location.reload();
                    }, 1500);
                }
            } else {
                showVenueNotification(response.message || 'Failed to update venue status', 'error');
                button.html(originalHtml).prop('disabled', false);
            }
        },
        error: function (xhr, status, error) {
            showVenueNotification('An error occurred while updating the venue status', 'error');
            button.html(originalHtml).prop('disabled', false);
        }
    });
}

/**
 * Update venue status UI elements
 */
function updateVenueStatusUI(venueId, isActive) {
    const statusBadge = $(`.venue-row[data-venue-id="${venueId}"] .status-badge`);
    const toggleButton = $(`.toggle-status-btn[data-venue-id="${venueId}"]`);

    // Update status badge
    statusBadge
        .removeClass('status-active status-inactive')
        .addClass(isActive ? 'status-active' : 'status-inactive')
        .text(isActive ? 'Active' : 'Inactive');

    // Update toggle button
    const newIcon = isActive ? 'pause' : 'play';
    const newColor = isActive ? 'warning' : 'success';
    const newText = isActive ? 'Deactivate' : 'Activate';

    toggleButton.html(`<i class="fas fa-${newIcon} text-${newColor}"></i> ${newText}`)
        .prop('disabled', false);
}

/**
 * Initialize delete confirmation functionality
 */
function initializeDeleteConfirmations() {
    $(document).on('click', '.delete-venue-btn', function () {
        const venueId = $(this).data('venue-id');
        const venueName = $(`.venue-row[data-venue-id="${venueId}"] .venue-name`).text() || 'this venue';

        if (confirm(`Are you sure you want to delete ${venueName}? This action cannot be undone.`)) {
            deleteVenue(venueId);
        }
    });
}

/**
 * Delete a venue
 */
function deleteVenue(venueId) {
    const button = $(`.delete-venue-btn[data-venue-id="${venueId}"]`);
    const originalHtml = button.html();

    // Show loading state
    button.html('<i class="fas fa-spinner fa-spin"></i> Deleting...').prop('disabled', true);

    $.ajax({
        url: '/VenueManagement/Delete',
        type: 'POST',
        data: {
            id: venueId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                showVenueNotification(response.message, 'success');
                $(`.venue-row[data-venue-id="${venueId}"]`).fadeOut(500, function () {
                    $(this).remove();
                    // Update total venues count
                    const totalVenues = parseInt($('.stat-value').text()) - 1;
                    $('.stat-value').text(totalVenues);
                    if (totalVenues === 0) {
                        location.reload();
                    }
                });
            } else {
                showVenueNotification(response.message || 'Failed to delete venue', 'error');
                button.html(originalHtml).prop('disabled', false);
            }
        },
        error: function (xhr, status, error) {
            showVenueNotification('An error occurred while deleting the venue', 'error');
            button.html(originalHtml).prop('disabled', false);
        }
    });
}

/**
 * Initialize form validation for create and edit venue forms
 */
function initializeFormValidation() {
    // Create Venue Form
    $('#createVenueForm').validate({
        rules: {
            VenueName: {
                required: true,
                minlength: 2,
                maxlength: 100
            },
            Capacity: {
                required: true,
                number: true,
                min: 1
            },
            Address: {
                required: true,
                minlength: 5,
                maxlength: 200
            },
            City: {
                required: true,
                minlength: 2,
                maxlength: 50
            },
            Country: {
                required: true,
                minlength: 2,
                maxlength: 50
            },
            PostalCode: {
                maxlength: 20
            },
            ContactEmail: {
                email: true,
                maxlength: 100
            },
            ContactPhone: {
                maxlength: 20
            },
            Facilities: {
                maxlength: 500
            }
        },
        messages: {
            VenueName: {
                required: "Please enter a venue name",
                minlength: "Venue name must be at least 2 characters",
                maxlength: "Venue name cannot exceed 100 characters"
            },
            Capacity: {
                required: "Please enter the venue capacity",
                number: "Please enter a valid number",
                min: "Capacity must be at least 1"
            },
            Address: {
                required: "Please enter the venue address",
                minlength: "Address must be at least 5 characters",
                maxlength: "Address cannot exceed 200 characters"
            },
            City: {
                required: "Please enter the city",
                minlength: "City must be at least 2 characters",
                maxlength: "City cannot exceed 50 characters"
            },
            Country: {
                required: "Please enter the country",
                minlength: "Country must be at least 2 characters",
                maxlength: "Country cannot exceed 50 characters"
            },
            PostalCode: {
                maxlength: "Postal code cannot exceed 20 characters"
            },
            ContactEmail: {
                email: "Please enter a valid email address",
                maxlength: "Email cannot exceed 100 characters"
            },
            ContactPhone: {
                maxlength: "Phone number cannot exceed 20 characters"
            },
            Facilities: {
                maxlength: "Facilities description cannot exceed 500 characters"
            }
        },
        errorElement: 'span',
        errorPlacement: function (error, element) {
            error.addClass('text-danger');
            error.insertAfter(element);
        }
    });

    // Edit Venue Form
    $('#editVenueForm').validate({
        rules: {
            VenueName: {
                required: true,
                minlength: 2,
                maxlength: 100
            },
            Capacity: {
                required: true,
                number: true,
                min: 1
            },
            Address: {
                required: true,
                minlength: 5,
                maxlength: 200
            },
            City: {
                required: true,
                minlength: 2,
                maxlength: 50
            },
            Country: {
                required: true,
                minlength: 2,
                maxlength: 50
            },
            PostalCode: {
                maxlength: 20
            },
            ContactEmail: {
                email: true,
                maxlength: 100
            },
            ContactPhone: {
                maxlength: 20
            },
            Facilities: {
                maxlength: 500
            }
        },
        messages: {
            VenueName: {
                required: "Please enter a venue name",
                minlength: "Venue name must be at least 2 characters",
                maxlength: "Venue name cannot exceed 100 characters"
            },
            Capacity: {
                required: "Please enter the venue capacity",
                number: "Please enter a valid number",
                min: "Capacity must be at least 1"
            },
            Address: {
                required: "Please enter the venue address",
                minlength: "Address must be at least 5 characters",
                maxlength: "Address cannot exceed 200 characters"
            },
            City: {
                required: "Please enter the city",
                minlength: "City must be at least 2 characters",
                maxlength: "City cannot exceed 50 characters"
            },
            Country: {
                required: "Please enter the country",
                minlength: "Country must be at least 2 characters",
                maxlength: "Country cannot exceed 50 characters"
            },
            PostalCode: {
                maxlength: "Postal code cannot exceed 20 characters"
            },
            ContactEmail: {
                email: "Please enter a valid email address",
                maxlength: "Email cannot exceed 100 characters"
            },
            ContactPhone: {
                maxlength: "Phone number cannot exceed 20 characters"
            },
            Facilities: {
                maxlength: "Facilities description cannot exceed 500 characters"
            }
        },
        errorElement: 'span',
        errorPlacement: function (error, element) {
            error.addClass('text-danger');
            error.insertAfter(element);
        }
    });
}

/**
 * Initialize form enhancements (phone formatting, capitalization)
 */
function initializeFormEnhancements() {
    // Format phone number input
    $('.create-venue-form, .edit-venue-form').on('input', '#ContactPhone', function () {
        formatPhoneNumber($(this));
    });

    // Auto-capitalize city, country, and state
    $('.create-venue-form, .edit-venue-form').on('input', '#City, #Country, #State', function () {
        capitalizeInput($(this));
    });
}

/**
 * Format phone number input
 */
function formatPhoneNumber(input) {
    let value = input.val().replace(/\D/g, '');
    if (value.length > 2 && value.length <= 6) {
        value = `(${value.slice(0, 3)}) ${value.slice(3)}`;
    } else if (value.length > 6 && value.length <= 10) {
        value = `(${value.slice(0, 3)}) ${value.slice(3, 6)}-${value.slice(6)}`;
    } else if (value.length > 10) {
        value = `+${value.slice(0, 1)} (${value.slice(1, 4)}) ${value.slice(4, 7)}-${value.slice(7, 11)}`;
    }
    input.val(value);
}

/**
 * Capitalize input for city, country, and state
 */
function capitalizeInput(input) {
    let words = input.val().split(' ');
    words = words.map(word => {
        if (word.length > 0) {
            return word.charAt(0).toUpperCase() + word.slice(1).toLowerCase();
        }
        return word;
    });
    input.val(words.join(' '));
}

/**
 * Initialize search functionality
 */
function initializeSearchFunctionality() {
    $('.filters-form').on('submit', function (e) {
        e.preventDefault();
        const form = $(this);
        const searchTerm = form.find('input[name="searchTerm"]').val();
        const cityFilter = form.find('select[name="cityFilter"]').val();
        const activeFilter = form.find('select[name="activeFilter"]').val();

        $.ajax({
            url: form.attr('action'),
            type: 'GET',
            data: {
                searchTerm: searchTerm,
                cityFilter: cityFilter,
                activeFilter: activeFilter
            },
            success: function (response) {
                $('.venues-table-card').html($(response).find('.venues-table-card').html());
                initializeDataTables();
                initializeTooltips();
            },
            error: function () {
                showVenueNotification('An error occurred while filtering venues', 'error');
            }
        });
    });
}

/**
 * Initialize tooltips
 */
function initializeTooltips() {
    $('[title]').tooltip({
        placement: 'top',
        trigger: 'hover'
    });
}

/**
 * Initialize DataTables for venues table
 */
function initializeDataTables() {
    $('.venues-table').DataTable({
        destroy: true,
        pageLength: 10,
        lengthChange: false,
        searching: false,
        ordering: true,
        info: false,
        columnDefs: [
            { orderable: false, targets: -1 } // Disable sorting on Actions column
        ],
        language: {
            paginate: {
                previous: '<i class="fas fa-chevron-left"></i>',
                next: '<i class="fas fa-chevron-right"></i>'
            }
        }
    });
}

/**
 * Show notification messages
 */
function showVenueNotification(message, type) {
    const notification = $(`
        <div class="alert alert-${type === 'success' ? 'success' : 'danger'} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `);

    $('body').append(notification);
    setTimeout(() => {
        notification.alert('close');
    }, 5000);
}


<script>
    $(document).ready(function() {
        // Toggle venue status
        $('.toggle-status-btn').on('click', function () {
            const venueId = $(this).data('venue-id');
            const button = $(this);
            const isCurrentlyActive = button.hasClass('btn-outline-warning');
            const action = isCurrentlyActive ? 'deactivate' : 'activate';

            if (confirm(`Are you sure you want to ${action} this venue?`)) {
                toggleVenueStatus(venueId, button);
            }
        });

    // Delete venue
    $('.delete-venue-btn').on('click', function() {
        const venueId = $(this).data('venue-id');
    const venueName = $(this).closest('tr').find('.venue-name').text().trim();

    if (confirm(`Are you sure you want to delete "${venueName}"? This action cannot be undone.`)) {
        deleteVenue(venueId);
        }
    });
});

    function toggleVenueStatus(venueId, button) {
    const originalHtml = button.html();
    const originalClass = button.attr('class');

    // Show loading state
    button.html('<i class="fas fa-spinner fa-spin"></i>')
    .prop('disabled', true)
    .removeClass('btn-outline-warning btn-outline-success')
    .addClass('btn-outline-secondary');

    $.ajax({
        url: '/VenueManagement/ToggleStatus',
    type: 'POST',
    data: {
        id: venueId,
    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
    success: function(response) {
            if (response.success) {
                // Update button appearance
                const newClass = response.isActive ? 'btn-outline-warning' : 'btn-outline-success';
    const newIcon = response.isActive ? 'pause' : 'play';
    const newTitle = response.isActive ? 'Deactivate Venue' : 'Activate Venue';

    button.removeClass('btn-outline-secondary btn-outline-warning btn-outline-success')
    .addClass(`btn-sm ${newClass}`)
    .html(`<i class="fas fa-${newIcon}"></i>`)
    .attr('title', newTitle)
    .prop('disabled', false);

    // Update status badge
    const statusBadge = button.closest('tr').find('.status-badge');
    statusBadge.removeClass('status-active status-inactive')
    .addClass(response.isActive ? 'status-active' : 'status-inactive')
    .text(response.isActive ? 'Active' : 'Inactive');

    showNotification(response.message, 'success');
            } else {
        button.attr('class', originalClass)
            .html(originalHtml)
            .prop('disabled', false);
    showNotification(response.message || 'Failed to update status', 'error');
            }
        },
    error: function(xhr) {
        button.attr('class', originalClass)
            .html(originalHtml)
            .prop('disabled', false);
    console.error('Error:', xhr.responseText);
    showNotification('Network error occurred', 'error');
        }
    });
}

    function deleteVenue(venueId) {
    const button = $(`.delete-venue-btn[data-venue-id="${venueId}"]`);
    const row = button.closest('tr');
    const originalHtml = button.html();

    // Show loading state
    button.html('<i class="fas fa-spinner fa-spin"></i>').prop('disabled', true);

    $.ajax({
        url: '/VenueManagement/Delete',
    type: 'POST',
    data: {
        id: venueId,
    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
    success: function(response) {
            if (response.success) {
        row.fadeOut(300, function () {
            $(this).remove();

            // Update total count if it exists
            const totalElement = $('.stat-value');
            if (totalElement.length) {
                const currentTotal = parseInt(totalElement.text()) || 0;
                totalElement.text(Math.max(0, currentTotal - 1));
            }

            // Check if table is empty
            if ($('.venues-table tbody tr:visible').length === 0) {
                location.reload();
            }
        });
    showNotification(response.message, 'success');
            } else {
        button.html(originalHtml).prop('disabled', false);
    showNotification(response.message || 'Failed to delete venue', 'error');
            }
        },
    error: function(xhr) {
        button.html(originalHtml).prop('disabled', false);
    console.error('Error:', xhr.responseText);
    showNotification('Network error occurred', 'error');
        }
    });
}

    function showNotification(message, type) {
        // Remove existing notifications
        $('.alert-notification').remove();

    const alertClass = type === 'success' ? 'alert-success' :
    type === 'error' ? 'alert-danger' : 'alert-info';

    const notification = $(`
    <div class="alert ${alertClass} alert-dismissible fade show alert-notification"
        role="alert" style="position: fixed; top: 20px; right: 20px; z-index: 9999; min-width: 300px;">
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
    `);

    $('body').append(notification);

    // Auto dismiss after 4 seconds
    setTimeout(() => {
        notification.alert('close');
    }, 4000);
}
</script>