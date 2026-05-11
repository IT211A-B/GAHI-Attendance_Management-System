/* ============================================================
   Toast Notification System – Client Library
   Provides showToast() for manual calls, plus auto‑display
   of any toasts passed via TempData on page load.
   ============================================================ */

(function () {
  "use strict";

  // ── Icon characters for each type ──────────────────────────
  var ICONS = {
    success: "\u2713",  // ✓
    error:   "\u2717",  // ✗
    warning: "\u26A0",  // ⚠
    info:    "\u2139"   // ℹ
  };

  // ── Default duration per type (ms) ─────────────────────────
  var DEFAULT_DURATION = {
    success: 4000,
    error:   6000,
    warning: 5000,
    info:    3500
  };

  // ── Retrieve or create the toast container ─────────────────
  function getContainer() {
    var container = document.getElementById("toast-container");
    if (!container) {
      container = document.createElement("div");
      container.id = "toast-container";
      container.className = "toast-container";
      container.setAttribute("aria-live", "polite");
      container.setAttribute("aria-relevant", "additions");
      document.body.appendChild(container);
    }
    return container;
  }

  // ── Remove a single toast element with animation ───────────
  function dismissToast(toastEl, delayMs) {
    var leaveDelay = typeof delayMs === "number" ? delayMs : 300;
    setTimeout(function () {
      toastEl.classList.add("is-leaving");
      // Remove from DOM after animation completes
      setTimeout(function () {
        if (toastEl.parentNode) {
          toastEl.parentNode.removeChild(toastEl);
        }
      }, 280);
    }, leaveDelay);
  }

  // ── Show a toast notification ──────────────────────────────
  // type: "success" | "error" | "warning" | "info"
  // message: string – displayed text
  // options (optional): { title, duration, dismissible, onDismiss }
  function showToast(type, message, options) {
    if (!message || typeof message !== "string") {
      return;
    }

    var validTypes = ["success", "error", "warning", "info"];
    if (validTypes.indexOf(type) < 0) {
      type = "info";
    }

    var config = (typeof options === "object" && options !== null) ? options : {};
    var title = typeof config.title === "string" ? config.title : "";
    var duration = typeof config.duration === "number" ? config.duration : (DEFAULT_DURATION[type] || 4000);
    var dismissible = config.dismissible !== false;
    var container = getContainer();

    // Build the toast element
    var toastEl = document.createElement("div");
    toastEl.className = "toast toast-" + type;
    toastEl.setAttribute("role", "alert");

    // Icon
    var iconEl = document.createElement("span");
    iconEl.className = "toast-icon";
    iconEl.textContent = ICONS[type] || ICONS.info;

    // Body
    var bodyEl = document.createElement("div");
    bodyEl.className = "toast-body";

    if (title) {
      var titleEl = document.createElement("p");
      titleEl.className = "toast-title";
      titleEl.textContent = title;
      bodyEl.appendChild(titleEl);
    }

    var msgEl = document.createElement("p");
    msgEl.className = "toast-message";
    msgEl.textContent = message;
    bodyEl.appendChild(msgEl);

    toastEl.appendChild(iconEl);
    toastEl.appendChild(bodyEl);

    // Dismiss button
    if (dismissible) {
      var dismissBtn = document.createElement("button");
      dismissBtn.type = "button";
      dismissBtn.className = "toast-dismiss";
      dismissBtn.setAttribute("aria-label", "Dismiss notification");
      dismissBtn.textContent = "\u2715"; // ✕
      dismissBtn.addEventListener("click", function () {
        clearTimeout(toastEl._hideTimer);
        if (typeof config.onDismiss === "function") {
          config.onDismiss();
        }
        dismissToast(toastEl, 0);
      });
      toastEl.appendChild(dismissBtn);
    }

    container.appendChild(toastEl);

    // Auto‑dismiss after duration
    if (duration > 0) {
      toastEl._hideTimer = setTimeout(function () {
        dismissToast(toastEl, 0);
      }, duration);
    }

    return toastEl;
  }

  // ── Display a structured API error as a toast ──────────────
  // Accepts the result object returned by requestApi().
  function showApiError(apiResult) {
    if (!apiResult || apiResult.ok !== false) {
      return;
    }

    showToast("error", apiResult.message || "An unexpected error occurred.", {
      title: apiResult.errorCode || "Error"
    });
  }

  // ── Convenience wrappers ───────────────────────────────────
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