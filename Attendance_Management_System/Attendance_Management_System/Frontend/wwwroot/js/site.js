// IIFE to avoid polluting global namespace with internal functions and variables.
(function () {
	// Session storage key for preserving scroll position across form postbacks.
	var postbackScrollStateKey = "ams.postback-scroll";
	// Scroll state expires after 15 seconds to prevent stale restoration.
	var postbackScrollStateMaxAgeMs = 15000;

	// Check if user prefers reduced motion for accessibility.
	function prefersReducedMotion() {
		return window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
	}

	// Verify that the Anime.js animation library is available.
	function supportsAnime() {
		return typeof window.anime === "function";
	}

	// Animate multiple elements in a staggered reveal sequence.
	function revealSequence(selector, options) {
		var nodes = document.querySelectorAll(selector);
		if (!nodes.length) {
			return;
		}

		var config = options || {};
		// Initially hide all target elements before animating.
		window.anime.set(nodes, {
			opacity: 0,
			translateY: config.translateY || 14
		});

		// Animate elements fading in and sliding up with staggered delay.
		window.anime({
			targets: nodes,
			opacity: [0, 1],
			translateY: [config.translateY || 14, 0],
			delay: window.anime.stagger(config.stagger || 60),
			duration: config.duration || 560,
			easing: config.easing || "easeOutExpo"
		});
	}

	// Animate the main application shell (sidebar and topbar).
	function animateShell() {
		var sidebar = document.querySelector(".app-sidebar");
		var topbar = document.querySelector(".topbar");

		if (sidebar) {
			window.anime({
				targets: sidebar,
				opacity: [0, 1],
				translateX: [-16, 0],
				duration: 520,
				easing: "easeOutCubic"
			});
		}

		if (topbar) {
			window.anime({
				targets: topbar,
				opacity: [0, 1],
				translateY: [-10, 0],
				delay: 80,
				duration: 500,
				easing: "easeOutCubic"
			});
		}

		revealSequence(".sidebar-nav .sidebar-link", {
			translateY: 8,
			stagger: 34,
			duration: 420,
			easing: "easeOutQuad"
		});
	}

	// Animate main content areas with staggered reveal effects.
	function animateContent() {
		revealSequence(".hero", { translateY: 18, duration: 620 });
		revealSequence(".stat-card", { translateY: 16, stagger: 70, duration: 560 });
		revealSequence(".panel", { translateY: 16, stagger: 80, duration: 560 });
		revealSequence(".info-card", { translateY: 12, stagger: 55, duration: 500 });
		revealSequence(".app-table tbody tr", { translateY: 10, stagger: 36, duration: 420 });
		revealSequence(".footer-note", { translateY: 8, stagger: 40, duration: 420 });
	}

	// Animate authentication page elements (login/registration).
	function animateAuth() {
		var hero = document.querySelector(".auth-hero");
		var card = document.querySelector(".auth-card");
		if (!hero && !card) {
			return;
		}

		if (hero) {
			window.anime({
				targets: hero,
				opacity: [0, 1],
				translateX: [-18, 0],
				duration: 560,
				easing: "easeOutExpo"
			});
		}

		if (card) {
			window.anime({
				targets: card,
				opacity: [0, 1],
				translateX: [18, 0],
				delay: 90,
				duration: 560,
				easing: "easeOutExpo"
			});
		}
	}

	// Convert total minutes to "HH:MM" time format, clamped to valid day range.
	function toTimeValue(totalMinutes) {
		var normalized = Math.max(0, Math.min(23 * 60 + 59, totalMinutes));
		var hours = Math.floor(normalized / 60);
		var minutes = normalized % 60;
		return String(hours).padStart(2, "0") + ":" + String(minutes).padStart(2, "0");
	}

	// Add minutes to a time string ("HH:MM") and return the new time.
	function addMinutesToTime(timeValue, minutesToAdd) {
		if (!timeValue || timeValue.indexOf(":") === -1) {
			return timeValue;
		}

		var parts = timeValue.split(":");
		var hours = parseInt(parts[0], 10);
		var minutes = parseInt(parts[1], 10);
		if (Number.isNaN(hours) || Number.isNaN(minutes)) {
			return timeValue;
		}

		return toTimeValue(hours * 60 + minutes + minutesToAdd);
	}

	// Set up the quick-add panel for creating schedule entries in the timetable.
	function initializeTimetableQuickAdd() {
		var panel = document.getElementById("quick-add-panel");
		if (!panel) {
			return;
		}

		var form = document.getElementById("quick-add-form");
		var sectionInput = document.getElementById("quick-add-section-id");
		var subjectInput = document.getElementById("quick-add-subject-id");
		var startInput = document.getElementById("quick-add-start-time");
		var endInput = document.getElementById("quick-add-end-time");
		var hint = document.getElementById("quick-add-hint");
		var triggers = document.querySelectorAll("[data-quick-add-trigger='true']");
		var closeButtons = panel.querySelectorAll("[data-quick-add-close='true']");
		var dayCheckboxes = panel.querySelectorAll("input[name='selectedDays']");

		if (!form || !startInput || !endInput || !triggers.length) {
			return;
		}

		function closePanel() {
			panel.hidden = true;
			panel.classList.remove("is-open");
		}

		function openPanel(trigger) {
			var sectionId = trigger.getAttribute("data-section-id") || "";
			var dayOfWeek = trigger.getAttribute("data-day-of-week") || "";
			var dayName = trigger.getAttribute("data-day-name") || "Selected day";
			var startTime = trigger.getAttribute("data-start-time") || "";
			var defaultEnd = trigger.getAttribute("data-default-end-time") || addMinutesToTime(startTime, 30);

			if (sectionInput) {
				sectionInput.value = sectionId;
			}

			startInput.value = startTime;
			endInput.value = defaultEnd;
			endInput.min = addMinutesToTime(startTime, 30);
			endInput.max = "19:00";

			if (subjectInput) {
				subjectInput.value = "";
			}

			dayCheckboxes.forEach(function (checkbox) {
				checkbox.checked = checkbox.value === dayOfWeek;
			});

			if (hint) {
				hint.textContent = "Choose a subject, then add " + dayName + " from " + startTime + " and optionally repeat on other days.";
			}

			panel.hidden = false;
			panel.classList.add("is-open");
			if (subjectInput && !subjectInput.disabled) {
				subjectInput.focus();
			} else {
				endInput.focus();
			}
		}

		triggers.forEach(function (trigger) {
			trigger.addEventListener("click", function () {
				openPanel(trigger);
			});
		});

		closeButtons.forEach(function (button) {
			button.addEventListener("click", closePanel);
		});

		panel.addEventListener("click", function (event) {
			if (event.target === panel) {
				closePanel();
			}
		});

		document.addEventListener("keydown", function (event) {
			if (event.key === "Escape" && !panel.hidden) {
				closePanel();
			}
		});
	}

	// Determine whether the content area or window is the primary scroll container.
	function getPrimaryScrollTarget() {
		var content = document.querySelector(".content");
		if (!content) {
			return null;
		}

		var style = window.getComputedStyle(content);
		var overflowY = style ? style.overflowY : "visible";
		var isScrollable = (overflowY === "auto" || overflowY === "scroll") && content.scrollHeight > content.clientHeight;
		return isScrollable ? content : null;
	}

	// Capture current scroll position for either content area or window.
	function captureScrollSnapshot() {
		var content = getPrimaryScrollTarget();
		if (content) {
			return {
				scope: "content",
				x: content.scrollLeft,
				y: content.scrollTop
			};
		}

		return {
			scope: "window",
			x: window.pageXOffset || window.scrollX || 0,
			y: window.pageYOffset || window.scrollY || 0
		};
	}

	// Restore scroll position from a previously captured snapshot.
	function applyScrollSnapshot(snapshot) {
		if (!snapshot) {
			return;
		}

		var x = Number(snapshot.x) || 0;
		var y = Number(snapshot.y) || 0;

		if (snapshot.scope === "content") {
			var content = document.querySelector(".content");
			if (content) {
				content.scrollLeft = x;
				content.scrollTop = y;
				return;
			}
		}

		window.scrollTo(x, y);
	}

	// Save current scroll position to session storage before form submission.
	function persistScrollForPostback() {
		var snapshot = captureScrollSnapshot();
		var payload = {
			path: window.location.pathname,
			timestamp: Date.now(),
			scope: snapshot.scope,
			x: snapshot.x,
			y: snapshot.y
		};

		try {
			sessionStorage.setItem(postbackScrollStateKey, JSON.stringify(payload));
		} catch (error) {
			// Ignore quota/privacy errors and allow regular page flow.
		}
	}

	// Restore scroll position after a postback, if valid state exists.
	function restoreScrollAfterPostback() {
		var rawPayload;
		try {
			rawPayload = sessionStorage.getItem(postbackScrollStateKey);
		} catch (error) {
			return;
		}

		if (!rawPayload) {
			return;
		}

		try {
			sessionStorage.removeItem(postbackScrollStateKey);
		} catch (error) {
			// Best-effort cleanup only.
		}

		var payload;
		try {
			payload = JSON.parse(rawPayload);
		} catch (error) {
			return;
		}

		if (!payload || payload.path !== window.location.pathname) {
			return;
		}

		if (typeof payload.timestamp !== "number" || Date.now() - payload.timestamp > postbackScrollStateMaxAgeMs) {
			return;
		}

		if (window.location.hash) {
			return;
		}

		if (window.history && "scrollRestoration" in window.history) {
			window.history.scrollRestoration = "manual";
		}

		window.requestAnimationFrame(function () {
			applyScrollSnapshot(payload);
			window.requestAnimationFrame(function () {
				applyScrollSnapshot(payload);
			});
		});
	}

	// Listen for form submissions and persist scroll position for POST forms.
	function initializePostbackScrollRetention() {
		document.addEventListener("submit", function (event) {
			var form = event.target;
			if (!(form instanceof HTMLFormElement)) {
				return;
			}

			if (event.defaultPrevented) {
				return;
			}

			if ((form.getAttribute("data-preserve-scroll") || "").toLowerCase() === "false") {
				return;
			}

			// Skip forms that open in a new tab.
			if ((form.getAttribute("target") || "").toLowerCase() === "_blank") {
				return;
			}

			var method = (form.getAttribute("method") || "get").toLowerCase();
			if (method !== "post") {
				return;
			}

			persistScrollForPostback();
		});
	}

	// Initialize all features when the DOM is fully loaded.
	document.addEventListener("DOMContentLoaded", function () {
		restoreScrollAfterPostback();
		initializePostbackScrollRetention();
		initializeTimetableQuickAdd();

		// Skip animations if user prefers reduced motion or library unavailable.
		if (prefersReducedMotion() || !supportsAnime()) {
			return;
		}

		animateShell();
		animateContent();
		animateAuth();
	});
})();
