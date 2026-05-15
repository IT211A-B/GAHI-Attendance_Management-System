/* ============================================================
   Toast Notification System – Client Library
   ============================================================ 
   Defense Prep: This file handles the popup notifications (toasts)
   seen in the top-right corner. It provides a pure Vanilla JS
   way to show alerts (success, error, warning, info) without 
   relying on bulky libraries. It includes smooth CSS transitions,
   auto-dismissal timeouts, and an API error wrapper. 
*/

(function () {
  "use strict";

  // ── Icon characters for each type ──────────────────────────
  // These mapping icons give visual context to the toast message.
  var ICONS = {
    success: "\u2713",  // ✓ (Check mark for success)
    error:   "\u2717",  // ✗ (Cross mark for errors)
    warning: "\u26A0",  // ⚠ (Warning triangle)
    info:    "\u2139"   // ℹ (Information icon)
  };

  // ── Default duration per type (ms) ─────────────────────────
  // Different severities get different reading times before auto-hiding.
  var DEFAULT_DURATION = {
    success: 4000, // 4s for quick success updates
    error:   6000, // 6s for errors (gives users time to read)
    warning: 5000,
    info:    3500
  };

  // ── Retrieve or create the toast container ─────────────────
  // Defense Prep: Checks if our fixed toast wrapper exists in the DOM.
  // If not, it creates it dynamically and appends it to the body.
  function getContainer() {
    var container = document.getElementById("toast-container");
    if (!container) {
      container = document.createElement("div");
      container.id = "toast-container";
      container.className = "toast-container";
      
      // Accessibility (A11y): screen readers will read new additions aloud
      container.setAttribute("aria-live", "polite");
      container.setAttribute("aria-relevant", "additions");
      document.body.appendChild(container);
    }
    return container;
  }

  // ── Remove a single toast element with animation ───────────
  // Defense Prep: Initiates the fade-out CSS animation by adding 'is-leaving'.
  // We use setTimeout to wait for the CSS animation to complete (280ms)
  // before physically removing the node from the DOM to avoid jagged jumps.
  function dismissToast(toastEl, delayMs) {
    var leaveDelay = typeof delayMs === "number" ? delayMs : 300;
    setTimeout(function () {
      toastEl.classList.add("is-leaving"); // Trigger CSS fade out
      // Remove from DOM after CSS transition completes
      setTimeout(function () {
        if (toastEl.parentNode) {
          toastEl.parentNode.removeChild(toastEl);
        }
      }, 280);
    }, leaveDelay);
  }

  // ── Show a toast notification ──────────────────────────────
  // Core function to build the DOM elements for the toast.
  // type: "success" | "error" | "warning" | "info"
  // message: string – displayed text
  // options (optional): { title, duration, dismissible, onDismiss }
  function showToast(type, message, options) {
    if (!message || typeof message !== "string") {
      return;
    }

    var validTypes = ["success", "error", "warning", "info"];
    if (validTypes.indexOf(type) < 0) {
      type = "info"; // Fallback to 'info' if an unknown type is passed
    }

    var config = (typeof options === "object" && options !== null) ? options : {};
    var title = typeof config.title === "string" ? config.title : "";
    var duration = typeof config.duration === "number" ? config.duration : (DEFAULT_DURATION[type] || 4000);
    var dismissible = config.dismissible !== false;
    var container = getContainer();

    // Build the main toast wrapper element
    var toastEl = document.createElement("div");
    toastEl.className = "toast toast-" + type; // Applies color variants (e.g. .toast-success)
    toastEl.setAttribute("role", "alert"); // A11y

    // Icon container
    var iconEl = document.createElement("span");
    iconEl.className = "toast-icon";
    iconEl.textContent = ICONS[type] || ICONS.info;

    // Body text container
    var bodyEl = document.createElement("div");
    bodyEl.className = "toast-body";

    // Only append title block if one is provided
    if (title) {
      var titleEl = document.createElement("p");
      titleEl.className = "toast-title";
      titleEl.textContent = title;
      bodyEl.appendChild(titleEl);
    }

    // Append the primary message text
    var msgEl = document.createElement("p");
    msgEl.className = "toast-message";
    msgEl.textContent = message;
    bodyEl.appendChild(msgEl);

    // Assemble the parts
    toastEl.appendChild(iconEl);
    toastEl.appendChild(bodyEl);

    // Dismiss button setup (X button)
    if (dismissible) {
      var dismissBtn = document.createElement("button");
      dismissBtn.type = "button";
      dismissBtn.className = "toast-dismiss";
      dismissBtn.setAttribute("aria-label", "Dismiss notification");
      dismissBtn.textContent = "\u2715"; // ✕
      
      // Stop the auto-hide timer and trigger dismissal immediately on click
      dismissBtn.addEventListener("click", function () {
        clearTimeout(toastEl._hideTimer);
        if (typeof config.onDismiss === "function") {
          config.onDismiss();
        }
        dismissToast(toastEl, 0);
      });
      toastEl.appendChild(dismissBtn);
    }

    // Inject into the DOM
    container.appendChild(toastEl);

    // Setup auto-dismiss after the defined duration passes
    if (duration > 0) {
      toastEl._hideTimer = setTimeout(function () {
        dismissToast(toastEl, 0);
      }, duration);
    }

    return toastEl;
  }

  // ── Display a structured API error as a toast ──────────────
  // Defense Prep: Connects nicely with `requestApi()` in site.js.
  // When an AJAX fetch call fails, this parses the result object 
  // and surfaces it to the user.
  function showApiError(apiResult) {
    if (!apiResult || apiResult.ok !== false) {
      return;
    }

    showToast("error", apiResult.message || "An unexpected error occurred.", {
      title: apiResult.errorCode || "Error"
    });
  }

  // ── Convenience wrappers ───────────────────────────────────
  // Helper functions exposed for cleaner code calling elsewhere
  function showSuccess(message, options) {
    return showToast("success", message, options);
  }

  function showError(message, options) {
    return showToast("error", message, options);
  }

  function showWarning(message, options) {
    return showToast("warning", message, options);
  }

  function showInfo(message, options) {
    return showToast("info", message, options);
  }

  // ── Auto‑display any toasts queued via TempData ────────────
  function processInitialToasts() {
    var container = document.getElementById("toast-container");
    if (!container) {
      return;
    }

    var types = ["success", "error", "warning", "info"];
    types.forEach(function (type) {
      var raw = container.getAttribute("data-toast-" + type);
      if (raw) {
        showToast(type, raw);
        container.removeAttribute("data-toast-" + type);
      }
    });
  }

  // ── Expose public API globally ─────────────────────────────
  window.amsToast = {
    show:       showToast,
    showApiError: showApiError,
    success:    showSuccess,
    error:      showError,
    warning:    showWarning,
    info:       showInfo,
    dismiss:    dismissToast
  };

  // ── Bootstrap on DOMContentLoaded ──────────────────────────
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", processInitialToasts);
  } else {
    processInitialToasts();
  }
})();