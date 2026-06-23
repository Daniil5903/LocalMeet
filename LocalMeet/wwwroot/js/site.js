(function () {
    var themeKey = "localmeet-theme";
    var root = document.documentElement;

    function getTheme() {
        return root.getAttribute("data-theme") || "dark";
    }

    function setTheme(theme) {
        root.setAttribute("data-theme", theme);
        root.setAttribute("data-bs-theme", theme);

        try {
            localStorage.setItem(themeKey, theme);
        } catch (error) {
            // localStorage может быть недоступен в некоторых режимах браузера.
        }

        var icons = document.querySelectorAll("[data-theme-icon]");

        for (var i = 0; i < icons.length; i++) {
            icons[i].textContent = theme === "dark" ? "☀" : "☾";
        }
    }

    function initThemeToggle() {
        setTheme(getTheme());

        var themeButtons = document.querySelectorAll("[data-theme-toggle]");

        for (var i = 0; i < themeButtons.length; i++) {
            themeButtons[i].addEventListener("click", function () {
                setTheme(getTheme() === "dark" ? "light" : "dark");
            });
        }
    }

    function initRevealAnimation() {
        var revealItems = document.querySelectorAll("[data-reveal]");

        if ("IntersectionObserver" in window) {
            var observer = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        entry.target.classList.add("is-visible");
                        observer.unobserve(entry.target);
                    }
                });
            }, {
                threshold: 0.15
            });

            revealItems.forEach(function (item) {
                observer.observe(item);
            });
        } else {
            revealItems.forEach(function (item) {
                item.classList.add("is-visible");
            });
        }
    }

    function initHeaderSearch() {
        var form = document.querySelector("[data-header-search-form]");
        var input = document.querySelector("[data-header-search-input]");
        var results = document.querySelector("[data-header-search-results]");

        if (!form || !input || !results) {
            return;
        }

        var searchUrl = input.getAttribute("data-search-url") || "/Search/Autocomplete";
        var fullSearchUrl = form.getAttribute("action") || "/Search";
        var debounceTimer = null;
        var requestNumber = 0;

        input.addEventListener("input", function () {
            var query = input.value.trim();

            window.clearTimeout(debounceTimer);

            if (query.length < 2) {
                closeResults();
                return;
            }

            debounceTimer = window.setTimeout(function () {
                loadResults(query);
            }, 220);
        });

        input.addEventListener("focus", function () {
            var query = input.value.trim();

            if (query.length >= 2) {
                loadResults(query);
            }
        });

        input.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                closeResults();
                input.blur();
            }
        });

        document.addEventListener("click", function (event) {
            if (!form.contains(event.target)) {
                closeResults();
            }
        });

        form.addEventListener("submit", function () {
            closeResults();
        });

        function loadResults(query) {
            if (!window.fetch) {
                return;
            }

            requestNumber++;
            var currentRequestNumber = requestNumber;

            showLoading();

            fetch(searchUrl + "?query=" + encodeURIComponent(query), {
                method: "GET",
                headers: {
                    "Accept": "application/json"
                }
            })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error("Ошибка поиска");
                    }

                    return response.json();
                })
                .then(function (data) {
                    if (currentRequestNumber !== requestNumber) {
                        return;
                    }

                    renderResults(data.items || [], query);
                })
                .catch(function () {
                    if (currentRequestNumber !== requestNumber) {
                        return;
                    }

                    renderError();
                });
        }

        function showLoading() {
            results.innerHTML = "";

            var loading = document.createElement("div");
            loading.className = "app-header-search-state";
            loading.textContent = "Поиск...";

            results.appendChild(loading);
            openResults();
        }

        function renderResults(items, query) {
            results.innerHTML = "";

            if (!items.length) {
                var empty = document.createElement("div");
                empty.className = "app-header-search-state";
                empty.textContent = "Ничего не найдено";

                results.appendChild(empty);
                appendFullSearchLink(query);
                openResults();

                return;
            }

            for (var i = 0; i < items.length; i++) {
                results.appendChild(createResultItem(items[i]));
            }

            appendFullSearchLink(query);
            openResults();
        }

        function renderError() {
            results.innerHTML = "";

            var error = document.createElement("div");
            error.className = "app-header-search-state app-header-search-error";
            error.textContent = "Не удалось выполнить поиск";

            results.appendChild(error);
            openResults();
        }

        function createResultItem(item) {
            var link = document.createElement("a");
            link.className = "app-header-search-item";
            link.href = item.url || "#";

            var icon = document.createElement("span");
            icon.className = "app-header-search-item-icon";

            if (item.type === "user" && item.avatarPath) {
                var avatar = document.createElement("img");
                avatar.src = item.avatarPath;
                avatar.alt = "";

                icon.appendChild(avatar);
            } else {
                icon.textContent = item.type === "user" ? "👤" : "📍";
            }

            var content = document.createElement("span");
            content.className = "app-header-search-item-content";

            var top = document.createElement("span");
            top.className = "app-header-search-item-top";

            var title = document.createElement("strong");
            title.textContent = item.title || "Без названия";

            var type = document.createElement("span");
            type.className = "app-header-search-item-type";
            type.textContent = item.typeText || "";

            top.appendChild(title);
            top.appendChild(type);

            var subtitle = document.createElement("span");
            subtitle.className = "app-header-search-item-subtitle";
            subtitle.textContent = item.subtitle || "";

            var meta = document.createElement("span");
            meta.className = "app-header-search-item-meta";
            meta.textContent = item.meta || "";

            content.appendChild(top);
            content.appendChild(subtitle);

            if (item.meta) {
                content.appendChild(meta);
            }

            link.appendChild(icon);
            link.appendChild(content);

            return link;
        }

        function appendFullSearchLink(query) {
            var separator = document.createElement("div");
            separator.className = "app-header-search-separator";

            var link = document.createElement("a");
            link.className = "app-header-search-all";
            link.href = fullSearchUrl + "?query=" + encodeURIComponent(query);
            link.textContent = "Показать все результаты";

            results.appendChild(separator);
            results.appendChild(link);
        }

        function openResults() {
            results.classList.add("is-open");
        }

        function closeResults() {
            results.classList.remove("is-open");
            results.innerHTML = "";
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        initThemeToggle();
        initRevealAnimation();
        initHeaderSearch();
    });
})();