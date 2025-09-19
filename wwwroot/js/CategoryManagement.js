// Global variables
let categoryToDelete = {
    id: null, name: ''
}

;

$(document).ready(function() {
    initializeCategoryManagement();
});

function initializeCategoryManagement() {
    // Initialize tooltips initializeTooltips();
    // Initialize search functionality initializeSearch();
    // Initialize delete modals initializeDeleteModal();
    // Initialize form validations initializeFormValidations();
    // Initialize auto-save for forms initializeAutoSave();
    // Initialize keyboard shortcuts initializeKeyboardShortcuts();
    // Initialize responsive sidebar initializeResponsiveSidebar();
    // Initialize loading states initializeLoadingStates();
    console .log('CategoryManagement initialized successfully');
}

function initializeTooltips() {
    // Initialize Bootstrap tooltips var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

function initializeSearch() {
    // Real-time search functionality let searchTimeout;
    const searchInput = $('#searchTerm, input[name="searchTerm"]');
    if (searchInput.length)

{
    // Add search icon and clear button if (!searchInput.closest('.search-container').length)

{
    searchInput .wrap('<div class="search-container position-relative"></div>');
    searchInput .after('<span class="search-clear-btn" style="display: none;">&times;</span>');
}

// Handle input changes
searchInput.on('input', function() {
            const $this = $(this);
            const clearBtn = $this.siblings('.search-clear-btn');
            
            if ($this.val().length > 0) {
                clearBtn.show();
            } else {
                clearBtn.hide();
            }
            
            // Debounce search
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(function() {
                if ($this.val().length >= 2 || $this.val().length === 0) {
                    // Auto-submit search form
                    $this.closest('form').submit();
                }
            }, 500);
        });

// Handle clear button click
$('.search-clear-btn').on('click', function() {
            searchInput.val('').trigger('input');
        });
}
}

function initializeDeleteModal() {
    const deleteModal = $('#deleteModal');
    const confirmDeleteBtn = $('#confirmDelete');
    if (deleteModal.length)

{
    confirmDeleteBtn .on('click', function() {
            if (categoryToDelete.id) {
                performDelete(categoryToDelete.id);
            }
        });
}

}

function deleteCategory(categoryId, categoryName) {
    categoryToDelete =

{
    id: categoryId, name: categoryName
}

;
$('#categoryNameToDelete').text(categoryName);
$('#deleteModal').modal('show');
}

function performDelete(categoryId) {
    const confirmBtn = $('#confirmDelete');
    const originalText = confirmBtn.html();
    // Show loading state confirmBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Deleting...');
    $.ajax({
        url: '/CategoryManagement/Delete/' + categoryId,
        type: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: function(response) {
            if (response.success) {
                $('#deleteModal').modal('hide');
                showNotification('success', response.message);
                
                // Remove the row from table or reload page
                setTimeout(function() {
                    location.reload();
                }, 1000);
            } else {
                showNotification('error', response.message);
            }
        },
        error: function(xhr, status, error) {
            showNotification('error', 'An error occurred while deleting the category. Please try again.');
            console.error('Delete error:', error);
        },
        complete: function() {
            confirmBtn.prop('disabled', false).html(originalText);
        }
    });
}

function initializeFormValidations() {
    // Real-time validation for category forms const categoryNameInput = $('#CategoryName');
    const descriptionInput = $('#Description');
    if (categoryNameInput.length)

{
    categoryNameInput .on('blur', function() {
            validateCategoryName($(this));
        });
    categoryNameInput .on('input', function() {
            const $this = $(this);
            clearValidationError($this);
            
            // Character counter
            updateCharacterCounter($this, 100);
        });
}

if (descriptionInput.length) {
    descriptionInput .on('input', function() {
            updateCharacterCounter($(this), 1000);
        });
}

// Form submission validation
$('.category-form').on('submit', function(e) {
        return validateCategoryForm($(this));
    });
}

function validateCategoryName($input) {
    const value = $input.val().trim();
    const minLength = 2;
    const maxLength = 100;
    clearValidationError($input);
    if (value.length < minLength)

{
    showValidationError($input, `Category name must be at least ${minLength} characters long.`);
    return false;
}

if (value.length > maxLength) {
    showValidationError($input, `Category name cannot exceed ${maxLength} characters.`);
    return false;
}

// Check for special characters (basic validation)
const validPattern = /^[a-zA-Z0-9\s\-&] + $/;
if (!validPattern.test(value)) {
    showValidationError($input, 'Category name contains invalid characters.');
    return false;
}

return true;
}

function validateCategoryForm($form) {
    let isValid = true;
    // Validate category name const categoryNameInput = $form.find('#CategoryName');
    if (categoryNameInput.length && !validateCategoryName(categoryNameInput))

{
    isValid = false;
}

// Additional validations can be added here

if (!isValid) {
    showNotification('error', 'Please correct the errors below before submitting.');
    return false;
}

return true;
}

function showValidationError($input, message) {
    clearValidationError($input);
    $input.addClass('is-invalid');
    $input.after(`<div class="invalid-feedback">${message}</div>`);
}

function clearValidationError($input) {
    $input.removeClass('is-invalid');
    $input.siblings('.invalid-feedback').remove();
}

function updateCharacterCounter($input, maxLength) {
    const current = $input.val().length;
    const remaining = maxLength - current;
    let counterElement = $input.siblings('.char-counter');
    if (!counterElement.length)

{
    counterElement = $('<small class="form-text text-muted char-counter"></small>');
    $input.after(counterElement);
}

counterElement.text(`${remaining} characters remaining`);

if (remaining < 20) {
    counterElement .removeClass('text-muted text-success').addClass('text-warning');
}

else if (remaining < 0) {
    counterElement .removeClass('text-muted text-warning').addClass('text-danger');
}

else {
    counterElement .removeClass('text-warning text-danger').addClass('text-muted');
}

}

function initializeAutoSave() {
    // Auto-save draft functionality for forms const formInputs = $('.category-form input, .category-form textarea');
    let autoSaveTimeout;
    if (formInputs.length)

{
    formInputs .on('input', function() {
            clearTimeout(autoSaveTimeout);
            autoSaveTimeout = setTimeout(function() {
                saveDraft();
            }, 2000);
        });
}

}

function saveDraft() {
    const formData =

{
}

;
$('.category-form input, .category-form textarea').each(function() {
        const $this = $(this);
        if ($this.attr('name') && $this.attr('name') !== '__RequestVerificationToken') {
            formData[$this.attr('name')] = $this.val();
        }
    });

localStorage.setItem('categoryFormDraft', JSON.stringify(formData));
showNotification('info', 'Draft saved automatically', 2000);
}

function loadDraft() {
    const draft = localStorage.getItem('categoryFormDraft');
    if (draft)

{
    try

{
    const formData = JSON.parse(draft);
    Object .keys(formData).forEach(function(key) {
                const input = $(`[name="${key}"]`);
                if (input.length && !input.val()) {
                    input.val(formData[key]);
                }
            });
    showNotification('info', 'Draft loaded', 2000);
}

catch (e) {
    console .error('Error loading draft:', e);
}

}
}

function clearDraft() {
    localStorage .removeItem('categoryFormDraft');
}

function initializeKeyboardShortcuts() {
    $(document).on('keydown', function(e) {
        // Ctrl/Cmd + S to save form
        if ((e.ctrlKey || e.metaKey) && e.key === 's') {
            e.preventDefault();
            const submitBtn = $('.category-form button[type="submit"]');
            if (submitBtn.length && !submitBtn.prop('disabled')) {
                submitBtn.click();
            }
        }
        
        // Escape to close modals
        if (e.key === 'Escape') {
            $('.modal.show').modal('hide');
        }
        
        // Ctrl/Cmd + N for new category
        if ((e.ctrlKey || e.metaKey) && e.key === 'n' && window.location.pathname.includes('CategoryManagement')) {
            e.preventDefault();
            window.location.href = '/CategoryManagement/Create';
        }
    });
}

function initializeResponsiveSidebar() {
    // Mobile sidebar toggle const sidebarToggle = $('<button class="sidebar-toggle d-md-none"><i class="fas fa-bars"></i></button>');
    $('.content-header .header-left').prepend(sidebarToggle);
    sidebarToggle .on('click', function() {
        $('.admin-sidebar').toggleClass('active');
    });
    // Close sidebar when clicking outside on mobile $(document).on('click', function(e) {
        if ($(window).width() < 768) {
            if (!$(e.target).closest('.admin-sidebar, .sidebar-toggle').length) {
                $('.admin-sidebar').removeClass('active');
            }
        }
    });
}

function initializeLoadingStates() {
    // Show loading states for AJAX requests $(document).ajaxStart(function() {
        $('body').addClass('ajax-loading');
    }).ajaxStop(function() {
        $('body').removeClass('ajax-loading');
    });
    // Add loading overlay styles if (!$('#loadingOverlay').length)

{
    $('body').append(` <div id="loadingOverlay" class="loading-overlay" style="display: none;"> <div class="loading-spinner"> <i class="fas fa-spinner fa-spin fa-2x"></i> <p>Loading...</p> </div> </div> `);
}

}

function showNotification(type, message, duration = 5000) {
    const alertClass = getAlertClass(type);
    const icon = getAlertIcon(type);
    const notification = $(`
        <div class="alert ${alertClass} alert-dismissible fade show notification-alert" role="alert">
            <i class="fas ${icon} me-2"></i>${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `);
    // Remove existing notifications of the same type $(`.notification-alert.${alertClass}`).alert('close');
    // Add new notification $('.content-body').prepend(notification);
    // Auto-dismiss after duration if (duration > 0)

{
    setTimeout(function() {
            notification.alert('close');
        }, duration);
}

// Animate in
notification.hide().slideDown(300);
}

function getAlertClass(type) {
    const classMap =

{
    'success': 'alert-success', 'error': 'alert-danger', 'warning': 'alert-warning', 'info': 'alert-info'
}

;
return classMap[type] || 'alert-info';
}

function getAlertIcon(type) {
    const iconMap =

{
    'success': 'fa-check-circle', 'error': 'fa-exclamation-circle', 'warning': 'fa-exclamation-triangle', 'info': 'fa-info-circle'
}

;
return iconMap[type] || 'fa-info-circle';
}

// Utility functions
function formatDate(date) {
    return new Date(date).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

function truncateText(text, maxLength) {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args)

{
    const later = () =>

{
    clearTimeout(timeout);
    func(...args);
}

;
clearTimeout(timeout);
timeout = setTimeout(later, wait);
}
;
}

// Export functions for external use
window.CategoryManagement = {
    deleteCategory: deleteCategory, showNotification: showNotification, validateCategoryName: validateCategoryName, saveDraft: saveDraft, loadDraft: loadDraft, clearDraft: clearDraft
}

;

// Initialize when page loads
$(document).ready(function() {
    // Load draft if on create/edit page
    if (window.location.pathname.includes('/Create') || window.location.pathname.includes('/Edit')) {
        setTimeout(loadDraft, 500);
        
        // Clear draft on successful form submission
        $('.category-form').on('submit', function() {
            setTimeout(clearDraft, 1000);
        });
    }
});
