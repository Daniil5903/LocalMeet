(function () {
    var themeKey = "localmeet-theme";
    var root = document.documentElement;

    function getTheme() {
        return root.getAttribute("data-theme") || "light";
    }

    function setTheme(theme) {
        root.setAttribute("data-theme", theme);

        try {
            localStorage.setItem(themeKey, theme);
        } catch (e) {
            // localStorage может быть недоступен в некоторых режимах браузера
        }

        var icons = document.querySelectorAll("[data-theme-icon]");
        for (var i = 0; i < icons.length; i++) {
            icons[i].textContent = theme === "dark" ? "☀" : "☾";
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        setTheme(getTheme());

        var themeButtons = document.querySelectorAll("[data-theme-toggle]");
        for (var i = 0; i < themeButtons.length; i++) {
            themeButtons[i].addEventListener("click", function () {
                setTheme(getTheme() === "dark" ? "light" : "dark");
            });
        }

        var revealItems = document.querySelectorAll("[data-reveal]");

        if ("IntersectionObserver" in window) {
            var observer = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        entry.target.classList.add("is-visible");
                        observer.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.15 });

            revealItems.forEach(function (item) {
                observer.observe(item);
            });
        } else {
            revealItems.forEach(function (item) {
                item.classList.add("is-visible");
            });
        }
    });
})();