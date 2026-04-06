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

	var qrSessionStorageKey = "ams.qr.active-session";
	var qrCheckinPrefix = "ams.qr.checkins.";
	var qrSessionTtlSeconds = 90;
	var studentSubmitCooldownMs = 2000;
	var studentLastSubmitAt = 0;

	function makeGuidLikeId(prefix) {
		if (window.crypto && typeof window.crypto.randomUUID === "function") {
			return prefix + window.crypto.randomUUID();
		}

		return prefix + String(Date.now()) + "-" + String(Math.random()).slice(2, 11);
	}

	function encodeBase64Url(value) {
		var input = typeof value === "string" ? value : String(value || "");
		var utf8 = new TextEncoder().encode(input);
		var binary = "";
		for (var i = 0; i < utf8.length; i += 1) {
			binary += String.fromCharCode(utf8[i]);
		}

		return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");
	}

	function decodeBase64Url(encoded) {
		var normalized = String(encoded || "").replace(/-/g, "+").replace(/_/g, "/");
		while (normalized.length % 4 !== 0) {
			normalized += "=";
		}

		var binary = atob(normalized);
		var bytes = new Uint8Array(binary.length);
		for (var i = 0; i < binary.length; i += 1) {
			bytes[i] = binary.charCodeAt(i);
		}

		return new TextDecoder().decode(bytes);
	}

	function stringifyJsonSafe(data) {
		try {
			return JSON.stringify(data);
		} catch (error) {
			return "";
		}
	}

	function parseJsonSafe(data) {
		if (!data) {
			return null;
		}

		try {
			return JSON.parse(data);
		} catch (error) {
			return null;
		}
	}

	function buildSessionToken(payload) {
		return encodeBase64Url(stringifyJsonSafe(payload));
	}

	function parseSessionToken(token) {
		if (!token) {
			return null;
		}

		try {
			var decoded = decodeBase64Url(token.trim());
			var parsed = parseJsonSafe(decoded);
			return parsed && typeof parsed === "object" ? parsed : null;
		} catch (error) {
			return null;
		}
	}

	function saveActiveQrSession(session) {
		try {
			localStorage.setItem(qrSessionStorageKey, stringifyJsonSafe(session));
		} catch (error) {
			// Ignore storage write failures in private browsing contexts.
		}
	}

	function getActiveQrSession() {
		var raw;
		try {
			raw = localStorage.getItem(qrSessionStorageKey);
		} catch (error) {
			return null;
		}

		var parsed = parseJsonSafe(raw);
		if (!parsed || typeof parsed !== "object") {
			return null;
		}

		if (typeof parsed.expiresAt !== "number" || Date.now() > parsed.expiresAt) {
			return null;
		}

		return parsed;
	}

	function clearQrState() {
		var active = getActiveQrSession();
		try {
			localStorage.removeItem(qrSessionStorageKey);
			if (active && active.sessionId) {
				localStorage.removeItem(qrCheckinPrefix + active.sessionId);
			}
		} catch (error) {
			// Ignore storage cleanup failures.
		}
	}

	function getCheckinsForSession(sessionId) {
		if (!sessionId) {
			return [];
		}

		var raw;
		try {
			raw = localStorage.getItem(qrCheckinPrefix + sessionId);
		} catch (error) {
			return [];
		}

		var parsed = parseJsonSafe(raw);
		if (!Array.isArray(parsed)) {
			return [];
		}

		return parsed
			.filter(function (item) {
				return item && typeof item.studentId === "string" && typeof item.studentName === "string" && typeof item.scannedAt === "number";
			})
			.sort(function (a, b) {
				return b.scannedAt - a.scannedAt;
			});
	}

	function saveCheckinsForSession(sessionId, checkins) {
		if (!sessionId) {
			return;
		}

		try {
			localStorage.setItem(qrCheckinPrefix + sessionId, stringifyJsonSafe(checkins || []));
		} catch (error) {
			// Ignore storage write failures in restricted environments.
		}
	}

	function createQrSession(sectionCode, subjectCode, periodLabel) {
		var normalizedSection = String(sectionCode || "").trim();
		var normalizedSubject = String(subjectCode || "").trim();
		var normalizedPeriod = String(periodLabel || "").trim();

		var expiresAt = Date.now() + qrSessionTtlSeconds * 1000;
		var payload = {
			version: 1,
			sessionId: makeGuidLikeId("sess_"),
			sectionCode: normalizedSection,
			subjectCode: normalizedSubject,
			periodLabel: normalizedPeriod,
			issuedAt: Date.now(),
			expiresAt: expiresAt,
			nonce: makeGuidLikeId("n_")
		};

		payload.token = buildSessionToken(payload);
		saveActiveQrSession(payload);
		saveCheckinsForSession(payload.sessionId, []);
		return payload;
	}

	function renderCheckinsTable(tbody, checkins) {
		if (!tbody) {
			return;
		}

		if (!checkins.length) {
			tbody.innerHTML = "";
			var emptyRow = document.createElement("tr");
			var emptyCell = document.createElement("td");
			emptyCell.colSpan = 3;
			emptyCell.className = "muted";
			emptyCell.textContent = "No check-ins yet.";
			emptyRow.appendChild(emptyCell);
			tbody.appendChild(emptyRow);
			return;
		}

		tbody.innerHTML = "";
		checkins.forEach(function (entry) {
			var row = document.createElement("tr");

			var idCell = document.createElement("td");
			idCell.textContent = entry.studentId;

			var nameCell = document.createElement("td");
			nameCell.textContent = entry.studentName;

			var scannedCell = document.createElement("td");
			scannedCell.textContent = new Date(entry.scannedAt).toLocaleTimeString();

			row.appendChild(idCell);
			row.appendChild(nameCell);
			row.appendChild(scannedCell);
			tbody.appendChild(row);
		});
	}

	function initializeTeacherQrPage() {
		var page = document.querySelector("[data-attendance-qr-page='teacher']");
		if (!page) {
			return;
		}

		var sectionInput = document.getElementById("qr-section-code");
		var subjectInput = document.getElementById("qr-subject-code");
		var periodInput = document.getElementById("qr-period-label");
		var generateBtn = document.getElementById("qr-generate-btn");
		var resetBtn = document.getElementById("qr-reset-btn");
		var statusText = document.getElementById("qr-status");
		var sessionText = document.getElementById("qr-session-id");
		var countdownText = document.getElementById("qr-countdown");
		var qrTarget = document.getElementById("qr-render-target");
		var checkinsBody = document.getElementById("qr-checkins-body");

		if (!sectionInput || !subjectInput || !periodInput || !generateBtn || !resetBtn || !statusText || !sessionText || !countdownText || !qrTarget || !checkinsBody) {
			return;
		}

		var activeSession = null;

		function updateTeacherUi(session) {
			activeSession = session;

			if (!session) {
				statusText.textContent = "Generate a session to display QR.";
				sessionText.textContent = "-";
				countdownText.textContent = "-";
				qrTarget.innerHTML = "";
				renderCheckinsTable(checkinsBody, []);
				return;
			}

			statusText.textContent = "Session active for " + session.sectionCode + " / " + session.subjectCode + " (" + session.periodLabel + ").";
			sessionText.textContent = session.sessionId;

			qrTarget.innerHTML = "";
			if (window.QRCode) {
				new window.QRCode(qrTarget, {
					text: session.token,
					width: 230,
					height: 230,
					correctLevel: window.QRCode.CorrectLevel.M
				});
			} else {
				qrTarget.textContent = "QR library failed to load.";
			}

			renderCheckinsTable(checkinsBody, getCheckinsForSession(session.sessionId));
		}

		function regenerateIfExpired() {
			if (!activeSession) {
				return;
			}

			var remainingSeconds = Math.max(0, Math.floor((activeSession.expiresAt - Date.now()) / 1000));
			countdownText.textContent = remainingSeconds + "s";

			renderCheckinsTable(checkinsBody, getCheckinsForSession(activeSession.sessionId));

			if (remainingSeconds > 0) {
				return;
			}

			var rotated = createQrSession(activeSession.sectionCode, activeSession.subjectCode, activeSession.periodLabel);
			updateTeacherUi(rotated);
		}

		generateBtn.addEventListener("click", function () {
			var sectionCode = sectionInput.value.trim();
			var subjectCode = subjectInput.value.trim();
			var periodLabel = periodInput.value.trim();
			var sectionPattern = /^[A-Za-z0-9\- ]{2,64}$/;
			var subjectPattern = /^[A-Za-z0-9\- ]{2,64}$/;
			var periodPattern = /^[A-Za-z0-9:\- ]{3,64}$/;

			if (!sectionCode || !subjectCode || !periodLabel) {
				statusText.textContent = "Section, subject, and period are required.";
				return;
			}

			if (!sectionPattern.test(sectionCode) || !subjectPattern.test(subjectCode) || !periodPattern.test(periodLabel)) {
				statusText.textContent = "Use letters, numbers, spaces, and dashes only for section/subject/period.";
				return;
			}

			var session = createQrSession(sectionCode, subjectCode, periodLabel);
			updateTeacherUi(session);
			countdownText.textContent = qrSessionTtlSeconds + "s";
		});

		resetBtn.addEventListener("click", function () {
			clearQrState();
			updateTeacherUi(null);
		});

		var existing = getActiveQrSession();
		updateTeacherUi(existing);
		window.setInterval(regenerateIfExpired, 1000);
	}

	function initializeStudentScanPage() {
		var page = document.querySelector("[data-attendance-qr-page='student']");
		if (!page) {
			return;
		}

		var scannerContainerId = "attendance-scanner";
		var startBtn = document.getElementById("scan-start-btn");
		var stopBtn = document.getElementById("scan-stop-btn");
		var statusText = document.getElementById("scan-status");
		var studentIdInput = document.getElementById("scan-student-id");
		var studentNameInput = document.getElementById("scan-student-name");
		var tokenInput = document.getElementById("scan-token");
		var submitBtn = document.getElementById("scan-submit-btn");
		var submitResult = document.getElementById("scan-submit-result");

		if (!startBtn || !stopBtn || !statusText || !studentIdInput || !studentNameInput || !tokenInput || !submitBtn || !submitResult) {
			return;
		}

		var scanner = null;
		var scannerState = "idle";

		function setStatus(message) {
			statusText.textContent = message;
		}

		function setSubmitResult(message, type) {
			submitResult.textContent = message;
			submitResult.classList.remove("result-success", "result-error", "result-warning");
			if (type) {
				submitResult.classList.add(type);
			}
		}

		function stopScanner() {
			if (!scanner) {
				scannerState = "idle";
				setStatus("Scanner already stopped.");
				return Promise.resolve();
			}

			if (scannerState !== "running" && scannerState !== "starting") {
				scannerState = "idle";
				try {
					scanner.clear();
				} catch (error) {
					// Ignore cleanup errors.
				}
				scanner = null;
				setStatus("Scanner already stopped.");
				return Promise.resolve();
			}

			scannerState = "stopping";
			var stopPromise;
			try {
				stopPromise = scanner.stop();
			} catch (error) {
				stopPromise = Promise.resolve();
			}

			return Promise.resolve(stopPromise)
				.catch(function () {
					return null;
				})
				.finally(function () {
					try {
						scanner.clear();
					} catch (error) {
						// Ignore cleanup errors.
					}
					scanner = null;
					scannerState = "idle";
					setStatus("Scanner stopped.");
				});
		}

		startBtn.addEventListener("click", function () {
			if (!window.Html5Qrcode) {
				setStatus("Scanner library failed to load. Paste token manually.");
				return;
			}

			if (scanner || scannerState === "starting" || scannerState === "running") {
				setStatus("Scanner already running.");
				return;
			}

			scanner = new window.Html5Qrcode(scannerContainerId);
			scannerState = "starting";
			setStatus("Starting scanner...");

			scanner
				.start(
					{ facingMode: "environment" },
					{ fps: 8, qrbox: { width: 230, height: 230 } },
					function (decodedText) {
						tokenInput.value = decodedText;
						setStatus("QR detected. Review details and submit.");
						stopScanner();
					},
					function () {
						// Ignore transient decode errors while scanning.
					}
				)
				.then(function () {
					scannerState = "running";
				})
				.catch(function () {
					scannerState = "idle";
					setStatus("Camera start failed. Paste token manually.");
					try {
						scanner.clear();
					} catch (error) {
						// Ignore cleanup failures.
					}
					scanner = null;
				});
		});

		stopBtn.addEventListener("click", function () {
			stopScanner();
		});

		submitBtn.addEventListener("click", function () {
			if (Date.now() - studentLastSubmitAt < studentSubmitCooldownMs) {
				setSubmitResult("Please wait before submitting again.", "result-warning");
				return;
			}

			studentLastSubmitAt = Date.now();

			var studentId = studentIdInput.value.trim();
			var studentName = studentNameInput.value.trim();
			var token = tokenInput.value.trim();
			var idPattern = /^[A-Za-z0-9\-]{2,32}$/;
			var namePattern = /^[A-Za-z][A-Za-z .'-]{1,118}$/;

			if (!studentId || !studentName || !token) {
				setSubmitResult("Student ID, student name, and token are required.", "result-error");
				return;
			}

			if (!idPattern.test(studentId)) {
				setSubmitResult("Student ID should be 2-32 characters using letters, numbers, and dashes.", "result-error");
				return;
			}

			if (!namePattern.test(studentName)) {
				setSubmitResult("Student name contains unsupported characters.", "result-error");
				return;
			}

			if (token.length < 20 || token.length > 4096) {
				setSubmitResult("Token size is invalid.", "result-error");
				return;
			}

			var tokenPayload = parseSessionToken(token);
			if (!tokenPayload || typeof tokenPayload.sessionId !== "string") {
				setSubmitResult("Invalid QR token format.", "result-error");
				return;
			}

			if (typeof tokenPayload.expiresAt !== "number" || Date.now() > tokenPayload.expiresAt) {
				setSubmitResult("QR token already expired. Ask your teacher for a fresh QR.", "result-error");
				return;
			}

			var activeSession = getActiveQrSession();
			if (!activeSession || activeSession.sessionId !== tokenPayload.sessionId) {
				setSubmitResult("This QR is not active for the current class session.", "result-error");
				return;
			}

			var checkins = getCheckinsForSession(activeSession.sessionId);
			var alreadyPresent = checkins.some(function (entry) {
				return entry.studentId.toLowerCase() === studentId.toLowerCase();
			});

			if (alreadyPresent) {
				setSubmitResult("Already marked present for this session.", "result-warning");
				return;
			}

			checkins.push({
				studentId: studentId,
				studentName: studentName,
				scannedAt: Date.now()
			});

			saveCheckinsForSession(activeSession.sessionId, checkins);
			setSubmitResult("Attendance submitted for " + activeSession.subjectCode + " (" + activeSession.periodLabel + ").", "result-success");
		});

		window.addEventListener("beforeunload", function () {
			if (scanner) {
				stopScanner();
			}
		});
	}

	// Initialize all features when the DOM is fully loaded.
	document.addEventListener("DOMContentLoaded", function () {
		restoreScrollAfterPostback();
		initializePostbackScrollRetention();
		initializeTimetableQuickAdd();
		initializeTeacherQrPage();
		initializeStudentScanPage();

		// Skip animations if user prefers reduced motion or library unavailable.
		if (prefersReducedMotion() || !supportsAnime()) {
			return;
		}

		animateShell();
		animateContent();
		animateAuth();
	});
})();
