(function () {
    "use strict";

    const root = document.documentElement;
    const themeToggle = document.getElementById("themeToggle");
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    function updateThemeButton() {
        if (!themeToggle) return;
        const isDark = root.dataset.theme === "dark";
        const icon = themeToggle.querySelector(".theme-icon");
        if (icon) icon.textContent = isDark ? "☀" : "☾";
        themeToggle.setAttribute("aria-pressed", String(isDark));
    }

    if (themeToggle) {
        updateThemeButton();
        themeToggle.addEventListener("click", function () {
            const nextTheme = root.dataset.theme === "dark" ? "light" : "dark";
            root.dataset.theme = nextTheme;
            localStorage.setItem("hotel-theme", nextTheme);
            updateThemeButton();
        });
    }

    const siteHeader = document.querySelector("[data-site-header]");
    function updateHeader() {
        if (siteHeader) siteHeader.classList.toggle("scrolled", window.scrollY > 14);
    }
    updateHeader();
    window.addEventListener("scroll", updateHeader, { passive: true });

    document.querySelectorAll("[data-date-checkin]").forEach(function (input) {
        input.min = formatDate(today);
        input.addEventListener("change", syncCheckOutDates);
        input.addEventListener("input", updateAllBookingPreviews);
    });

    document.querySelectorAll("[data-date-checkout]").forEach(function (input) {
        if (!input.min) input.min = addDays(today, 1);
        input.addEventListener("change", updateAllBookingPreviews);
        input.addEventListener("input", updateAllBookingPreviews);
    });

    function syncCheckOutDates() {
        document.querySelectorAll("form").forEach(function (form) {
            const checkIn = form.querySelector("[data-date-checkin]");
            const checkOut = form.querySelector("[data-date-checkout]");
            if (!checkIn || !checkOut) return;

            const baseDate = parseDate(checkIn.value) || today;
            const minimumCheckOut = addDays(baseDate, 1);
            checkOut.min = minimumCheckOut;
            if (!checkOut.value || checkOut.value < minimumCheckOut) {
                checkOut.value = minimumCheckOut;
            }
        });
        updateAllBookingPreviews();
    }

    document.querySelectorAll("[data-booking-form]").forEach(function (form) {
        ["input", "change"].forEach(function (eventName) {
            form.addEventListener(eventName, function (event) {
                if (event.target.matches("[data-guests], [data-breakfast], [data-pickup], [data-promo]")) {
                    updateBookingPreview(form);
                }
            });
        });

        const applyButton = form.querySelector("[data-apply-promo]");
        if (applyButton) {
            applyButton.addEventListener("click", function () {
                updateBookingPreview(form, true);
            });
        }
    });

    function updateAllBookingPreviews() {
        document.querySelectorAll("[data-booking-form]").forEach(function (form) {
            updateBookingPreview(form);
        });
    }

    function updateBookingPreview(form, showPromoFeedback) {
        const scope = form.closest(".booking-checkout-layout") || form;
        const checkIn = parseDate(form.querySelector("[data-date-checkin]")?.value);
        const checkOut = parseDate(form.querySelector("[data-date-checkout]")?.value);
        const price = numberValue(form.dataset.price);
        const breakfastPrice = numberValue(form.dataset.breakfastPrice);
        const pickupPrice = numberValue(form.dataset.pickupPrice);
        const guests = Math.max(1, Number.parseInt(form.querySelector("[data-guests]")?.value || "1", 10));
        const breakfast = Boolean(form.querySelector("[data-breakfast]")?.checked);
        const pickup = Boolean(form.querySelector("[data-pickup]")?.checked);
        const promoInput = form.querySelector("[data-promo]");
        const promo = (promoInput?.value || "").trim().toUpperCase();

        let nights = 0;
        if (checkIn && checkOut) {
            nights = Math.max(0, Math.round((checkOut.getTime() - checkIn.getTime()) / 86400000));
        }

        const subtotal = price * nights;
        const extras = (breakfast ? breakfastPrice * guests * nights : 0) + (pickup ? pickupPrice : 0);
        let discountRate = 0;
        let validPromo = false;
        if (promo === "AURORA10") {
            discountRate = 0.10;
            validPromo = true;
        } else if (promo === "STAY3" && nights >= 3) {
            discountRate = 0.15;
            validPromo = true;
        }
        const discount = subtotal * discountRate;
        const total = Math.max(0, subtotal + extras - discount);

        setAllText(scope, "[data-night-count]", String(nights));
        setAllText(scope, "[data-subtotal]", formatMoney(subtotal));
        setAllText(scope, "[data-extras]", formatMoney(extras));
        setAllText(scope, "[data-discount]", formatMoney(discount));
        setAllText(scope, "[data-total-price]", formatMoney(total));
        setAllText(scope, "[data-summary-checkin]", checkIn ? shortDate(checkIn) : "—");
        setAllText(scope, "[data-summary-checkout]", checkOut ? shortDate(checkOut) : "—");

        const feedback = form.querySelector("[data-promo-feedback]");
        if (feedback && (showPromoFeedback || promo.length > 0)) {
            feedback.classList.remove("valid", "invalid");
            if (!promo) {
                feedback.textContent = "";
            } else if (validPromo) {
                feedback.textContent = feedback.dataset.valid || "Discount applied";
                feedback.classList.add("valid");
            } else {
                feedback.textContent = feedback.dataset.invalid || "Invalid code";
                feedback.classList.add("invalid");
            }
        }
    }

    function setAllText(scope, selector, value) {
        scope.querySelectorAll(selector).forEach(function (target) {
            target.textContent = value;
        });
    }

    document.querySelectorAll("[data-favorite-room]").forEach(function (button) {
        const roomId = String(button.dataset.favoriteRoom || "");
        const storageKey = "aurora-favorite-rooms";
        const favorites = readFavorites(storageKey);
        renderFavorite(button, favorites.includes(roomId));
        button.addEventListener("click", function () {
            const current = readFavorites(storageKey);
            const index = current.indexOf(roomId);
            if (index >= 0) current.splice(index, 1);
            else current.push(roomId);
            localStorage.setItem(storageKey, JSON.stringify(current));
            document.querySelectorAll(`[data-favorite-room="${roomId}"]`).forEach(function (matchingButton) {
                renderFavorite(matchingButton, index < 0);
            });
        });
    });

    function readFavorites(key) {
        try {
            const parsed = JSON.parse(localStorage.getItem(key) || "[]");
            return Array.isArray(parsed) ? parsed.map(String) : [];
        } catch {
            return [];
        }
    }

    function renderFavorite(button, isFavorite) {
        button.classList.toggle("active", isFavorite);
        button.textContent = isFavorite ? "♥" : "♡";
        button.setAttribute("aria-pressed", String(isFavorite));
    }

    document.querySelectorAll("[data-copy-code]").forEach(function (button) {
        button.addEventListener("click", async function () {
            const code = button.dataset.copyCode || "";
            try {
                await navigator.clipboard.writeText(code);
                const original = button.textContent;
                button.textContent = "✓";
                window.setTimeout(function () { button.textContent = original; }, 1200);
            } catch {
                button.textContent = code;
            }
        });
    });

    document.querySelectorAll("[data-password-toggle]").forEach(function (button) {
        button.addEventListener("click", function () {
            const input = button.parentElement?.querySelector("input");
            if (!input) return;
            const show = input.type === "password";
            input.type = show ? "text" : "password";
            button.textContent = show ? "◌" : "◉";
        });
    });

    document.querySelectorAll("[data-alert-close]").forEach(function (button) {
        button.addEventListener("click", function () {
            button.closest(".app-alert")?.remove();
        });
    });

    document.querySelectorAll(".app-alert:not(.inline-alert)").forEach(function (alert) {
        window.setTimeout(function () {
            alert.style.opacity = "0";
            alert.style.transform = "translateY(-7px)";
            window.setTimeout(function () { alert.remove(); }, 220);
        }, 5200);
    });

    document.querySelectorAll("[data-print-booking]").forEach(function (button) {
        button.addEventListener("click", function () { window.print(); });
    });

    const revealItems = document.querySelectorAll(".reveal");
    if ("IntersectionObserver" in window) {
        const revealObserver = new IntersectionObserver(function (entries, observer) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                entry.target.classList.add("visible");
                observer.unobserve(entry.target);
            });
        }, { threshold: 0.08, rootMargin: "0px 0px -30px 0px" });
        revealItems.forEach(function (item, index) {
            item.style.transitionDelay = `${Math.min(index % 4, 3) * 65}ms`;
            revealObserver.observe(item);
        });
    } else {
        revealItems.forEach(function (item) { item.classList.add("visible"); });
    }

    syncCheckOutDates();
    updateAllBookingPreviews();

    function parseDate(value) {
        if (!value) return null;
        const parts = value.split("-").map(Number);
        if (parts.length !== 3 || parts.some(Number.isNaN)) return null;
        return new Date(parts[0], parts[1] - 1, parts[2]);
    }

    function addDays(date, days) {
        const result = new Date(date.getTime());
        result.setDate(result.getDate() + days);
        return formatDate(result);
    }

    function formatDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");
        return `${year}-${month}-${day}`;
    }

    function shortDate(date) {
        try {
            return new Intl.DateTimeFormat(document.documentElement.lang || "en", { day: "2-digit", month: "short" }).format(date);
        } catch {
            return formatDate(date);
        }
    }

    function numberValue(value) {
        const parsed = Number.parseFloat(value || "0");
        return Number.isFinite(parsed) ? parsed : 0;
    }

    function formatMoney(value) {
        return value.toFixed(2).replace(/\.00$/, "");
    }
})();
