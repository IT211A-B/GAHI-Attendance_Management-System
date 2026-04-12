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

	var studentSubmitCooldownMs = 2000;
	var studentLastSubmitAt = 0;

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

	function debounce(callback, waitMs) {
		var timeoutId = 0;
		return function () {
			var args = arguments;
			window.clearTimeout(timeoutId);
			timeoutId = window.setTimeout(function () {
				callback.apply(null, args);
			}, waitMs);
		};
	}

	function makeApiUrl(baseUrl, queryParams) {
		var url = new URL(baseUrl, window.location.origin);
		if (!queryParams) {
			return url.toString();
		}

		Object.keys(queryParams).forEach(function (key) {
			var rawValue = queryParams[key];
			if (rawValue === null || rawValue === undefined) {
				return;
			}

			var textValue = String(rawValue).trim();
			if (!textValue.length) {
				return;
			}

			url.searchParams.set(key, textValue);
		});

		return url.toString();
	}

	function requestApi(method, url, body) {
		var options = {
			method: method,
			headers: {
				Accept: "application/json"
			}
		};

		if (body !== undefined) {
			options.headers["Content-Type"] = "application/json";
			options.body = JSON.stringify(body);
		}

		return window.fetch(url, options)
			.then(function (response) {
				return response.text().then(function (rawText) {
					var payload = parseJsonSafe(rawText);
					if (response.ok && payload && payload.success === true) {
						return {
							ok: true,
							data: payload.data,
							status: response.status,
							payload: payload
						};
					}

					var errorCode = payload && payload.error && payload.error.code ? payload.error.code : "BAD_REQUEST";
					var message = payload && payload.error && payload.error.message
						? payload.error.message
						: "Request failed.";

					return {
						ok: false,
						errorCode: errorCode,
						message: message,
						status: response.status,
						payload: payload
					};
				});
			})
			.catch(function () {
				return {
					ok: false,
					errorCode: "NETWORK_ERROR",
					message: "Unable to connect to the server right now.",
					status: 0,
					payload: null
				};
			});
	}

	function getApi(url) {
		return requestApi("GET", url);
	}

	function postApi(url, body) {
		return requestApi("POST", url, body);
	}

	function closeSuggestionBox(container) {
		if (!container) {
			return;
		}

		container.classList.remove("is-open");
		container.innerHTML = "";
	}

	function renderSuggestionBox(container, items, labelResolver, selectHandler, emptyMessage) {
		if (!container) {
			return;
		}

		container.innerHTML = "";

		if (!Array.isArray(items) || !items.length) {
			var emptyText = document.createElement("p");
			emptyText.className = "qr-suggestion-empty";
			emptyText.textContent = emptyMessage;
			container.appendChild(emptyText);
			container.classList.add("is-open");
			return;
		}

		items.forEach(function (item) {
			var button = document.createElement("button");
			button.type = "button";
			button.className = "qr-suggestion";
			button.textContent = labelResolver(item);
			button.addEventListener("click", function () {
				selectHandler(item);
			});
			container.appendChild(button);
		});

		container.classList.add("is-open");
	}

	function buildSessionUrl(template, sessionId) {
		if (!template) {
			return "";
		}

		return template.replace("__SESSION__", encodeURIComponent(sessionId));
	}

	function renderTeacherCheckinsTable(tbody, checkins) {
		if (!tbody) {
			return;
		}

		tbody.innerHTML = "";

		if (!Array.isArray(checkins) || !checkins.length) {
			var emptyRow = document.createElement("tr");
			var emptyCell = document.createElement("td");
			emptyCell.colSpan = 4;
			emptyCell.className = "muted";
			emptyCell.textContent = "No check-ins yet.";
			emptyRow.appendChild(emptyCell);
			tbody.appendChild(emptyRow);
			return;
		}

		checkins.forEach(function (entry) {
			var row = document.createElement("tr");

			var studentIdCell = document.createElement("td");
			studentIdCell.textContent = entry.studentNumber || String(entry.studentId || "-");

			var nameCell = document.createElement("td");
			nameCell.textContent = entry.studentName || "Unknown student";

			var checkedAtCell = document.createElement("td");
			var checkedAt = entry.checkedInAtUtc ? new Date(entry.checkedInAtUtc) : null;
			checkedAtCell.textContent = checkedAt && !Number.isNaN(checkedAt.getTime())
				? checkedAt.toLocaleTimeString()
				: "-";

			var statusCell = document.createElement("td");
			var statusPill = document.createElement("span");
			var normalizedStatus = String(entry.status || "present").toLowerCase();
			statusPill.className = "qr-status-pill " + (normalizedStatus === "late" ? "late" : "present");
			statusPill.textContent = normalizedStatus === "late" ? "Late" : "Present";
			statusCell.appendChild(statusPill);

			row.appendChild(studentIdCell);
			row.appendChild(nameCell);
			row.appendChild(checkedAtCell);
			row.appendChild(statusCell);
			tbody.appendChild(row);
		});
	}

	function initializeTeacherQrPage() {
		var page = document.querySelector("[data-attendance-qr-page='teacher']");
		if (!page) {
			return;
		}

		var sectionsUrl = page.getAttribute("data-sections-url") || "";
		var subjectsUrl = page.getAttribute("data-subjects-url") || "";
		var periodsUrl = page.getAttribute("data-periods-url") || "";
		var createSessionUrl = page.getAttribute("data-create-session-url") || "";
		var refreshSessionUrlTemplate = page.getAttribute("data-refresh-session-url-template") || "";
		var closeSessionUrlTemplate = page.getAttribute("data-close-session-url-template") || "";
		var feedUrlTemplate = page.getAttribute("data-feed-url-template") || "";

		var configuredFeedPollSeconds = parseInt(page.getAttribute("data-feed-poll-seconds") || "3", 10);
		if (Number.isNaN(configuredFeedPollSeconds) || configuredFeedPollSeconds < 1) {
			configuredFeedPollSeconds = 3;
		}

		var configuredRefreshThresholdSeconds = parseInt(page.getAttribute("data-refresh-threshold-seconds") || "10", 10);
		if (Number.isNaN(configuredRefreshThresholdSeconds) || configuredRefreshThresholdSeconds < 2) {
			configuredRefreshThresholdSeconds = 10;
		}

		var sectionInput = document.getElementById("qr-section-search");
		var sectionIdInput = document.getElementById("qr-section-id");
		var sectionSuggestions = document.getElementById("qr-section-suggestions");
		var subjectInput = document.getElementById("qr-subject-search");
		var subjectIdInput = document.getElementById("qr-subject-id");
		var subjectSuggestions = document.getElementById("qr-subject-suggestions");
		var periodInput = document.getElementById("qr-period-search");
		var scheduleIdInput = document.getElementById("qr-schedule-id");
		var periodSuggestions = document.getElementById("qr-period-suggestions");
		var timeRangeText = document.getElementById("qr-time-range");
		var generateBtn = document.getElementById("qr-generate-btn");
		var resetBtn = document.getElementById("qr-reset-btn");
		var statusText = document.getElementById("qr-status");
		var sessionText = document.getElementById("qr-session-id");
		var countdownText = document.getElementById("qr-countdown");
		var qrTarget = document.getElementById("qr-render-target");
		var tokenValue = document.getElementById("qr-token-value");
		var checkinsBody = document.getElementById("qr-checkins-body");

		if (!sectionInput || !sectionIdInput || !sectionSuggestions
			|| !subjectInput || !subjectIdInput || !subjectSuggestions
			|| !periodInput || !scheduleIdInput || !periodSuggestions
			|| !timeRangeText || !generateBtn || !resetBtn
			|| !statusText || !sessionText || !countdownText || !qrTarget || !tokenValue || !checkinsBody) {
			return;
		}

		var selectedSection = null;
		var selectedSubject = null;
		var selectedPeriod = null;
		var activeSession = null;
		var refreshInFlight = false;
		var countdownTimerId = 0;
		var feedTimerId = 0;

		function setStatus(message) {
			statusText.textContent = message;
		}

		function setGenerateState() {
			generateBtn.disabled = !(selectedSection && selectedSubject && selectedPeriod);
		}

		function stopSessionTimers() {
			if (countdownTimerId) {
				window.clearInterval(countdownTimerId);
				countdownTimerId = 0;
			}

			if (feedTimerId) {
				window.clearInterval(feedTimerId);
				feedTimerId = 0;
			}
		}

		function renderQrToken(token) {
			qrTarget.innerHTML = "";

			if (!token) {
				return;
			}

			if (!window.QRCode) {
				qrTarget.textContent = "QR library failed to load.";
				return;
			}

			new window.QRCode(qrTarget, {
				text: token,
				width: 230,
				height: 230,
				correctLevel: window.QRCode.CorrectLevel.M
			});
		}

		function renderQrTokenValue(token) {
			tokenValue.value = token || "";
		}

		function clearActiveSession(statusMessage) {
			activeSession = null;
			refreshInFlight = false;
			stopSessionTimers();
			sessionText.textContent = "-";
			countdownText.textContent = "-";
			renderQrToken("");
			renderQrTokenValue("");
			renderTeacherCheckinsTable(checkinsBody, []);
			if (statusMessage) {
				setStatus(statusMessage);
			}
		}

		function setSection(selection) {
			selectedSection = selection;
			sectionIdInput.value = selection ? String(selection.sectionId) : "";

			if (selection) {
				sectionInput.value = selection.sectionName;
			}

			closeSuggestionBox(sectionSuggestions);

			selectedSubject = null;
			subjectIdInput.value = "";
			subjectInput.value = "";
			subjectInput.disabled = !selection;
			closeSuggestionBox(subjectSuggestions);

			selectedPeriod = null;
			scheduleIdInput.value = "";
			periodInput.value = "";
			periodInput.disabled = true;
			timeRangeText.textContent = "Select a period to lock class time.";
			closeSuggestionBox(periodSuggestions);

			setGenerateState();
		}

		function setSubject(selection) {
			selectedSubject = selection;
			subjectIdInput.value = selection ? String(selection.subjectId) : "";

			if (selection) {
				subjectInput.value = selection.label || selection.subjectName;
			}

			closeSuggestionBox(subjectSuggestions);

			selectedPeriod = null;
			scheduleIdInput.value = "";
			periodInput.value = "";
			periodInput.disabled = !selection;
			timeRangeText.textContent = "Select a period to lock class time.";
			closeSuggestionBox(periodSuggestions);

			setGenerateState();
		}

		function setPeriod(selection) {
			selectedPeriod = selection;
			scheduleIdInput.value = selection ? String(selection.scheduleId) : "";

			if (selection) {
				periodInput.value = selection.label;
				timeRangeText.textContent = selection.dayName + " | " + selection.timeRangeLabel;
			} else {
				timeRangeText.textContent = "Select a period to lock class time.";
			}

			closeSuggestionBox(periodSuggestions);
			setGenerateState();
		}

		function disableQrInputs(message) {
			sectionInput.disabled = true;
			subjectInput.disabled = true;
			periodInput.disabled = true;
			generateBtn.disabled = true;
			setStatus(message || "QR controls are unavailable for this account.");
		}

		function loadSectionSuggestions(queryText) {
			if (!sectionsUrl) {
				disableQrInputs("Section suggestion endpoint is unavailable.");
				return;
			}

			var url = makeApiUrl(sectionsUrl, {
				q: queryText,
				take: 8
			});

			getApi(url).then(function (result) {
				if (!result.ok) {
					if (result.errorCode === "FORBIDDEN") {
						disableQrInputs(result.message);
					}
					closeSuggestionBox(sectionSuggestions);
					return;
				}

				renderSuggestionBox(
					sectionSuggestions,
					Array.isArray(result.data) ? result.data : [],
					function (item) { return item.sectionName; },
					function (item) {
						setSection(item);
						subjectInput.focus();
					},
					"No matching sections found."
				);
			});
		}

		function loadSubjectSuggestions(queryText) {
			if (!subjectsUrl || !selectedSection) {
				closeSuggestionBox(subjectSuggestions);
				return;
			}

			var url = makeApiUrl(subjectsUrl, {
				sectionId: selectedSection.sectionId,
				q: queryText,
				take: 8
			});

			getApi(url).then(function (result) {
				if (!result.ok) {
					closeSuggestionBox(subjectSuggestions);
					return;
				}

				var subjectResults = Array.isArray(result.data) ? result.data : [];
				var normalizedQueryText = (queryText || "").trim();

				if (!activeSession && subjectResults.length === 0 && !normalizedQueryText) {
					setStatus("No owned subjects are scheduled today for " + selectedSection.sectionName + ". Choose another section or check your timetable.");
				}

				renderSuggestionBox(
					subjectSuggestions,
					subjectResults,
					function (item) { return item.label || item.subjectName; },
					function (item) {
						setSubject(item);
						periodInput.focus();
					},
					"No matching subjects found for today."
				);
			});
		}

		function loadPeriodSuggestions(queryText) {
			if (!periodsUrl || !selectedSection || !selectedSubject) {
				closeSuggestionBox(periodSuggestions);
				return;
			}

			var url = makeApiUrl(periodsUrl, {
				sectionId: selectedSection.sectionId,
				subjectId: selectedSubject.subjectId,
				q: queryText,
				take: 8
			});

			getApi(url).then(function (result) {
				if (!result.ok) {
					closeSuggestionBox(periodSuggestions);
					return;
				}

				var periodResults = Array.isArray(result.data) ? result.data : [];
				var normalizedQueryText = (queryText || "").trim();

				if (!activeSession && periodResults.length === 0 && !normalizedQueryText) {
					setStatus("No owned periods are available today for the selected section and subject.");
				}

				renderSuggestionBox(
					periodSuggestions,
					periodResults,
					function (item) { return item.label; },
					function (item) {
						setPeriod(item);
					},
					"No matching periods found for today."
				);
			});
		}

		function fetchLiveCheckins() {
			if (!activeSession || !feedUrlTemplate) {
				return;
			}

			var url = buildSessionUrl(feedUrlTemplate, activeSession.sessionId);
			getApi(url).then(function (result) {
				if (!result.ok || !result.data) {
					return;
				}

				renderTeacherCheckinsTable(checkinsBody, result.data.checkins || []);
			});
		}

		function startSessionTimers() {
			stopSessionTimers();

			countdownTimerId = window.setInterval(function () {
				if (!activeSession) {
					countdownText.textContent = "-";
					return;
				}

				var remainingSeconds = Math.max(0, Math.floor((activeSession.expiresAtMs - Date.now()) / 1000));
				countdownText.textContent = remainingSeconds + "s";

				var refreshThreshold = activeSession.refreshAfterSeconds || configuredRefreshThresholdSeconds;
				if (remainingSeconds <= 0) {
					refreshSession(true);
					return;
				}

				if (remainingSeconds <= refreshThreshold) {
					refreshSession(false);
				}
			}, 1000);

			feedTimerId = window.setInterval(function () {
				fetchLiveCheckins();
			}, configuredFeedPollSeconds * 1000);
		}

		function applySession(sessionDto) {
			activeSession = {
				sessionId: sessionDto.sessionId,
				token: sessionDto.token,
				expiresAtMs: new Date(sessionDto.expiresAtUtc).getTime(),
				refreshAfterSeconds: sessionDto.refreshAfterSeconds || configuredRefreshThresholdSeconds,
				sectionName: sessionDto.sectionName || "-",
				subjectLabel: sessionDto.subjectLabel || "-",
				periodLabel: sessionDto.periodLabel || "-",
				timeRangeLabel: sessionDto.timeRangeLabel || "-"
			};

			sessionText.textContent = activeSession.sessionId;
			renderQrToken(activeSession.token);
			renderQrTokenValue(activeSession.token);
			renderTeacherCheckinsTable(checkinsBody, []);
			refreshInFlight = false;
			startSessionTimers();
			fetchLiveCheckins();
		}

		function refreshSession(forceWhenExpired) {
			if (!activeSession || refreshInFlight || !refreshSessionUrlTemplate) {
				return;
			}

			refreshInFlight = true;
			var url = buildSessionUrl(refreshSessionUrlTemplate, activeSession.sessionId);

			postApi(url, {})
				.then(function (result) {
					refreshInFlight = false;
					if (!result.ok || !result.data) {
						if (forceWhenExpired) {
							clearActiveSession(result.message || "Session expired. Generate a new QR.");
						}
						return;
					}

					applySession(result.data);
					setStatus("QR refreshed for " + activeSession.sectionName + " / " + activeSession.subjectLabel + ".");
				});
		}

		var debouncedSectionSearch = debounce(function () {
			loadSectionSuggestions(sectionInput.value.trim());
		}, 220);

		var debouncedSubjectSearch = debounce(function () {
			loadSubjectSuggestions(subjectInput.value.trim());
		}, 220);

		var debouncedPeriodSearch = debounce(function () {
			loadPeriodSuggestions(periodInput.value.trim());
		}, 220);

		sectionInput.addEventListener("input", function () {
			if (!selectedSection || sectionInput.value.trim() !== selectedSection.sectionName) {
				setSection(null);
			}
			debouncedSectionSearch();
		});

		sectionInput.addEventListener("focus", function () {
			loadSectionSuggestions(sectionInput.value.trim());
		});

		subjectInput.addEventListener("input", function () {
			if (!selectedSubject || subjectInput.value.trim() !== (selectedSubject.label || selectedSubject.subjectName)) {
				setSubject(null);
			}
			debouncedSubjectSearch();
		});

		subjectInput.addEventListener("focus", function () {
			loadSubjectSuggestions(subjectInput.value.trim());
		});

		periodInput.addEventListener("input", function () {
			if (!selectedPeriod || periodInput.value.trim() !== selectedPeriod.label) {
				setPeriod(null);
			}
			debouncedPeriodSearch();
		});

		periodInput.addEventListener("focus", function () {
			loadPeriodSuggestions(periodInput.value.trim());
		});

		document.addEventListener("click", function (event) {
			var target = event.target;
			if (!(target instanceof Element)) {
				return;
			}

			if (!sectionSuggestions.contains(target) && target !== sectionInput) {
				closeSuggestionBox(sectionSuggestions);
			}

			if (!subjectSuggestions.contains(target) && target !== subjectInput) {
				closeSuggestionBox(subjectSuggestions);
			}

			if (!periodSuggestions.contains(target) && target !== periodInput) {
				closeSuggestionBox(periodSuggestions);
			}
		});

		generateBtn.addEventListener("click", function () {
			if (!selectedSection || !selectedSubject || !selectedPeriod) {
				setStatus("Select section, subject, and period before generating QR.");
				return;
			}

			if (!createSessionUrl) {
				setStatus("Session endpoint is unavailable.");
				return;
			}

			setStatus("Creating QR session...");
			postApi(createSessionUrl, {
				sectionId: selectedSection.sectionId,
				subjectId: selectedSubject.subjectId,
				scheduleId: selectedPeriod.scheduleId
			}).then(function (result) {
				if (!result.ok || !result.data) {
					setStatus(result.message || "Unable to create QR session.");
					return;
				}

				applySession(result.data);
				setStatus(
					"Session active for "
						+ activeSession.sectionName
						+ " / "
						+ activeSession.subjectLabel
						+ " ("
						+ activeSession.periodLabel
						+ ")."
				);
			});
		});

		resetBtn.addEventListener("click", function () {
			if (!activeSession) {
				clearActiveSession("Session reset. Select section, subject, and period to generate a new QR.");
				return;
			}

			if (!closeSessionUrlTemplate) {
				clearActiveSession("Session reset locally. Select section, subject, and period to generate a new QR.");
				return;
			}

			var sessionId = activeSession.sessionId;
			var closeUrl = buildSessionUrl(closeSessionUrlTemplate, sessionId);
			setStatus("Closing session...");

			postApi(closeUrl, {})
				.then(function (result) {
					if (result.ok) {
						clearActiveSession("Session closed. Select section, subject, and period to generate a new QR.");
						return;
					}

					clearActiveSession(result.message || "Session reset. Close confirmation pending from server.");
				});
		});

		setSection(null);
		setStatus("Search section, subject, and period to generate a session.");
		loadSectionSuggestions("");
	}

	function initializeStudentScanPage() {
		var page = document.querySelector("[data-attendance-qr-page='student']");
		if (!page) {
			return;
		}

		var checkinUrl = page.getAttribute("data-checkin-url") || "";
		var scannerContainerId = "attendance-scanner";
		var startBtn = document.getElementById("scan-start-btn");
		var stopBtn = document.getElementById("scan-stop-btn");
		var statusText = document.getElementById("scan-status");
		var tokenInput = document.getElementById("scan-token");
		var submitBtn = document.getElementById("scan-submit-btn");
		var submitResult = document.getElementById("scan-submit-result");

		if (!startBtn || !stopBtn || !statusText || !tokenInput || !submitBtn || !submitResult) {
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
						setStatus("QR detected. Submit attendance when ready.");
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

			var token = tokenInput.value.trim();
			if (!token) {
				setSubmitResult("QR token is required.", "result-error");
				return;
			}

			if (token.length < 20 || token.length > 4096) {
				setSubmitResult("Token size is invalid.", "result-error");
				return;
			}

			if (!checkinUrl) {
				setSubmitResult("Check-in endpoint is unavailable.", "result-error");
				return;
			}

			setSubmitResult("Submitting attendance...", null);
			postApi(checkinUrl, {
				token: token
			}).then(function (result) {
				if (!result.ok || !result.data) {
					var errorType = result.errorCode === "ALREADY_CHECKED_IN"
						? "result-warning"
						: "result-error";
					setSubmitResult(result.message || "Unable to submit attendance.", errorType);
					return;
				}

				var normalizedStatus = String(result.data.status || "present").toLowerCase();
				var successType = normalizedStatus === "late"
					? "result-warning"
					: "result-success";

				setSubmitResult(result.data.message || "Attendance submitted successfully.", successType);
			});
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
