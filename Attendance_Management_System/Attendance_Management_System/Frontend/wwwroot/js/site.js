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

	function initializeLazyLoading() {
		var lazyImages = Array.from(document.querySelectorAll("img[data-lazy-src]"));
		if (!lazyImages.length) {
			return;
		}

		function loadImage(image) {
			if (!image || image.getAttribute("data-lazy-loaded") === "true") {
				return;
			}

			var source = image.getAttribute("data-lazy-src");
			if (!source) {
				return;
			}

			image.addEventListener("load", function () {
				image.classList.add("is-loaded");
			}, { once: true });

			image.src = source;
			image.setAttribute("data-lazy-loaded", "true");
			image.removeAttribute("data-lazy-src");
		}

		if (!("IntersectionObserver" in window)) {
			lazyImages.forEach(loadImage);
			return;
		}

		var observer = new IntersectionObserver(function (entries) {
			entries.forEach(function (entry) {
				if (!entry.isIntersecting) {
					return;
				}

				loadImage(entry.target);
				observer.unobserve(entry.target);
			});
		}, {
			rootMargin: "160px 0px"
		});

		lazyImages.forEach(function (image) {
			observer.observe(image);
		});
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

	// Initialize floating help chat with role-filtered predefined Q&A entries.
	function initializePredefinedChatWidget() {
		var widget = document.getElementById("predefined-chat-widget");
		if (!widget) {
			return;
		}

		var launcher = document.getElementById("chat-widget-launcher");
		var panel = document.getElementById("chat-widget-panel");
		var closeBtn = panel ? panel.querySelector("[data-chat-widget-close='true']") : null;
		var questionList = document.getElementById("chat-widget-question-list");
		var answer = document.getElementById("chat-widget-answer");
		var searchInput = document.getElementById("chat-widget-search");
		var categoryTabs = document.getElementById("chat-widget-categories");

		if (!launcher || !panel || !questionList || !answer) {
			return;
		}

		var currentRole = (widget.getAttribute("data-user-role") || "guest").toLowerCase();
		var apiUrl = (widget.getAttribute("data-chat-api-url") || "").trim();
		var activeQuestionId = null;
		var activeCategory = "all";
		var entries = [];

		// Keep these entries as plain objects so an API payload can swap in later.
		var fallbackEntries = [
			{
				id: "scan_qr",
				roles: ["student"],
				question: "How do I scan my attendance QR code?",
				answer: "Open Scan QR in the left menu, allow camera access, and point your camera at the teacher's QR. If camera fails, paste the token manually and submit.",
				tags: ["scan", "qr", "attendance", "camera"],
				category: "attendance"
			},
			{
				id: "attendance_missing",
				roles: ["student"],
				question: "What should I do if my attendance is missing?",
				answer: "Open your attendance details and note the date and subject, then message your teacher with the session information so they can review the check-in log.",
				tags: ["attendance", "missing", "record"],
				category: "attendance"
			},
			{
				id: "late_policy",
				roles: ["student", "teacher"],
				question: "What if I am marked late?",
				answer: "Late status is based on the session window set by your teacher. If you think it is incorrect, contact your teacher immediately for attendance review.",
				tags: ["late", "status", "attendance"],
				category: "attendance"
			},
			{
				id: "create_qr",
				roles: ["teacher"],
				question: "How do I create a QR attendance session?",
				answer: "Go to Attendance QR, pick section, subject, and academic period, then click Generate Session QR. Share the displayed code with students before the session expires.",
				tags: ["teacher", "generate", "session", "qr"],
				category: "attendance"
			},
			{
				id: "session_expired",
				roles: ["teacher", "student"],
				question: "Why is the QR session expired?",
				answer: "Each attendance QR has a time limit for security. Teachers should regenerate a new QR session and students should scan immediately.",
				tags: ["qr", "expired", "session", "attendance"],
				category: "attendance"
			},
			{
				id: "schedule_conflict",
				roles: ["teacher", "admin"],
				question: "Why can I not save this schedule slot?",
				answer: "A slot may fail if times overlap, required fields are missing, or ownership rules block updates. Check section, subject, day, and time range, then try again.",
				tags: ["schedule", "conflict", "save"],
				category: "schedule"
			},
			{
				id: "schedule_quick_add",
				roles: ["teacher", "admin"],
				question: "How can I add classes faster in timetable?",
				answer: "Use the quick-add slots in the timetable grid. Pick the subject, verify start and end time, then apply to selected days before saving.",
				tags: ["quick add", "timetable", "schedule"],
				category: "schedule"
			},
			{
				id: "teacher_ownership",
				roles: ["teacher"],
				question: "Why can I not edit another teacher's schedule?",
				answer: "Schedule ownership rules restrict teachers to their assigned schedules. Contact an admin if reassignment is needed.",
				tags: ["teacher", "ownership", "schedule"],
				category: "schedule"
			},
			{
				id: "password_update",
				roles: ["student", "teacher", "admin"],
				question: "How can I change my password?",
				answer: "Open Settings, enter your current password, then set a new password with confirmation and save changes.",
				tags: ["password", "settings", "account"],
				category: "account"
			},
			{
				id: "profile_update",
				roles: ["student", "teacher", "admin"],
				question: "How do I update my profile details?",
				answer: "Go to Settings, edit allowed fields, and save. Some fields may be locked by system policy.",
				tags: ["profile", "settings", "account"],
				category: "account"
			},
			{
				id: "manage_users",
				roles: ["admin"],
				question: "How do I manage user accounts?",
				answer: "Go to Users from the sidebar. You can review user details, update roles, and maintain access based on school policy.",
				tags: ["users", "admin", "roles", "accounts"],
				category: "account"
			},
			{
				id: "enrollments_manage",
				roles: ["admin", "student"],
				question: "Where do I check enrollment status?",
				answer: "Open Enrollments in the sidebar to view current status and required actions for your account or managed students.",
				tags: ["enrollment", "status", "student"],
				category: "account"
			},
			{
				id: "report_export",
				roles: ["teacher", "admin"],
				question: "How do I view attendance reports?",
				answer: "Open Reports, apply your filters (period, section, subject), then run the report. Export options depend on your role and current report type.",
				tags: ["reports", "attendance", "export"],
				category: "attendance"
			},
			{
				id: "programs_periods",
				roles: ["admin"],
				question: "When should I configure programs and academic periods?",
				answer: "Set academic periods first, then programs and sections. This order keeps enrollment, schedule, and reporting consistent.",
				tags: ["programs", "periods", "admin", "setup"],
				category: "schedule"
			}
		];

		function createFallbackId(index) {
			return "faq_" + String(index + 1);
		}

		function normalizeEntry(raw, index) {
			if (!raw || typeof raw !== "object") {
				return null;
			}

			var roles = Array.isArray(raw.roles) && raw.roles.length
				? raw.roles.map(function (role) { return String(role).toLowerCase(); })
				: ["student", "teacher", "admin"];

			var question = String(raw.question || "").trim();
			var response = String(raw.answer || "").trim();
			if (!question || !response) {
				return null;
			}

			var category = String(raw.category || "all").trim().toLowerCase();
			if (!category) {
				category = "all";
			}

			var tags = Array.isArray(raw.tags)
				? raw.tags.map(function (tag) { return String(tag).toLowerCase(); })
				: [];

			return {
				id: String(raw.id || createFallbackId(index)),
				roles: roles,
				question: question,
				answer: response,
				tags: tags,
				category: category
			};
		}

		function normalizeEntries(rawItems) {
			if (!Array.isArray(rawItems)) {
				return [];
			}

			return rawItems
				.map(function (item, index) {
					return normalizeEntry(item, index);
				})
				.filter(function (item) {
					return item !== null;
				});
		}

		function loadEntries() {
			if (!apiUrl) {
				return Promise.resolve(normalizeEntries(fallbackEntries));
			}

			return fetch(apiUrl, {
				method: "GET",
				headers: {
					Accept: "application/json"
				},
				credentials: "same-origin"
			})
				.then(function (response) {
					if (!response.ok) {
						throw new Error("Failed to load chat entries.");
					}

					return response.json();
				})
				.then(function (payload) {
					var rawItems = Array.isArray(payload)
						? payload
						: payload && Array.isArray(payload.items)
							? payload.items
							: [];

					var normalized = normalizeEntries(rawItems);
					if (normalized.length) {
						return normalized;
					}

					throw new Error("Empty chat data payload.");
				})
				.catch(function () {
					return normalizeEntries(fallbackEntries);
				});
		}

		function canAccess(entry) {
			return entry.roles.indexOf(currentRole) >= 0;
		}

		function matchesCategory(entry) {
			if (activeCategory === "all") {
				return true;
			}

			return entry.category === activeCategory;
		}

		function matchesSearch(entry, query) {
			if (!query) {
				return true;
			}

			var normalized = query.toLowerCase();
			if (entry.question.toLowerCase().indexOf(normalized) >= 0) {
				return true;
			}

			return entry.tags.some(function (tag) {
				return tag.indexOf(normalized) >= 0;
			});
		}

		function getVisibleEntries() {
			var query = searchInput ? searchInput.value.trim() : "";
			return entries.filter(function (entry) {
				return canAccess(entry) && matchesCategory(entry) && matchesSearch(entry, query);
			});
		}

		function setAnswer(entry) {
			if (!entry) {
				answer.textContent = "No answer available.";
				return;
			}

			activeQuestionId = entry.id;
			answer.replaceChildren();

			var questionText = document.createElement("p");
			var questionLabel = document.createElement("strong");
			questionLabel.textContent = "Q: ";
			questionText.appendChild(questionLabel);
			questionText.appendChild(document.createTextNode(entry.question));

			var answerText = document.createElement("p");
			var answerLabel = document.createElement("strong");
			answerLabel.textContent = "A: ";
			answerText.appendChild(answerLabel);
			answerText.appendChild(document.createTextNode(entry.answer));

			answer.appendChild(questionText);
			answer.appendChild(answerText);
			refreshActiveState();
		}

		function refreshActiveState() {
			var buttons = questionList.querySelectorAll(".chat-widget-question");
			buttons.forEach(function (button) {
				var isActive = button.getAttribute("data-question-id") === activeQuestionId;
				button.classList.toggle("active", isActive);
				button.setAttribute("aria-selected", isActive ? "true" : "false");
			});
		}

		function renderQuestions() {
			var visible = getVisibleEntries();
			questionList.replaceChildren();

			if (!visible.length) {
				activeQuestionId = null;
				answer.textContent = "No predefined questions matched your search.";
				var emptyNote = document.createElement("p");
				emptyNote.className = "chat-widget-empty";
				emptyNote.textContent = "No questions found.";
				questionList.appendChild(emptyNote);
				return;
			}

			visible.forEach(function (entry) {
				var button = document.createElement("button");
				button.type = "button";
				button.className = "chat-widget-question";
				button.setAttribute("role", "option");
				button.setAttribute("data-question-id", entry.id);
				button.textContent = entry.question;
				button.addEventListener("click", function () {
					setAnswer(entry);
				});
				questionList.appendChild(button);
			});

			var activeEntry = visible.find(function (entry) {
				return entry.id === activeQuestionId;
			});

			if (!activeEntry) {
				setAnswer(visible[0]);
			} else {
				refreshActiveState();
			}
		}

		function refreshCategoryState() {
			if (!categoryTabs) {
				return;
			}

			var tabs = categoryTabs.querySelectorAll("[data-chat-category]");
			tabs.forEach(function (tab) {
				var isActive = tab.getAttribute("data-chat-category") === activeCategory;
				tab.classList.toggle("active", isActive);
				tab.setAttribute("aria-selected", isActive ? "true" : "false");
			});
		}

		function openWidget() {
			panel.hidden = false;
			widget.setAttribute("data-widget-state", "open");
			launcher.setAttribute("aria-expanded", "true");
			if (searchInput) {
				searchInput.focus();
			}
		}

		function closeWidget() {
			panel.hidden = true;
			widget.setAttribute("data-widget-state", "closed");
			launcher.setAttribute("aria-expanded", "false");
		}

		launcher.addEventListener("click", function () {
			if (panel.hidden) {
				openWidget();
				return;
			}

			closeWidget();
		});

		if (closeBtn) {
			closeBtn.addEventListener("click", closeWidget);
		}

		document.addEventListener("keydown", function (event) {
			if (event.key === "Escape" && !panel.hidden) {
				closeWidget();
			}
		});

		document.addEventListener("click", function (event) {
			if (panel.hidden) {
				return;
			}

			var clickedInsideWidget = widget.contains(event.target);
			if (!clickedInsideWidget) {
				closeWidget();
			}
		});

		if (searchInput) {
			searchInput.addEventListener("input", renderQuestions);
		}

		if (categoryTabs) {
			categoryTabs.addEventListener("click", function (event) {
				var tab = event.target.closest("[data-chat-category]");
				if (!tab) {
					return;
				}

				activeCategory = String(tab.getAttribute("data-chat-category") || "all").toLowerCase();
				refreshCategoryState();
				renderQuestions();
			});
		}

		loadEntries().then(function (loadedEntries) {
			entries = loadedEntries;
			refreshCategoryState();
			renderQuestions();
		});
	}

	function initializeNotificationCenter() {
		var center = document.querySelector("[data-notification-center='true']");
		if (!center) {
			return;
		}

		var toggleBtn = document.getElementById("notification-toggle");
		var panel = document.getElementById("notification-panel");
		var listContainer = document.getElementById("notification-list");
		var unreadBadge = document.getElementById("notification-unread-badge");
		var markAllBtn = document.getElementById("notification-mark-all-btn");

		if (!toggleBtn || !panel || !listContainer || !unreadBadge || !markAllBtn) {
			return;
		}

		var listUrl = (center.getAttribute("data-list-url") || "").trim();
		var markReadUrlTemplate = (center.getAttribute("data-mark-read-url-template") || "").trim();
		var markAllReadUrl = (center.getAttribute("data-mark-all-read-url") || "").trim();
		var hubUrl = (center.getAttribute("data-hub-url") || "").trim();
		var maxItems = parseInt(center.getAttribute("data-max-items") || "20", 10);
		if (Number.isNaN(maxItems) || maxItems < 5) {
			maxItems = 20;
		}

		var notifications = [];
		var hasLoaded = false;
		var signalrConnection = null;

		function buildNotificationMarkReadUrl(notificationId) {
			if (!markReadUrlTemplate) {
				return "";
			}

			return markReadUrlTemplate.replace("__ID__", encodeURIComponent(notificationId));
		}

		function formatNotificationTime(isoTime) {
			if (!isoTime) {
				return "";
			}

			var dateValue = new Date(isoTime);
			if (Number.isNaN(dateValue.getTime())) {
				return "";
			}

			return dateValue.toLocaleString();
		}

		function computeUnreadCount() {
			return notifications.reduce(function (count, item) {
				return count + (item.isRead ? 0 : 1);
			}, 0);
		}

		function refreshUnreadBadge() {
			var unreadCount = computeUnreadCount();
			if (unreadCount <= 0) {
				unreadBadge.textContent = "0";
				unreadBadge.hidden = true;
				return;
			}

			unreadBadge.textContent = unreadCount > 99 ? "99+" : String(unreadCount);
			unreadBadge.hidden = false;
		}

		function markNotificationRead(notificationId) {
			var url = buildNotificationMarkReadUrl(notificationId);
			if (!url) {
				return Promise.resolve(false);
			}

			return postApi(url, {})
				.then(function (result) {
					if (!result.ok) {
						return false;
					}

					notifications = notifications.map(function (item) {
						if (item.id !== notificationId) {
							return item;
						}

						item.isRead = true;
						return item;
					});

					refreshUnreadBadge();
					renderNotifications();
					return true;
				});
		}

		function renderEmptyState(message) {
			listContainer.replaceChildren();
			var emptyText = document.createElement("p");
			emptyText.className = "notification-empty";
			emptyText.textContent = message;
			listContainer.appendChild(emptyText);
		}

		function renderNotifications() {
			listContainer.replaceChildren();

			if (!notifications.length) {
				renderEmptyState("No notifications yet.");
				markAllBtn.disabled = true;
				refreshUnreadBadge();
				return;
			}

			markAllBtn.disabled = computeUnreadCount() === 0;

			notifications.forEach(function (item) {
				var card = document.createElement("article");
				card.className = "notification-item" + (item.isRead ? "" : " unread");

				var header = document.createElement("div");
				header.className = "notification-item-header";

				var title = document.createElement("p");
				title.className = "notification-item-title";
				title.textContent = item.title || "Notification";

				var time = document.createElement("p");
				time.className = "notification-item-time";
				time.textContent = formatNotificationTime(item.createdAt);

				header.appendChild(title);
				header.appendChild(time);

				var message = document.createElement("p");
				message.className = "notification-item-message";
				message.textContent = item.message || "";

				var actions = document.createElement("div");
				actions.className = "notification-item-actions";

				if (item.linkUrl) {
					var link = document.createElement("a");
					link.className = "notification-link";
					link.href = item.linkUrl;
					link.textContent = "Open";
					actions.appendChild(link);
				}

				if (!item.isRead) {
					var readBtn = document.createElement("button");
					readBtn.type = "button";
					readBtn.className = "btn-secondary notification-read-btn";
					readBtn.textContent = "Mark read";
					readBtn.addEventListener("click", function () {
						markNotificationRead(item.id);
					});
					actions.appendChild(readBtn);
				}

				card.appendChild(header);
				card.appendChild(message);
				card.appendChild(actions);
				listContainer.appendChild(card);
			});

			refreshUnreadBadge();
		}

		function upsertRealtimeNotification(pushDto) {
			if (!pushDto || typeof pushDto !== "object") {
				return;
			}

			var incomingId = Number(pushDto.id);
			if (!incomingId) {
				return;
			}

			var existingIndex = notifications.findIndex(function (item) {
				return item.id === incomingId;
			});

			var normalized = {
				id: incomingId,
				type: String(pushDto.type || ""),
				title: String(pushDto.title || "Notification"),
				message: String(pushDto.message || ""),
				linkUrl: pushDto.linkUrl ? String(pushDto.linkUrl) : null,
				isRead: false,
				createdAt: pushDto.createdAt || new Date().toISOString()
			};

			if (existingIndex >= 0) {
				normalized.isRead = notifications[existingIndex].isRead;
				notifications[existingIndex] = normalized;
			} else {
				notifications.unshift(normalized);
			}

			notifications = notifications
				.sort(function (a, b) {
					return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
				})
				.slice(0, maxItems);

			renderNotifications();
		}

		function loadNotifications() {
			if (!listUrl) {
				renderEmptyState("Notification endpoint is unavailable.");
				return;
			}

			return window.fetch(listUrl, {
				method: "GET",
				headers: {
					Accept: "application/json"
				},
				credentials: "same-origin"
			})
				.then(function (response) {
					if (!response.ok) {
						throw new Error("Failed to load notifications.");
					}

					return response.json();
				})
				.then(function (payload) {
					notifications = (Array.isArray(payload) ? payload : [])
						.map(function (item) {
							return {
								id: Number(item.id),
								type: String(item.type || ""),
								title: String(item.title || "Notification"),
								message: String(item.message || ""),
								linkUrl: item.linkUrl ? String(item.linkUrl) : null,
								isRead: Boolean(item.isRead),
								createdAt: item.createdAt || new Date().toISOString()
							};
						})
						.filter(function (item) {
							return !!item.id;
						})
						.sort(function (a, b) {
							return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
						})
						.slice(0, maxItems);

					hasLoaded = true;
					renderNotifications();
				})
				.catch(function () {
					renderEmptyState("Unable to load notifications right now.");
				});
		}

		function openPanel() {
			panel.hidden = false;
			toggleBtn.setAttribute("aria-expanded", "true");
			if (!hasLoaded) {
				loadNotifications();
			}
		}

		function closePanel() {
			panel.hidden = true;
			toggleBtn.setAttribute("aria-expanded", "false");
		}

		toggleBtn.addEventListener("click", function () {
			if (panel.hidden) {
				openPanel();
				return;
			}

			closePanel();
		});

		markAllBtn.addEventListener("click", function () {
			if (!markAllReadUrl) {
				return;
			}

			postApi(markAllReadUrl, {})
				.then(function (result) {
					if (!result.ok) {
						return;
					}

					notifications = notifications.map(function (item) {
						item.isRead = true;
						return item;
					});

					renderNotifications();
				});
		});

		document.addEventListener("click", function (event) {
			if (panel.hidden) {
				return;
			}

			var clickedInside = center.contains(event.target);
			if (!clickedInside) {
				closePanel();
			}
		});

		document.addEventListener("keydown", function (event) {
			if (event.key === "Escape" && !panel.hidden) {
				closePanel();
			}
		});

		if (window.signalR && hubUrl) {
			signalrConnection = new window.signalR.HubConnectionBuilder()
				.withUrl(hubUrl)
				.withAutomaticReconnect()
				.build();

			signalrConnection.on("notification:new", function (payload) {
				upsertRealtimeNotification(payload);
			});

			signalrConnection.start().catch(function () {
				// Keep polling/list mode even if live channel is unavailable.
			});
		}

		window.addEventListener("beforeunload", function () {
			if (signalrConnection) {
				signalrConnection.stop();
			}
		});

		loadNotifications();
	}

	function initializeMobileSidebar() {
		var toggleBtn = document.getElementById("mobile-nav-toggle");
		var sidebar = document.getElementById("app-sidebar");
		var backdrop = document.getElementById("mobile-sidebar-backdrop");

		if (!toggleBtn || !sidebar || !backdrop) {
			return;
		}

		function isMobileViewport() {
			return window.matchMedia("(max-width: 860px)").matches;
		}

		function syncState(isOpen) {
			document.body.classList.toggle("sidebar-open", isOpen);
			backdrop.hidden = !isOpen;
			toggleBtn.setAttribute("aria-expanded", isOpen ? "true" : "false");
		}

		function openSidebar() {
			if (!isMobileViewport()) {
				return;
			}

			syncState(true);
		}

		function closeSidebar() {
			syncState(false);
		}

		toggleBtn.addEventListener("click", function () {
			var isOpen = document.body.classList.contains("sidebar-open");
			if (isOpen) {
				closeSidebar();
				return;
			}

			openSidebar();
		});

		backdrop.addEventListener("click", closeSidebar);

		document.addEventListener("keydown", function (event) {
			if (event.key === "Escape" && document.body.classList.contains("sidebar-open")) {
				closeSidebar();
			}
		});

		sidebar.querySelectorAll("a").forEach(function (link) {
			link.addEventListener("click", function () {
				if (isMobileViewport()) {
					closeSidebar();
				}
			});
		});

		window.addEventListener("resize", function () {
			if (!isMobileViewport()) {
				closeSidebar();
			}
		});

		closeSidebar();
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

		var configuredRefreshThresholdSeconds = parseInt(page.getAttribute("data-refresh-threshold-seconds") || "60", 10);
		if (Number.isNaN(configuredRefreshThresholdSeconds) || configuredRefreshThresholdSeconds < 2) {
			configuredRefreshThresholdSeconds = 60;
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

			studentLastSubmitAt = Date.now();

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
		initializeLazyLoading();
		restoreScrollAfterPostback();
		initializePostbackScrollRetention();
		initializeMobileSidebar();
		initializeTimetableQuickAdd();
		initializePredefinedChatWidget();
		initializeNotificationCenter();
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
