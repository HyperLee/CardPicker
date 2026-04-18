(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        enhanceValidationForms();
    });

    function enhanceValidationForms() {
        const forms = document.querySelectorAll("[data-card-form]");

        forms.forEach((form) => {
            parseUnobtrusiveValidation(form);
            wireFormValidationState(form);
        });
    }

    function parseUnobtrusiveValidation(form) {
        if (!window.jQuery?.validator?.unobtrusive) {
            return;
        }

        window.jQuery.validator.unobtrusive.parse(form);
    }

    function wireFormValidationState(form) {
        const fields = form.querySelectorAll("input[name], select[name], textarea[name]");

        fields.forEach((field) => {
            const refresh = () => refreshFieldState(field);

            ["input", "change", "blur"].forEach((eventName) => {
                field.addEventListener(eventName, refresh);
            });

            refresh();
        });

        form.addEventListener("submit", () => {
            window.setTimeout(() => {
                refreshFormState(form);
                focusFirstInvalidField(form);
            }, 0);
        });

        form.addEventListener("reset", () => {
            window.setTimeout(() => refreshFormState(form), 0);
        });

        refreshFormState(form);
    }

    function refreshFormState(form) {
        const fields = form.querySelectorAll("input[name], select[name], textarea[name]");

        fields.forEach((field) => refreshFieldState(field));

        const summary = form.querySelector("[data-validation-summary]");
        if (!summary) {
            return;
        }

        const hasSummaryMessage = (summary.textContent ?? "").trim().length > 0;
        summary.classList.toggle("validation-summary--active", hasSummaryMessage);

        if (hasSummaryMessage) {
            summary.setAttribute("tabindex", "-1");
        } else {
            summary.removeAttribute("tabindex");
        }
    }

    function refreshFieldState(field) {
        const validationMessage = getValidationMessageElement(field);
        if (!validationMessage) {
            return;
        }

        ensureDescribedBy(field, validationMessage.id);

        const hasValidationError =
            (validationMessage.textContent ?? "").trim().length > 0 ||
            validationMessage.classList.contains("field-validation-error");

        field.setAttribute("aria-invalid", hasValidationError ? "true" : "false");
        validationMessage.classList.toggle("field-validation-message--active", hasValidationError);
    }

    function getValidationMessageElement(field) {
        const fieldName = field.getAttribute("name");
        if (!fieldName) {
            return null;
        }

        return field.form?.querySelector(`[data-valmsg-for="${escapeSelector(fieldName)}"]`) ?? null;
    }

    function ensureDescribedBy(field, targetId) {
        if (!targetId) {
            return;
        }

        const existingIds = (field.getAttribute("aria-describedby") ?? "")
            .split(/\s+/)
            .filter((value) => value.length > 0);

        if (!existingIds.includes(targetId)) {
            existingIds.push(targetId);
            field.setAttribute("aria-describedby", existingIds.join(" "));
        }
    }

    function focusFirstInvalidField(form) {
        const firstInvalidField = form.querySelector("[aria-invalid='true']");
        firstInvalidField?.focus();
    }

    function escapeSelector(value) {
        if (window.CSS?.escape) {
            return window.CSS.escape(value);
        }

        return value.replace(/["\\]/g, "\\$&");
    }
})();
