document.addEventListener("DOMContentLoaded", function () {
    const mapElement = document.getElementById("eventCreateMap");

    if (!mapElement) {
        return;
    }

    const latitudeInput = document.getElementById("Latitude");
    const longitudeInput = document.getElementById("Longitude");
    const addressInput = document.getElementById("Address");

    const defaultLatitude = 59.4370;
    const defaultLongitude = 24.7536;

    const map = L.map("eventCreateMap").setView([defaultLatitude, defaultLongitude], 12);

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; OpenStreetMap"
    }).addTo(map);

    let marker = null;

    if (latitudeInput.value && longitudeInput.value) {
        const lat = parseFloat(latitudeInput.value.replace(",", "."));
        const lng = parseFloat(longitudeInput.value.replace(",", "."));

        if (!Number.isNaN(lat) && !Number.isNaN(lng)) {
            marker = L.marker([lat, lng]).addTo(map);
            map.setView([lat, lng], 15);
        }
    }

    map.on("click", async function (event) {
        const lat = event.latlng.lat;
        const lng = event.latlng.lng;

        latitudeInput.value = lat.toFixed(7);
        longitudeInput.value = lng.toFixed(7);

        if (marker) {
            marker.setLatLng([lat, lng]);
        } else {
            marker = L.marker([lat, lng]).addTo(map);
        }

        await fillAddressByCoordinates(lat, lng);
    });

    async function fillAddressByCoordinates(latitude, longitude) {
        if (!addressInput) {
            return;
        }

        addressInput.value = "Определение адреса...";

        const url =
            `https://nominatim.openstreetmap.org/reverse?format=jsonv2` +
            `&lat=${encodeURIComponent(latitude)}` +
            `&lon=${encodeURIComponent(longitude)}` +
            `&addressdetails=1` +
            `&accept-language=ru`;

        try {
            const response = await fetch(url, {
                method: "GET",
                headers: {
                    "Accept": "application/json"
                }
            });

            if (!response.ok) {
                addressInput.value = "";
                return;
            }

            const data = await response.json();
            const address = data.address;

            if (!address) {
                addressInput.value = "";
                return;
            }

            const region =
                address.state ||
                address.region ||
                address.county ||
                "";

            const city =
                address.city ||
                address.town ||
                address.village ||
                address.hamlet ||
                address.municipality ||
                "";

            const street =
                address.road ||
                address.pedestrian ||
                address.footway ||
                address.path ||
                "";

            const formattedAddress = buildShortAddress([
                region,
                city,
                street
            ]);

            addressInput.value = formattedAddress || "";
        } catch {
            addressInput.value = "";
        }
    }

    function buildShortAddress(parts) {
        const result = [];
        const used = new Set();

        parts.forEach(function (part) {
            if (!part) {
                return;
            }

            const normalized = part.trim().toLowerCase();

            if (!normalized || used.has(normalized)) {
                return;
            }

            used.add(normalized);
            result.push(part.trim());
        });

        return result.join(", ");
    }
});