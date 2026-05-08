window.LeafletMap = {
    maps: {},

    initialize: function (elementId, coordinates, editable, dotNetRef) {
        const map = L.map(elementId).setView([47.695, 8.635], 13);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        }).addTo(map);

        this.maps[elementId] = { map, dotNetRef, layers: [], editable };

        if (coordinates && coordinates.length > 0) {
            this._render(elementId, coordinates);
        }

        if (editable) {
            map.on('click', (e) => {
                dotNetRef.invokeMethodAsync('HandleMapClick', e.latlng.lat, e.latlng.lng);
            });
        }
    },

    update: function (elementId, coordinates) {
        if (!this.maps[elementId]) return;
        this._render(elementId, coordinates);
    },

    _render: function (elementId, coordinates) {
        const entry = this.maps[elementId];
        const { map, dotNetRef, editable } = entry;

        entry.layers.forEach(l => map.removeLayer(l));
        entry.layers = [];

        if (!coordinates || coordinates.length === 0) return;

        const latlngs = coordinates.map(c => [c.latitude, c.longitude]);

        // Draw shape underneath the vertex markers
        if (coordinates.length === 2) {
            const line = L.polyline(latlngs, { color: '#1565C0', weight: 4, interactive: false }).addTo(map);
            entry.layers.push(line);
            map.fitBounds(line.getBounds(), { padding: [30, 30] });
        } else if (coordinates.length >= 3) {
            const polygon = L.polygon(latlngs, { color: '#1565C0', weight: 3, fillOpacity: 0.15, interactive: false }).addTo(map);
            entry.layers.push(polygon);
            map.fitBounds(polygon.getBounds(), { padding: [30, 30] });
        }

        // Draw a vertex marker for every coordinate
        latlngs.forEach((latlng, index) => {
            let marker;

            if (coordinates.length === 1) {
                marker = L.marker(latlng);
            } else {
                marker = L.circleMarker(latlng, {
                    radius: 7,
                    color: '#1565C0',
                    fillColor: '#ffffff',
                    fillOpacity: 1,
                    weight: 2
                });
            }

            if (editable) {
                marker.bindTooltip('Klicken zum Entfernen', { direction: 'top' });
                marker.on('click', (e) => {
                    L.DomEvent.stopPropagation(e); // prevent also firing map click
                    dotNetRef.invokeMethodAsync('HandleMarkerClick', index);
                });
            }

            marker.addTo(map);
            entry.layers.push(marker);
        });

        if (coordinates.length === 1) {
            map.setView(latlngs[0], Math.max(map.getZoom(), 14));
        }
    },

    destroy: function (elementId) {
        if (this.maps[elementId]) {
            this.maps[elementId].map.remove();
            delete this.maps[elementId];
        }
    }
};
