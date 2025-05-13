// Support ticket functionality
document.addEventListener('DOMContentLoaded', function() {
    // Get current template title if we're on a template page
    let templateTitle = '';
    const templateTitleElement = document.querySelector('.template-title');
    if (templateTitleElement) {
        templateTitle = templateTitleElement.textContent.trim();
    }

    // Set template value in form if it exists
    const templateInput = document.querySelector('input[name="Template"]');
    if (templateInput && templateTitle) {
        templateInput.value = templateTitle;
    }

    // Handle form submission
    const supportForm = document.getElementById('support-form');
    if (supportForm) {
        supportForm.addEventListener('submit', function(event) {
            const summaryInput = document.querySelector('textarea[name="Summary"]');
            const priorityInput = document.querySelector('select[name="Priority"]');
            
            // Basic validation
            let isValid = true;
            
            if (!summaryInput.value.trim()) {
                isValid = false;
                showError(summaryInput, 'Please provide a summary');
            } else {
                hideError(summaryInput);
            }
            
            if (!priorityInput.value) {
                isValid = false;
                showError(priorityInput, 'Please select a priority');
            } else {
                hideError(priorityInput);
            }
            
            if (!isValid) {
                event.preventDefault();
            }
        });
    }
    
    // Helper functions for form validation
    function showError(element, message) {
        const errorSpan = element.nextElementSibling;
        if (errorSpan && errorSpan.classList.contains('text-danger')) {
            errorSpan.textContent = message;
        } else {
            const span = document.createElement('span');
            span.classList.add('text-danger');
            span.textContent = message;
            element.parentNode.insertBefore(span, element.nextSibling);
        }
        element.classList.add('is-invalid');
    }
    
    function hideError(element) {
        const errorSpan = element.nextElementSibling;
        if (errorSpan && errorSpan.classList.contains('text-danger')) {
            errorSpan.textContent = '';
        }
        element.classList.remove('is-invalid');
    }
}); 