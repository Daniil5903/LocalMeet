document.addEventListener("DOMContentLoaded", function () {
    const mapElement = document.getElementById("eventDetailsMap");

    if (!mapElement) {
        return;
    }

    const latitude = parseFloat(mapElement.dataset.latitude);
    const longitude = parseFloat(mapElement.dataset.longitude);
    const title = mapElement.dataset.title || "Место проведения";

    if (Number.isNaN(latitude) || Number.isNaN(longitude)) {
        return;
    }

    const map = L.map("eventDetailsMap").setView([latitude, longitude], 15);

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; OpenStreetMap"
    }).addTo(map);

    L.marker([latitude, longitude])
        .addTo(map)
        .bindPopup(title)
        .openPopup();
});