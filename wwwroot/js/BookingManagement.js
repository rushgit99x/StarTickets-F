class BookingManagement {
    constructor() {
        this.init();
        this.bindEvents();
        this.setupEnhancements();
    }

    init() {
        console.log('Booking Management initialized');
        this.showLoadingOverlay(false);
        this.animatePageLoad();
        this.setupTooltips();
    }

    bindEvents() {
        // Filter form auto-submit on change
        this.setupAutoFilter();

        // Enhanced search functionality
        this.setupSearchEnhancements();

        // Table interactions
        this.setupTableInteractions();

        // Keyboard shortcuts
        this.setupKeyboardShortcuts();

        // Enhanced pagination
        this.setupPaginationEnhancements();
    }

    setupEnhancements() {
        // Add smooth scrolling
        this.addSmoothScrolling();

        // Setup data refresh
        this.setupDataRefresh();

        // Add loading states
        this.setupLoadingStates();

        // Setup notifications
        this.setupNotifications();
    }

    // Loading overlay management
    showLoadingOverlay(show = true) {
        let overlay = document.querySelector('.loading-overlay');

        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'loading-overlay';
            overlay.innerHTML = `
                <div class="loading-spinner"></div>
            `;
            document.body.appendChild(overlay);
        }

        if (show) {
            overlay.classList.add('active');
        } else {
            overlay.classList.remove('active');
        }
    }

    // Page load animation
    animatePageLoad() {
        // Animate cards with stagger
        const cards = document.querySelectorAll('.dashboard-card, .action-bar');
        cards.forEach((card, index) => {
            card.style.opacity = '0';
            card.style.transform = 'translateY(30px)';

            setTimeout(() => {
                card.style.transition = 'all 0.6s cubic-bezier(0.4, 0, 0.2, 1)';
                card.style.opacity = '1';
                card.style.transform = 'translateY(0)';
            }, index * 100);
        });

        // Animate table rows
        setTimeout(() => {
            const rows = document.querySelectorAll('tbody tr');
            rows.forEach((row, index) => {
                row.style.opacity = '0';
                row.style.transform = 'translateX(-20px)';

                setTimeout(() => {
                    row.style.transition = 'all 0.4s ease';
                    row.style.opacity = '1';
                    row.style.transform = 'translateX(0)';
                }, index * 50);
            });
        }, 300);
    }

    // Auto-filter setup
    setupAutoFilter() {
        const filterForm = document.querySelector('.action-bar form');
        const selects = filterForm.querySelectorAll('select');

        selects.forEach(select => {
            select.addEventListener('change', (e) => {
                this.showLoadingOverlay(true);

                // Add a small delay for better UX
                setTimeout(() => {
                    filterForm.submit();
                }, 300);
            });
        });
    }

    // Enhanced search functionality
    setupSearchEnhancements() {
        const searchInput = document.querySelector('input[name="search"]');
        if (!searchInput) return;

        let searchTimeout;

        // Add search suggestions
        const searchContainer = searchInput.closest('.input-group');
        const suggestionsDiv = document.createElement('div');
        suggestionsDiv.className = 'search-suggestions';
        suggestionsDiv.style.cssText = `
            position: absolute;
            top: 100%;
            left: 0;
            right: 0;
            background: white;
            border-radius: 12px;
            box-shadow: 0 8px 32px rgba(0,0,0,0.1);
            border: 1px solid #e2e8f0;
            z-index: 1000;
            max-height: 300px;
            overflow-y: auto;
            display: none;
        `;
        searchContainer.style.position = 'relative';
        searchContainer.appendChild(suggestionsDiv);

        // Real-time search (debounced)
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            const query = e.target.value.trim();

            if (query.length < 2) {
                suggestionsDiv.style.display = 'none';
                return;
            }

            searchTimeout = setTimeout(() => {
                this.performLiveSearch(query, suggestionsDiv);
            }, 300);
        });

        // Hide suggestions when clicking outside
        document.addEventListener('click', (e) => {
            if (!searchContainer.contains(e.target)) {
                suggestionsDiv.style.display = 'none';
            }
        });

        // Clear search functionality
        const clearButton = document.createElement('button');
        clearButton.type = 'button';
        clearButton.className = 'btn-clear-search';
        clearButton.innerHTML = '<i class="fas fa-times"></i>';
        clearButton.style.cssText = `
            position: absolute;
            right: 10px;
            top: 50%;
            transform: translateY(-50%);
            background: none;
            border: none;
            color: #64748b;
            cursor: pointer;
            display: none;
            z-index: 10;
        `;

        searchContainer.appendChild(clearButton);

        clearButton.addEventListener('click', () => {
            searchInput.value = '';
            clearButton.style.display = 'none';
            searchInput.focus();
            suggestionsDiv.style.display = 'none';
        });

        searchInput.addEventListener('input', (e) => {
            clearButton.style.display = e.target.value ? 'block' : 'none';
        });

        // Show clear button if search has value on load
        if (searchInput.value) {
            clearButton.style.display = 'block';
        }
    }

    // Live search functionality
    performLiveSearch(query, suggestionsDiv) {
        // This would typically make an AJAX call to get search suggestions
        // For now, we'll simulate with static data
        const mockSuggestions = [
            'REF001234',
            'John Doe',
            'Concert Event',
            'Payment Completed',
            'Active Booking'
        ].filter(item => item.toLowerCase().includes(query.toLowerCase()));

        if (mockSuggestions.length > 0) {
            suggestionsDiv.innerHTML = mockSuggestions.map(suggestion => `
                <div class="suggestion-item" style="padding: 0.75rem 1rem; cursor: pointer; border-bottom: 1px solid #f1f5f9;">
                    <i class="fas fa-search me-2" style="color: #64748b;"></i>
                    ${suggestion}
                </div>
            `).join('');

            suggestionsDiv.style.display = 'block';

            // Add click handlers to suggestions
            suggestionsDiv.querySelectorAll('.suggestion-item').forEach(item => {
                item.addEventListener('click', () => {
                    const searchInput = document.querySelector('input[name="search"]');
                    searchInput.value = item.textContent.trim();
                    suggestionsDiv.style.display = 'none';
                    searchInput.closest('form').submit();
                });

                item.addEventListener('mouseenter', () => {
                    item.style.background = '#f8fafc';
                });

                item.addEventListener('mouseleave', () => {
                    item.style.background = 'white';
                });
            });
        } else {
            suggestionsDiv.style.display = 'none';
        }
    }

    // Table interactions
    setupTableInteractions() {
        const table = document.querySelector('.table');
        if (!table) return;

        // Row selection (for future bulk actions)
        this.setupRowSelection();

        // Enhanced row hover effects
        this.setupRowHoverEffects();

        // Quick preview on row click
        this.setupQuickPreview();
    }

    setupRowSelection() {
        const tbody = document.querySelector('tbody');
        if (!tbody) return;

        // Add checkbox column header
        const headerRow = document.querySelector('thead tr');
        const selectAllTh = document.createElement('th');
        headerRow.insertBefore(selectAllTh, headerRow.firstChild);

        // Add checkbox to each row
        const rows = tbody.querySelectorAll('tr');
        rows.forEach((row, index) => {
            const checkbox = document.createElement('td');
            row.insertBefore(checkbox, row.firstChild);
        });

        // Select all functionality
        const selectAllCheckbox = document.getElementById('selectAll');
        const rowCheckboxes = document.querySelectorAll('.row-select');

        selectAllCheckbox.addEventListener('change', (e) => {
            rowCheckboxes.forEach(cb => {
                cb.checked = e.target.checked;
                this.toggleRowSelection(cb.closest('tr'), cb.checked);
            });
            this.updateBulkActions();
        });

        rowCheckboxes.forEach(cb => {
            cb.addEventListener('change', (e) => {
                this.toggleRowSelection(e.target.closest('tr'), e.target.checked);
                this.updateSelectAllState();
                this.updateBulkActions();
            });
        });
    }

    toggleRowSelection(row, selected) {
        if (selected) {
            row.classList.add('table-row-selected');
            row.style.background = 'linear-gradient(135deg, rgba(59, 130, 246, 0.1), rgba(139, 92, 246, 0.1))';
        } else {
            row.classList.remove('table-row-selected');
            row.style.background = '';
        }
    }

    updateSelectAllState() {
        const selectAllCheckbox = document.getElementById('selectAll');
        const rowCheckboxes = document.querySelectorAll('.row-select');
        const checkedCount = Array.from(rowCheckboxes).filter(cb => cb.checked).length;

        if (checkedCount === 0) {
            selectAllCheckbox.indeterminate = false;
            selectAllCheckbox.checked = false;
        } else if (checkedCount === rowCheckboxes.length) {
            selectAllCheckbox.indeterminate = false;
            selectAllCheckbox.checked = true;
        } else {
            selectAllCheckbox.indeterminate = true;
        }
    }

    updateBulkActions() {
        const selectedCount = document.querySelectorAll('.row-select:checked').length;

        // Show/hide bulk actions bar
        let bulkActionsBar = document.querySelector('.bulk-actions-bar');

        if (selectedCount > 0 && !bulkActionsBar) {
            bulkActionsBar = document.createElement('div');
            bulkActionsBar.className = 'bulk-actions-bar';
            bulkActionsBar.style.cssText = `
                position: fixed;
                bottom: 20px;
                left: 50%;
                transform: translateX(-50%);
                background: linear-gradient(135deg, #1e293b, #334155);
                color: white;
                padding: 1rem 2rem;
                border-radius: 16px;
                box-shadow: 0 8px 32px rgba(0,0,0,0.2);
                display: flex;
                align-items: center;
                gap: 1rem;
                z-index: 1000;
                animation: slideUp 0.3s ease;
            `;

            bulkActionsBar.innerHTML = `
                <span class="selected-count">${selectedCount} selected</span>
                <button class="btn btn-sm btn-outline-light">Export</button>
                <button class="btn btn-sm btn-outline-danger">Delete</button>
                <button class="btn btn-sm btn-outline-light" onclick="bookingManager.clearSelection()">
                    <i class="fas fa-times"></i>
                </button>
            `;

            document.body.appendChild(bulkActionsBar);
        } else if (selectedCount > 0 && bulkActionsBar) {
            bulkActionsBar.querySelector('.selected-count').textContent = `${selectedCount} selected`;
        } else if (selectedCount === 0 && bulkActionsBar) {
            bulkActionsBar.remove();
        }
    }

    clearSelection() {
        const rowCheckboxes = document.querySelectorAll('.row-select');
        const selectAllCheckbox = document.getElementById('selectAll');

        rowCheckboxes.forEach(cb => {
            cb.checked = false;
            this.toggleRowSelection(cb.closest('tr'), false);
        });

        if (selectAllCheckbox) {
            selectAllCheckbox.checked = false;
            selectAllCheckbox.indeterminate = false;
        }

        this.updateBulkActions();
    }

    setupRowHoverEffects() {
        const rows = document.querySelectorAll('tbody tr');

        rows.forEach(row => {
            row.addEventListener('mouseenter', () => {
                if (!row.classList.contains('table-row-selected')) {
                    row.style.transform = 'scale(1.01)';
                    row.style.zIndex = '10';
                    row.style.boxShadow = '0 8px 25px rgba(0,0,0,0.1)';
                }
            });

            row.addEventListener('mouseleave', () => {
                if (!row.classList.contains('table-row-selected')) {
                    row.style.transform = '';
                    row.style.zIndex = '';
                    row.style.boxShadow = '';
                }
            });
        });
    }

    setupQuickPreview() {
        const detailButtons = document.querySelectorAll('a[href*="Details"]');

        detailButtons.forEach(button => {
            button.addEventListener('mouseenter', (e) => {
                // Show quick preview tooltip
                this.showQuickPreview(e.target, button.closest('tr'));
            });

            button.addEventListener('mouseleave', () => {
                this.hideQuickPreview();
            });
        });
    }

    showQuickPreview(button, row) {
        const existingPreview = document.querySelector('.quick-preview');
        if (existingPreview) existingPreview.remove();

        const cells = row.querySelectorAll('td');
        const bookingRef = cells[1]?.textContent || 'N/A';
        const customer = cells[2]?.textContent || 'N/A';
        const event = cells[3]?.textContent || 'N/A';
        const amount = cells[5]?.textContent || 'N/A';

        const preview = document.createElement('div');
        preview.className = 'quick-preview';
        preview.style.cssText = `
            position: absolute;
            background: linear-gradient(135deg, #1e293b, #334155);
            color: white;
            padding: 1rem;
            border-radius: 12px;
            box-shadow: 0 8px 32px rgba(0,0,0,0.3);
            z-index: 1000;
            min-width: 250px;
            opacity: 0;
            transform: translateY(10px);
            transition: all 0.3s ease;
            pointer-events: none;
        `;

        preview.innerHTML = `
            <h6 style="margin-bottom: 0.5rem; color: #3b82f6;">${bookingRef}</h6>
            <p style="margin: 0.25rem 0; font-size: 0.9rem;"><strong>Customer:</strong> ${customer}</p>
            <p style="margin: 0.25rem 0; font-size: 0.9rem;"><strong>Event:</strong> ${event}</p>
            <p style="margin: 0.25rem 0; font-size: 0.9rem;"><strong>Amount:</strong> ${amount}</p>
            <small style="color: #94a3b8;">Click to view full details</small>
        `;

        document.body.appendChild(preview);

        const buttonRect = button.getBoundingClientRect();
        preview.style.left = `${buttonRect.left}px`;
        preview.style.top = `${buttonRect.top - preview.offsetHeight - 10}px`;

        requestAnimationFrame(() => {
            preview.style.opacity = '1';
            preview.style.transform = 'translateY(0)';
        });
    }

    hideQuickPreview() {
        const preview = document.querySelector('.quick-preview');
        if (preview) {
            preview.style.opacity = '0';
            preview.style.transform = 'translateY(10px)';
            setTimeout(() => preview.remove(), 300);
        }
    }

    // Keyboard shortcuts
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Ctrl/Cmd + F - Focus search
            if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
                e.preventDefault();
                const searchInput = document.querySelector('input[name="search"]');
                if (searchInput) {
                    searchInput.focus();
                    searchInput.select();
                }
            }

            // Ctrl/Cmd + R - Refresh data
            if ((e.ctrlKey || e.metaKey) && e.key === 'r') {
                e.preventDefault();
                this.refreshData();
            }

            // Escape - Clear selection and close modals
            if (e.key === 'Escape') {
                this.clearSelection();
                this.hideQuickPreview();
                const suggestions = document.querySelector('.search-suggestions');
                if (suggestions) suggestions.style.display = 'none';
            }

            // Ctrl/Cmd + A - Select all (when table is focused)
            if ((e.ctrlKey || e.metaKey) && e.key === 'a' && document.activeElement.closest('.table')) {
                e.preventDefault();
                const selectAllCheckbox = document.getElementById('selectAll');
                if (selectAllCheckbox) {
                    selectAllCheckbox.checked = !selectAllCheckbox.checked;
                    selectAllCheckbox.dispatchEvent(new Event('change'));
                }
            }
        });
    }

    // Enhanced pagination
    setupPaginationEnhancements() {
        const paginationLinks = document.querySelectorAll('.page-link');

        paginationLinks.forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                this.showLoadingOverlay(true);

                // Add loading state to clicked link
                const originalText = link.innerHTML;
                link.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

                setTimeout(() => {
                    window.location.href = link.href;
                }, 500);
            });
        });

        // Add keyboard navigation for pagination
        document.addEventListener('keydown', (e) => {
            if (e.target.closest('.table')) {
                if (e.key === 'ArrowLeft' && e.ctrlKey) {
                    const prevLink = document.querySelector('.page-link[href*="page=' + (getCurrentPage() - 1) + '"]');
                    if (prevLink) prevLink.click();
                } else if (e.key === 'ArrowRight' && e.ctrlKey) {
                    const nextLink = document.querySelector('.page-link[href*="page=' + (getCurrentPage() + 1) + '"]');
                    if (nextLink) nextLink.click();
                }
            }
        });
    }

    // Smooth scrolling
    addSmoothScrolling() {
        const links = document.querySelectorAll('a[href^="#"]');
        links.forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const target = document.querySelector(link.getAttribute('href'));
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }

    // Data refresh functionality
    setupDataRefresh() {
        // Add refresh button to action bar
        const actionBar = document.querySelector('.action-bar');
        if (actionBar) {
            const refreshButton = document.createElement('button');
            refreshButton.type = 'button';
            refreshButton.className = 'btn btn-outline-secondary';
            refreshButton.innerHTML = '<i class="fas fa-sync-alt"></i> Refresh';
            refreshButton.style.marginLeft = 'auto';

            refreshButton.addEventListener('click', () => {
                this.refreshData();
            });

            const form = actionBar.querySelector('form');
            form.appendChild(refreshButton);
        }

        // Auto-refresh every 5 minutes (optional)
        setInterval(() => {
            this.refreshData(true);
        }, 300000);
    }

    refreshData(silent = false) {
        if (!silent) {
            this.showLoadingOverlay(true);
        }

        const refreshIcon = document.querySelector('.fa-sync-alt');
        if (refreshIcon) {
            refreshIcon.style.animation = 'spin 1s linear infinite';
        }

        // In a real application, this would make an AJAX call
        setTimeout(() => {
            if (!silent) {
                window.location.reload();
            }

            if (refreshIcon) {
                refreshIcon.style.animation = '';
            }

            this.showNotification('Data refreshed successfully', 'success');
        }, 1000);
    }

    // Loading states for buttons and forms
    setupLoadingStates() {
        const forms = document.querySelectorAll('form');

        forms.forEach(form => {
            form.addEventListener('submit', (e) => {
                const submitButton = form.querySelector('button[type="submit"]');
                if (submitButton) {
                    const originalText = submitButton.innerHTML;
                    submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
                    submitButton.disabled = true;

                    // Re-enable after 5 seconds as fallback
                    setTimeout(() => {
                        submitButton.innerHTML = originalText;
                        submitButton.disabled = false;
                    }, 5000);
                }
            });
        });
    }

    // Notification system
    setupNotifications() {
        // Create notification container
        if (!document.querySelector('.notification-container')) {
            const container = document.createElement('div');
            container.className = 'notification-container';
            container.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                z-index: 9999;
                max-width: 400px;
            `;
            document.body.appendChild(container);
        }
    }

    showNotification(message, type = 'info', duration = 5000) {
        const container = document.querySelector('.notification-container');
        if (!container) return;

        const notification = document.createElement('div');
        notification.className = `alert alert-${type} notification-toast`;
        notification.style.cssText = `
            margin-bottom: 10px;
            border-radius: 12px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
            opacity: 0;
            transform: translateX(100%);
            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            position: relative;
            overflow: hidden;
        `;

        const iconMap = {
            success: 'check-circle',
            error: 'exclamation-triangle',
            warning: 'exclamation-circle',
            info: 'info-circle'
        };

        notification.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="fas fa-${iconMap[type]} me-2"></i>
                <span>${message}</span>
                <button type="button" class="btn-close ms-auto" onclick="this.parentElement.parentElement.remove()"></button>
            </div>
        `;

        container.appendChild(notification);

        // Animate in
        requestAnimationFrame(() => {
            notification.style.opacity = '1';
            notification.style.transform = 'translateX(0)';
        });

        // Auto remove
        setTimeout(() => {
            notification.style.opacity = '0';
            notification.style.transform = 'translateX(100%)';
            setTimeout(() => notification.remove(), 300);
        }, duration);
    }

    // Tooltip setup
    setupTooltips() {
        const tooltipElements = document.querySelectorAll('[title]');

        tooltipElements.forEach(element => {
            const title = element.getAttribute('title');
            element.removeAttribute('title');

            let tooltip;

            element.addEventListener('mouseenter', () => {
                tooltip = document.createElement('div');
                tooltip.className = 'custom-tooltip';
                tooltip.style.cssText = `
                    position: absolute;
                    background: #1e293b;
                    color: white;
                    padding: 0.5rem 1rem;
                    border-radius: 8px;
                    font-size: 0.85rem;
                    z-index: 1000;
                    opacity: 0;
                    transition: opacity 0.3s ease;
                    pointer-events: none;
                    white-space: nowrap;
                `;
                tooltip.textContent = title;
                document.body.appendChild(tooltip);

                const rect = element.getBoundingClientRect();
                tooltip.style.left = `${rect.left + (rect.width / 2) - (tooltip.offsetWidth / 2)}px`;
                tooltip.style.top = `${rect.top - tooltip.offsetHeight - 8}px`;

                requestAnimationFrame(() => {
                    tooltip.style.opacity = '1';
                });
            });

            element.addEventListener('mouseleave', () => {
                if (tooltip) {
                    tooltip.style.opacity = '0';
                    setTimeout(() => tooltip.remove(), 300);
                }
            });
        });
    }

    // Export functionality
    exportData(format = 'csv') {
        const selectedRows = document.querySelectorAll('.row-select:checked');
        const allRows = document.querySelectorAll('tbody tr');
        const rowsToExport = selectedRows.length > 0 ?
            Array.from(selectedRows).map(cb => cb.closest('tr')) :
            Array.from(allRows);

        this.showLoadingOverlay(true);
        this.showNotification(`Exporting ${rowsToExport.length} records as ${format.toUpperCase()}...`, 'info');

        // Simulate export delay
        setTimeout(() => {
            this.showLoadingOverlay(false);
            this.showNotification('Export completed successfully!', 'success');
        }, 2000);
    }
}

// Helper functions
function getCurrentPage() {
    const activePageLink = document.querySelector('.page-item.active .page-link');
    return activePageLink ? parseInt(activePageLink.textContent) : 1;
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.bookingManager = new BookingManagement();
});

// Export for global access
window.BookingManagement = BookingManagement;