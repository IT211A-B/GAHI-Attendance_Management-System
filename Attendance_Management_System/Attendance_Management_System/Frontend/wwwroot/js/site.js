(function () {
	function prefersReducedMotion() {
		return window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
	}

	function supportsAnime() {
		return typeof window.anime === "function";
	}

	function revealSequence(selector, options) {
		var nodes = document.querySelectorAll(selector);
		if (!nodes.length) {
			return;
		}

		var config = options || {};
		window.anime.set(nodes, {
			opacity: 0,
			translateY: config.translateY || 14
		});

		window.anime({
			targets: nodes,
			opacity: [0, 1],
			translateY: [config.translateY || 14, 0],
			delay: window.anime.stagger(config.stagger || 60),
			duration: config.duration || 560,
			easing: config.easing || "easeOutExpo"
		});
	}

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

	function animateContent() {
		revealSequence(".hero", { translateY: 18, duration: 620 });
		revealSequence(".stat-card", { translateY: 16, stagger: 70, duration: 560 });
		revealSequence(".panel", { translateY: 16, stagger: 80, duration: 560 });
		revealSequence(".info-card", { translateY: 12, stagger: 55, duration: 500 });
		revealSequence(".app-table tbody tr", { translateY: 10, stagger: 36, duration: 420 });
		revealSequence(".footer-note", { translateY: 8, stagger: 40, duration: 420 });
	}

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

	function toTimeValue(totalMinutes) {
		var normalized = Math.max(0, Math.min(23 * 60 + 59, totalMinutes));
		var hours = Math.floor(normalized / 60);
		var minutes = normalized % 60;
		return String(hours).padStart(2, "0") + ":" + String(minutes).padStart(2, "0");
	}

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

	function initializeTimetableQuickAdd() {
		var panel = document.getElementById("quick-add-panel");
		if (!panel) {
			return;
		}

		var form = document.getElementById("quick-add-form");
		var sectionInput = document.getElementById("quick-add-section-id");
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

			dayCheckboxes.forEach(function (checkbox) {
				checkbox.checked = checkbox.value === dayOfWeek;
			});

			if (hint) {
				hint.textContent = "Add " + dayName + " from " + startTime + " and optionally repeat on other days.";
			}

			panel.hidden = false;
			panel.classList.add("is-open");
			endInput.focus();
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

	document.addEventListener("DOMContentLoaded", function () {
		initializeTimetableQuickAdd();

		if (prefersReducedMotion() || !supportsAnime()) {
			return;
		}

		animateShell();
		animateContent();
		animateAuth();
	});
})();
