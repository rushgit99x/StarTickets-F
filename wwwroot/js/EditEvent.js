// EditEvent.js - Event Organizer Edit Event functionality

$(document).ready(function () {
    initializeEditEventForm();
});

let ticketCategoryIndex = 0;

function initializeEditEventForm() {
    // Initialize ticket category counter
    updateTicketCategoryIndex();

    // Set up form validation
    setupFormValidation();

    // Set up event handlers
    setupEventHandlers();

    // Initialize datetime inputs with minimum date
    setMinimumDates();

    // Setup auto-save functionality (optional)
    // setupAutoSave();
}

function updateTicketCategoryIndex() {
    const existingCategories = document.querySelectorAll('.ticket-category-item');
    ticketCategoryIndex = existingCategories.length;
}

function setupFormValidation() {
    const form = document.getElementById('editEventForm');

    // Custom validation rules
    if (form) {
        form.addEventListener('submit', function (e) {
            if (!validateForm()) {
                e.preventDefault();
                e.stopPropagation();
            }
        });
    }

    // Real-time validation for specific fields
    setupFieldValidation();
}

function setupFieldValidation() {
    // Event name validation
    const eventNameInput = document.getElementById('EventName');
    if (eventNameInput) {
        eventNameInput.addEventListener('blur', function () {
            validateEventName(this.value);
        });
    }

    // Date validation
    const eventDateInput = document.getElementById('EventDate');
    const endDateInput = document.getElementById('EndDate');

    if (eventDateInput && endDateInput) {
        eventDateInput.addEventListener('change', function () {
            validateEventDates();
        });

        endDateInput.addEventListener('change', function () {
            validateEventDates();
        });
    }

    // Price validation for existing ticket categories
    document.querySelectorAll('input[name*="Price"]').forEach(input => {
        input.addEventListener('blur', function () {
            validatePrice(this);
        });
    });

    // Quantity validation for existing ticket categories
    document.querySelectorAll('input[name*="Quantity"]').forEach(input => {
        input.addEventListener('blur', function () {
            validateQuantity(this);
        });
    });
}

function setupEventHandlers() {
    // Form submission with confirmation
    const form = document.getElementById('editEventForm');
    const saveButton = document.getElementById('saveEventBtn');

    if (form && saveButton) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            showConfirmationModal();
        });
    }

    // Confirmation modal handler
    const confirmButton = document.getElementById('confirmUpdateBtn');
    if (confirmButton) {
        confirmButton.addEventListener('click', function () {
            submitForm();
        });
    }

    // Image URL preview
    const imageUrlInput = document.getElementById('imageUrlInput');
    if (imageUrlInput) {
        imageUrlInput.addEventListener('blur', function () {
            previewImage();
        });
    }

    // Auto-update available quantity when total quantity changes
    document.addEventListener('input', function (e) {
        if (e.target.name && e.target.name.includes('TotalQuantity')) {
            updateAvailableQuantity(e.target);
        }
    });
}

function setMinimumDates() {
    const now = new Date();
    const minDateTime = now.toISOString().slice(0, 16);

    const eventDateInput = document.getElementById('EventDate');
    const endDateInput = document.getElementById('EndDate');

    if (eventDateInput) {
        eventDateInput.setAttribute('min', minDateTime);
    }

    if (endDateInput) {
        endDateInput.setAttribute('min', minDateTime);
    }
}

function validateForm() {
    let isValid = true;
    const errors = [];

    // Clear previous errors
    clearValidationErrors();

    // Validate event name
    const eventName = document.getElementById('EventName')?.value.trim();
    if (!eventName) {
        errors.push('Event name is required');
        showFieldError('EventName', 'Event name is required');
        isValid = false;
    } else if (eventName.length < 3) {
        errors.push('Event name must be at least 3 characters long');
        showFieldError('EventName', 'Event name must be at least 3 characters long');
        isValid = false;
    }

    // Validate category
    const categoryId = document.getElementById('CategoryId')?.value;
    if (!categoryId) {
        errors.push('Please select a category');
        showFieldError('CategoryId', 'Please select a category');
        isValid = false;
    }

    // Validate venue
    const venueId = document.getElementById('VenueId')?.value;
    if (!venueId) {
        errors.push('Please select a venue');
        showFieldError('VenueId', 'Please select a venue');
        isValid = false;
    }

    // Validate dates
    if (!validateEventDates()) {
        isValid = false;
    }

    // Validate ticket categories
    if (!validateTicketCategories()) {
        isValid = false;
    }

    // Show general error if validation failed
    if (!isValid) {
        showAlert('Please fix the validation errors before submitting.', 'error');
    }

    return isValid;
}

function validateEventName(name) {
    const trimmedName = name.trim();
    if (!trimmedName) {
        showFieldError('EventName', 'Event name is required');
        return false;
    } else if (trimmedName.length < 3) {
        showFieldError('EventName', 'Event name must be at least 3 characters long');
        return false;
    } else {
        clearFieldError('EventName');
        return true;
    }
}

function validateEventDates() {
    const eventDateInput = document.getElementById('EventDate');
    const endDateInput = document.getElementById('EndDate');

    if (!eventDateInput || !endDateInput) return true;

    const eventDate = new Date(eventDateInput.value);
    const endDate = new Date(endDateInput.value);
    const now = new Date();

    let isValid = true;

    // Check if event date is in the future
    if (eventDate <= now) {
        showFieldError('EventDate', 'Event date must be in the future');
        isValid = false;
    } else {
        clearFieldError('EventDate');
    }

    // Check if end date is after event date
    if (endDate && eventDate && endDate <= eventDate) {
        showFieldError('EndDate', 'End date must be after the event start date');
        isValid = false;
    } else if (endDate) {
        clearFieldError('EndDate');
    }

    return isValid;
}

function validateTicketCategories() {
    const categories = document.querySelectorAll('.ticket-category-item');
    let isValid = true;

    if (categories.length === 0) {
        showAlert('Please add at least one ticket category', 'error');
        return false;
    }

    categories.forEach((category, index) => {
        const nameInput = category.querySelector('input[name*="CategoryName"]');
        const priceInput = category.querySelector('input[name*="Price"]');
        const totalQuantityInput = category.querySelector('input[name*="TotalQuantity"]');

        // Validate category name
        if (!nameInput?.value.trim()) {
            showFieldError(nameInput, 'Category name is required');
            isValid = false;
        }

        // Validate price
        const price = parseFloat(priceInput?.value);
        if (isNaN(price) || price < 0) {
            showFieldError(priceInput, 'Please enter a valid price');
            isValid = false;
        }

        // Validate quantity
        const quantity = parseInt(totalQuantityInput?.value);
        if (isNaN(quantity) || quantity < 1) {
            showFieldError(totalQuantityInput, 'Please enter a valid quantity (minimum 1)');
            isValid = false;
        }
    });

    return isValid;
}

function validatePrice(input) {
    const price = parseFloat(input.value);
    if (isNaN(price) || price < 0) {
        showFieldError(input, 'Please enter a valid price');
        return false;
    } else {
        clearFieldError(input);
        return true;
    }
}

function validateQuantity(input) {
    const quantity = parseInt(input.value);
    const min = input.name.includes('Available') ? 0 : 1;

    if (isNaN(quantity) || quantity < min) {
        const message = min === 0 ? 'Please enter a valid quantity (minimum 0)' : 'Please enter a valid quantity (minimum 1)';
        showFieldError(input, message);
        return false;
    } else {
        clearFieldError(input);
        return true;
    }
}

function addTicketCategory() {
    const container = document.getElementById('ticketCategoriesContainer');
    if (!container) return;

    const categoryHtml = `
        <div class="ticket-category-item" data-index="${ticketCategoryIndex}">
            <input type="hidden" name="TicketCategories[${ticketCategoryIndex}].TicketCategoryId" value="0" />
            
            <div class="category-header">
                <h5>Ticket Category #${ticketCategoryIndex + 1}</h5>
                <button type="button" class="btn btn-remove" onclick="removeTicketCategory(this)">
                    <i class="fas fa-trash"></i>
                </button>
            </div>

            <div class="category-grid">
                <div class="form-group">
                    <label class="form-label required">
                        <i class="fas fa-tag"></i>
                        Category Name
                    </label>
                    <input name="TicketCategories[${ticketCategoryIndex}].CategoryName" class="form-control" placeholder="e.g., VIP, General Admission" />
                    <span class="validation-error"></span>
                </div>

                <div class="form-group">
                    <label class="form-label required">
                        <i class="fas fa-dollar-sign"></i>
                        Price ($)
                    </label>
                    <input name="TicketCategories[${ticketCategoryIndex}].Price" class="form-control" type="number" step="0.01" min="0" />
                    <span class="validation-error"></span>
                </div>

                <div class="form-group">
                    <label class="form-label required">
                        <i class="fas fa-sort-numeric-up"></i>
                        Total Quantity
                    </label>
                    <input name="TicketCategories[${ticketCategoryIndex}].TotalQuantity" class="form-control" type="number" min="1" />
                    <span class="validation-error"></span>
                </div>

                <div class="form-group">
                    <label class="form-label">
                        <i class="fas fa-check-circle"></i>
                        Available Quantity
                    </label>
                    <input name="TicketCategories[${ticketCategoryIndex}].AvailableQuantity" class="form-control" type="number" min="0" />
                    <span class="validation-error"></span>
                </div>

                <div class="form-group col-span-2">
                    <label class="form-label">
                        <i class="fas fa-info-circle"></i>
                        Description
                    </label>
                    <textarea name="TicketCategories[${ticketCategoryIndex}].Description" class="form-control" rows="2" placeholder="Optional description for this ticket category"></textarea>
                    <span class="validation-error"></span>
                </div>

                <div class="form-group col-span-2">
                    <div class="form-check">
                        <input name="TicketCategories[${ticketCategoryIndex}].IsActive" type="checkbox" class="form-check-input" value="true" checked />
                        <input name="TicketCategories[${ticketCategoryIndex}].IsActive" type="hidden" value="false" />
                        <label class="form-check-label">
                            Category is Active
                        </label>
                    </div>
                </div>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', categoryHtml);

    // Setup validation for new inputs
    const newCategory = container.lastElementChild;
    const priceInput = newCategory.querySelector('input[name*="Price"]');
    const quantityInputs = newCategory.querySelectorAll('input[name*="Quantity"]');

    if (priceInput) {
        priceInput.addEventListener('blur', function () {
            validatePrice(this);
        });
    }

    quantityInputs.forEach(input => {
        input.addEventListener('blur', function () {
            validateQuantity(this);
        });
    });

    // Setup auto-update for available quantity
    const totalQuantityInput = newCategory.querySelector('input[name*="TotalQuantity"]');
    if (totalQuantityInput) {
        totalQuantityInput.addEventListener('input', function () {
            updateAvailableQuantity(this);
        });
    }

    ticketCategoryIndex++;

    // Scroll to the new category
    newCategory.scrollIntoView({ behavior: 'smooth', block: 'center' });

    // Focus on the category name input
    const nameInput = newCategory.querySelector('input[name*="CategoryName"]');
    if (nameInput) {
        nameInput.focus();
    }
}

function removeTicketCategory(button) {
    const categoryItem = button.closest('.ticket-category-item');
    if (!categoryItem) return;

    // Show confirmation for removal
    if (confirm('Are you sure you want to remove this ticket category?')) {
        categoryItem.remove();

        // Reindex remaining categories
        reindexTicketCategories();

        // Update counter
        updateTicketCategoryIndex();

        showAlert('Ticket category removed successfully', 'success');
    }
}

function reindexTicketCategories() {
    const categories = document.querySelectorAll('.ticket-category-item');

    categories.forEach((category, index) => {
        // Update data-index
        category.setAttribute('data-index', index);

        // Update header number
        const header = category.querySelector('.category-header h5');
        if (header) {
            header.textContent = `Ticket Category #${index + 1}`;
        }

        // Update input names and IDs
        const inputs = category.querySelectorAll('input, textarea');
        inputs.forEach(input => {
            const name = input.getAttribute('name');
            if (name && name.includes('TicketCategories[')) {
                const newName = name.replace(/TicketCategories\[\d+\]/, `TicketCategories[${index}]`);
                input.setAttribute('name', newName);
            }
        });
    });
}

function updateAvailableQuantity(totalQuantityInput) {
    const categoryItem = totalQuantityInput.closest('.ticket-category-item');
    if (!categoryItem) return;

    const availableQuantityInput = categoryItem.querySelector('input[name*="AvailableQuantity"]');
    if (availableQuantityInput) {
        const totalQuantity = parseInt(totalQuantityInput.value) || 0;
        // Only update if available quantity is currently empty or greater than total
        if (!availableQuantityInput.value || parseInt(availableQuantityInput.value) > totalQuantity) {
            availableQuantityInput.value = totalQuantity;
        }
    }
}

function previewImage() {
    const imageUrlInput = document.getElementById('imageUrlInput');
    const imagePreview = document.getElementById('imagePreview');
    const previewImg = document.getElementById('previewImg');

    if (!imageUrlInput || !imagePreview || !previewImg) return;

    const imageUrl = imageUrlInput.value.trim();

    if (imageUrl) {
        previewImg.src = imageUrl;
        previewImg.onload = function () {
            imagePreview.style.display = 'block';
        };
        previewImg.onerror = function () {
            imagePreview.style.display = 'none';
            showAlert('Unable to load image from the provided URL', 'warning');
        };
    } else {
        imagePreview.style.display = 'none';
    }
}

function showConfirmationModal() {
    const modal = new bootstrap.Modal(document.getElementById('confirmationModal'));
    modal.show();
}

function submitForm() {
    const form = document.getElementById('editEventForm');
    if (!form) return;

    // Show loading state
    const submitButtons = document.querySelectorAll('button[type="submit"], #confirmUpdateBtn');
    submitButtons.forEach(btn => {
        btn.disabled = true;
        const icon = btn.querySelector('i');
        if (icon) {
            icon.className = 'fas fa-spinner fa-spin';
        }
        btn.innerHTML = btn.innerHTML.replace(/Update Event|Save/, 'Updating...');
    });

    // Hide confirmation modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('confirmationModal'));
    if (modal) {
        modal.hide();
    }

    // Submit the form
    form.submit();
}

function resetForm() {
    if (confirm('Are you sure you want to reset all changes? This will restore the form to its original state.')) {
        // Reload the page to reset form
        window.location.reload();
    }
}

// Utility functions for showing/hiding alerts and field errors
function showAlert(message, type = 'info') {
    const alertClass = type === 'error' ? 'alert-danger' : `alert-${type}`;
    const iconClass = type === 'error' ? 'fa-exclamation-circle' :
        type === 'success' ? 'fa-check-circle' :
            type === 'warning' ? 'fa-exclamation-triangle' : 'fa-info-circle';

    // Remove existing alerts
    document.querySelectorAll('.alert.show').forEach(alert => {
        alert.classList.remove('show');
        setTimeout(() => alert.remove(), 300);
    });

    // Create new alert
    const alertHtml = `
        <div class="alert ${alertClass} alert-dismissible fade show" role="alert" style="position: fixed; top: 20px; right: 20px; z-index: 1060; min-width: 300px;">
            <i class="fas ${iconClass}"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;

    document.body.insertAdjacentHTML('afterbegin', alertHtml);

    // Auto-dismiss after 5 seconds for success messages
    if (type === 'success') {
        setTimeout(() => {
            const alert = document.querySelector('.alert.show');
            if (alert) {
                bootstrap.Alert.getInstance(alert)?.close();
            }
        }, 5000);
    }
}

function showFieldError(fieldOrName, message) {
    let field;
    if (typeof fieldOrName === 'string') {
        field = document.getElementById(fieldOrName) || document.querySelector(`[name="${fieldOrName}"]`);
    } else {
        field = fieldOrName;
    }

    if (!field) return;

    // Add error class to field
    field.classList.add('is-invalid');

    // Find or create error message element
    let errorElement = field.parentElement.querySelector('.validation-error');
    if (!errorElement) {
        errorElement = document.createElement('span');
        errorElement.className = 'validation-error';
        field.parentElement.appendChild(errorElement);
    }

    errorElement.textContent = message;
    errorElement.style.display = 'block';
}

function clearFieldError(fieldOrName) {
    let field;
    if (typeof fieldOrName === 'string') {
        field = document.getElementById(fieldOrName) || document.querySelector(`[name="${fieldOrName}"]`);
    } else {
        field = fieldOrName;
    }

    if (!field) return;

    field.classList.remove('is-invalid');
    const errorElement = field.parentElement.querySelector('.validation-error');
    if (errorElement) {
        errorElement.style.display = 'none';
        errorElement.textContent = '';
    }
}

function clearValidationErrors() {
    // Remove all error classes
    document.querySelectorAll('.is-invalid').forEach(field => {
        field.classList.remove('is-invalid');
    });

    // Hide all error messages
    document.querySelectorAll('.validation-error').forEach(error => {
        error.style.display = 'none';
        error.textContent = '';
    });
}

// Auto-save functionality (optional - uncomment to enable)
/*
function setupAutoSave() {
    let autoSaveTimer;
    const form = document.getElementById('editEventForm');
    
    if (form) {
        form.addEventListener('input', function() {
            clearTimeout(autoSaveTimer);
            autoSaveTimer = setTimeout(() => {
                autoSaveFormData();
            }, 30000); // Auto-save every 30 seconds after user stops typing
        });
    }
}

function autoSaveFormData() {
    const formData = new FormData(document.getElementById('editEventForm'));
    const data = Object.fromEntries(formData.entries());
    
    // Save to sessionStorage
    sessionStorage.setItem('editEventAutoSave', JSON.stringify(data));
    
    // Show auto-save indicator
    showAlert('Form auto-saved', 'success');
}

function loadAutoSavedData() {
    const saved = sessionStorage.getItem('editEventAutoSave');
    if (saved) {
        const data = JSON.parse(saved);
        Object.keys(data).forEach(key => {
            const field = document.querySelector(`[name="${key}"]`);
            if (field) {
                field.value = data[key];
            }
        });
    }
}
*/