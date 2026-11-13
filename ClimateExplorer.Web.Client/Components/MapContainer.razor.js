// Draws the location markers on the Leaflet map. Each location carries the lowest zoom it shows at
// (MapMarkerZoomTiers), so markers don't overlap: zoomed out, only locations with the longest records show.
// Only markers in or near the view are on the map; panning or zooming adds and removes them.

const iconSize = [48, 48];
const iconAnchor = [23, 47];
const icons = new Map();

function getIcon(label) {
    let icon = icons.get(label);
    if (!icon) {
        icon = L.icon({ iconUrl: `/images/map-markers/${label}.png`, iconSize, iconAnchor });
        icons.set(label, icon);
    }
    return icon;
}

function isInView(bounds, lat, lng) {
    if (lat < bounds.getSouth() || lat > bounds.getNorth()) {
        return false;
    }

    const west = bounds.getWest();
    const east = bounds.getEast();
    if (east - west >= 360) {
        return true;
    }

    // Leaflet doesn't wrap the bounds' longitudes, so a view across the antimeridian runs past ±180.
    return [lng, lng - 360, lng + 360].some(x => x >= west && x <= east);
}

export function createMarkerLayer(map, dotNetRef, locations, selectedId) {
    if (!map) {
        throw new Error("createMarkerLayer: the Leaflet map has not been created yet");
    }

    const layer = L.layerGroup().addTo(map);
    const markers = new Map();
    let items = locations;
    let selected = selectedId;

    function createMarker(location) {
        const marker = L.marker([location.lat, location.lng], {
            icon: getIcon(location.label),
            alt: location.title,
            opacity: 0.75,
        });

        // An element rather than a string, so the name is shown as text, not parsed as HTML.
        const tooltip = document.createElement("span");
        tooltip.textContent = location.title;
        marker.bindTooltip(tooltip);
        marker.on("click", () => dotNetRef.invokeMethodAsync("OnMarkerClick", location.id));
        return marker;
    }

    function clear() {
        layer.clearLayers();
        markers.clear();
    }

    function refresh() {
        const zoom = map.getZoom();
        const bounds = map.getBounds().pad(0.1);
        const wanted = new Set();

        for (const location of items) {
            if ((location.minZoom > zoom && location.id !== selected) || !isInView(bounds, location.lat, location.lng)) {
                continue;
            }

            wanted.add(location.id);
            if (!markers.has(location.id)) {
                const marker = createMarker(location);
                markers.set(location.id, marker);
                layer.addLayer(marker);
            }
        }

        for (const [id, marker] of markers) {
            if (!wanted.has(id)) {
                layer.removeLayer(marker);
                markers.delete(id);
            }
        }
    }

    map.on("moveend", refresh);
    refresh();

    return {
        setLocations(newLocations, newSelectedId) {
            items = newLocations;
            selected = newSelectedId;
            clear();
            refresh();
        },
        setSelected(newSelectedId) {
            selected = newSelectedId;
            refresh();
        },
        dispose() {
            map.off("moveend", refresh);
            clear();
            layer.remove();
        },
    };
}
