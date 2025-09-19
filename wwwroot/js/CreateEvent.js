class CreateEventManager {
    constructor() {
        this.categoryIndex = 0;
        this.init();
    }

    init() {
        this.bindEvents();
        this.setDefaultDates();
        // Only add initial ticket category if none exist
        if (document.querySelectorAll('.ticket-category-item').length === 0) {
            this.addInitialTicketCategory();
        }
        // Set categoryIndex based on existing categories
        this.categoryIndex = document.querySelectorAll('.ticket-category-item').length;
    }

    bindEvents() {
        // Form submission
        const form = document.getElementById('createEventForm');
        if (form) {
            form.addEventListener('submit', (e) => {
                console.log('Form submit triggered');
                this.handleFormSubmit(e);
            });
        }

        // Add ticket category button
        const addCategoryBtn = document.getElementById('addCategoryBtn');
        if (addCategoryBtn) {
            addCategoryBtn.addEventListener('click', () => {
                console.log('Adding new ticket category');
                this.addTicketCategory();
            });
        }

        // Image URL preview
        const imageUrlInput = document.querySelector('input[name="ImageUrl"]');
        if (imageUrlInput) {
            imageUrlInput.addEventListener('input', (e) => {
                this.previewImage(e.target.value);
            });
        }

        // Date validation
        const eventDateInput = document.querySelector('input[name="EventDate"]');
        const endDateInput = document.querySelector('input[name="EndDate"]');

        if (eventDateInput) {
            eventDateInput.addEventListener('change', (e) => {
                this.validateEventDate(e.target.value);
            });
        }

        if (endDateInput) {
            endDateInput.addEventListener('change', (e) => {
                this.validateEndDate(e.target.value);
            });
        }

        // Real-time validation
        this.setupRealTimeValidation();
    }

    setDefaultDates() {
        const now = new Date();
        const eventDate = new Date(now.getTime() + (7 * 24 * 60 * 60 * 1000)); // 7 days from now
        const endDate = new Date(eventDate.getTime() + (3 * 60 * 60 * 1000)); // 3 hours after event start

        const eventDateInput = document.querySelector('input[name="EventDate"]');
        const endDateInput = document.querySelector('input[name="EndDate"]');

        if (eventDateInput && !eventDateInput.value) {
            eventDateInput.value = this.formatDateTime(eventDate);
        }

        if (endDateInput && !endDateInput.value) {
            endDateInput.value = this.formatDateTime(endDate);
        }
    }

    formatDateTime(date) {
        return date.toISOString().slice(0, 16);
    }

    addInitialTicketCategory() {
        this.addTicketCategory();
    }

    addTicketCategory() {
        const template = document.getElementById('ticketCategoryTemplate');
        const container = document.getElementById('ticketCategoriesContainer');

        if (!template || !container) {
            console.error('Template or container not found');
            return;
        }

        const clone = template.content.cloneNode(true);
        const categoryItem = clone.querySelector('.ticket-category-item');

        // Set unique index
        categoryItem.setAttribute('data-index', this.categoryIndex);

        // Update input names with proper indexing
        const inputs = clone.querySelectorAll('input, textarea');
        inputs.forEach(input => {
            if (input.name && input.name.includes('[]')) {
                input.name = input.name.replace('[]', `[${this.categoryIndex}]`);
                console.log(`Set input name: ${input.name}`);
            }
        });

        // Add validation listeners to new inputs
        this.addValidationListeners(clone);

        container.appendChild(clone);
        this.categoryIndex++;

        // Focus on the first input of the new category
        const firstInput = categoryItem.querySelector('input[type="text"]');
        if (firstInput) {
            setTimeout(() => firstInput.focus(), 100);
        }
    }

    removeTicketCategory(button) {
        const categoryItem = button.closest('.ticket-category-item');
        const container = document.getElementById('ticketCategoriesContainer');

        // Don't allow removal if it's the only category
        if (container.children.length <= 1) {
            this.showAlert('error', 'At least one ticket category is required.');
            return;
        }

        // Add fade out animation
        categoryItem.style.opacity = '0';
        categoryItem.style.transform = 'translateY(-10px)';

        setTimeout(() => {
            categoryItem.remove();
            this.reindexTicketCategories();
        }, 300);
    }

    reindexTicketCategories() {
        const categories = document.querySelectorAll('.ticket-category-item');
        categories.forEach((category, index) => {
            category.setAttribute('data-index', index);

            const inputs = category.querySelectorAll('input, textarea');
            inputs.forEach(input => {
                if (input.name && input.name.match(/\[\d+\]/)) {
                    input.name = input.name.replace(/\[\d+\]/, `[${index}]`);
                    console.log(`Reindexed input name to: ${input.name}`);
                }
            });
        });
        this.categoryIndex = categories.length;
    }

    previewImage(url) {
        const preview = document.getElementById('imagePreview');
        const container = document.querySelector('.image-preview-container');

        if (!preview || !container) return;

        if (url && this.isValidUrl(url)) {
            preview.src = url;
            preview.onload = () => {
                container.style.display = 'block';
            };
            preview.onerror = () => {
                container.style.display = 'none';
            };
        } else {
            container.style.display = 'none';
        }
    }

    isValidUrl(string) {
        try {
            new URL(string);
            return true;
        } catch (_) {
            return false;
        }
    }

    validateEventDate(dateValue) {
        if (!dateValue) return true;

        const eventDate = new Date(dateValue);
        const now = new Date();
        const input = document.querySelector('input[name="EventDate"]');

        if (eventDate <= now) {
            this.setFieldError(input, 'Event date must be in the future.');
            return false;
        } else {
            this.clearFieldError(input);
            // Update end date validation if necessary
            const endDateInput = document.querySelector('input[name="EndDate"]');
            if (endDateInput && endDateInput.value) {
                this.validateEndDate(endDateInput.value);
            }
            return true;
        }
    }

    validateEndDate(dateValue) {
        if (!dateValue) return true; // End date is optional

        const endDate = new Date(dateValue);
        const eventDateInput = document.querySelector('input[name="EventDate"]');

        if (!eventDateInput || !eventDateInput.value) return true;

        const eventDate = new Date(eventDateInput.value);
        const input = document.querySelector('input[name="EndDate"]');

        if (endDate <= eventDate) {
            this.setFieldError(input, 'End date must be after the event start date.');
            return false;
        } else {
            this.clearFieldError(input);
            return true;
        }
    }

    addValidationListeners(container) {
        const inputs = container.querySelectorAll('input[required], textarea[required], select[required]');
        inputs.forEach(input => {
            input.addEventListener('input', () => {
                if (input.value.trim() === '') {
                    this.setFieldError(input, 'This field is required.');
                } else if (input.type === 'number' && input.name.includes('Price') && parseFloat(input.value) <= 0) {
                    this.setFieldError(input, 'Price must be greater than 0.');
                } else if (input.type === 'number' && input.name.includes('TotalQuantity') && parseInt(input.value) <= 0) {
                    this.setFieldError(input, 'Quantity must be at least 1.');
                } else {
                    this.clearFieldError(input);
                }
            });
        });
    }

    setFieldError(input, message) {
        input.classList.add('is-invalid');
        let feedback = input.parentElement.querySelector('.invalid-feedback');
        if (feedback) {
            feedback.textContent = message;
        }
    }

    clearFieldError(input) {
        input.classList.remove('is-invalid');
        let feedback = input.parentElement.querySelector('.invalid-feedback');
        if (feedback) {
            feedback.textContent = '';
        }
    }

    showAlert(type, message) {
        const errorAlert = document.getElementById('errorAlert');
        const successAlert = document.getElementById('successAlert');

        if (type === 'error') {
            document.getElementById('errorMessage').textContent = message;
            errorAlert.style.display = 'block';
            successAlert.style.display = 'none';
        } else {
            document.getElementById('successMessage').textContent = message;
            successAlert.style.display = 'block';
            errorAlert.style.display = 'none';
        }

        setTimeout(() => {
            errorAlert.style.display = 'none';
            successAlert.style.display = 'none';
        }, 5000);
    }

    handleFormSubmit(e) {
        let valid = true;

        // Validate event date
        const eventDateInput = document.querySelector('input[name="EventDate"]');
        if (eventDateInput && !this.validateEventDate(eventDateInput.value)) {
            valid = false;
        }

        // Validate end date
        const endDateInput = document.querySelector('input[name="EndDate"]');
        if (endDateInput && !this.validateEndDate(endDateInput.value)) {
            valid = false;
        }

        // Validate required inputs
        const requiredInputs = document.querySelectorAll('#createEventForm input[required], #createEventForm select[required], #createEventForm textarea[required]');
        requiredInputs.forEach(input => {
            if (input.value.trim() === '') {
                this.setFieldError(input, 'This field is required.');
                valid = false;
            } else {
                this.clearFieldError(input);
            }
        });

        // Validate ticket categories
        const ticketCategories = document.querySelectorAll('.ticket-category-item');
        ticketCategories.forEach(category => {
            const priceInput = category.querySelector('input[name*="Price"]');
            const quantityInput = category.querySelector('input[name*="TotalQuantity"]');
            if (priceInput && parseFloat(priceInput.value) <= 0) {
                this.setFieldError(priceInput, 'Price must be greater than 0.');
                valid = false;
            }
            if (quantityInput && parseInt(quantityInput.value) <= 0) {
                this.setFieldError(quantityInput, 'Quantity must be at least 1.');
                valid = false;
            }
        });

        if (!valid) {
            e.preventDefault();
            this.showAlert('error', 'Please correct the errors in the form.');
            console.log('Form validation failed');
        } else {
            console.log('Form validation passed, submitting form...');
            this.showAlert('success', 'Submitting event...');
            // Allow form to submit naturally
        }
    }

    setupRealTimeValidation() {
        const inputs = document.querySelectorAll('#createEventForm input[required], #createEventForm select[required], #createEventForm textarea[required]');
        inputs.forEach(input => {
            input.addEventListener('input', () => {
                if (input.value.trim() === '') {
                    this.setFieldError(input, 'This field is required.');
                } else {
                    this.clearFieldError(input);
                }
            });
        });
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const eventManager = new CreateEventManager();
    window.removeTicketCategory = function (button) {
        eventManager.removeTicketCategory(button);
    };
});