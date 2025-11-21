import { Component, OnInit, OnDestroy, Input, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';
import { Activity } from '../../models/activity.model';

/**
 * Decode Google encoded polyline to array of coordinates
 */
function decodePolyline(encoded: string): [number, number][] {
  const points: [number, number][] = [];
  let index = 0;
  let lat = 0;
  let lng = 0;

  while (index < encoded.length) {
    let shift = 0;
    let result = 0;
    let byte: number;

    do {
      byte = encoded.charCodeAt(index++) - 63;
      result |= (byte & 0x1f) << shift;
      shift += 5;
    } while (byte >= 0x20);

    const dlat = result & 1 ? ~(result >> 1) : result >> 1;
    lat += dlat;

    shift = 0;
    result = 0;

    do {
      byte = encoded.charCodeAt(index++) - 63;
      result |= (byte & 0x1f) << shift;
      shift += 5;
    } while (byte >= 0x20);

    const dlng = result & 1 ? ~(result >> 1) : result >> 1;
    lng += dlng;

    points.push([lat / 1e5, lng / 1e5]);
  }

  return points;
}

/**
 * Get color for activity type
 */
function getActivityColor(type: string): string {
  const colors: { [key: string]: string } = {
    'Run': '#FF5722',
    'Ride': '#2196F3',
    'Walk': '#4CAF50',
    'Hike': '#8BC34A',
    'Swim': '#00BCD4',
    'VirtualRide': '#9C27B0',
    'VirtualRun': '#E91E63',
    'Workout': '#FFC107',
    'WeightTraining': '#795548',
    'Yoga': '#009688'
  };
  return colors[type] || '#607D8B';
}

@Component({
  selector: 'app-activity-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activity-map.component.html',
  styleUrls: ['./activity-map.component.css']
})
export class ActivityMapComponent implements OnInit, AfterViewInit, OnDestroy {
  @Input() activities: Activity[] = [];
  @Input() height = '500px';
  @Input() filterTypes: string[] = [];

  private map: L.Map | null = null;
  private routeLayers: L.LayerGroup = L.layerGroup();

  activityTypes: { type: string; color: string; count: number; visible: boolean }[] = [];

  ngOnInit(): void {
    this.updateActivityTypes();
  }

  ngAfterViewInit(): void {
    this.initMap();
    this.drawRoutes();
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
  }

  private initMap(): void {
    // Fix Leaflet default icon issue
    delete (L.Icon.Default.prototype as any)._getIconUrl;
    L.Icon.Default.mergeOptions({
      iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
      iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
      shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png'
    });

    this.map = L.map('activity-map', {
      center: [48.2082, 16.3738], // Default: Vienna
      zoom: 10
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(this.map);

    this.routeLayers.addTo(this.map);
  }

  private updateActivityTypes(): void {
    const typeMap = new Map<string, number>();

    this.activities.forEach(activity => {
      if (activity.summaryPolyline) {
        const count = typeMap.get(activity.type) || 0;
        typeMap.set(activity.type, count + 1);
      }
    });

    this.activityTypes = Array.from(typeMap.entries()).map(([type, count]) => ({
      type,
      color: getActivityColor(type),
      count,
      visible: true
    }));
  }

  drawRoutes(): void {
    if (!this.map) return;

    this.routeLayers.clearLayers();
    const bounds: L.LatLngBounds | null = null;
    let hasRoutes = false;

    const visibleTypes = this.activityTypes
      .filter(t => t.visible)
      .map(t => t.type);

    const activitiesToShow = this.activities.filter(
      activity => activity.summaryPolyline && visibleTypes.includes(activity.type)
    );

    activitiesToShow.forEach(activity => {
      if (!activity.summaryPolyline) return;

      const coordinates = decodePolyline(activity.summaryPolyline);
      if (coordinates.length === 0) return;

      hasRoutes = true;
      const color = getActivityColor(activity.type);

      const polyline = L.polyline(coordinates as L.LatLngExpression[], {
        color: color,
        weight: 3,
        opacity: 0.7
      });

      // Add popup with activity info
      const date = new Date(activity.startDate).toLocaleDateString('en-US');
      const distance = (activity.distanceMeters / 1000).toFixed(2);
      const duration = this.formatDuration(activity.movingTimeSeconds);

      polyline.bindPopup(`
        <div style="min-width: 150px;">
          <strong>${activity.name || activity.type}</strong><br>
          <small>${date}</small><br>
          <hr style="margin: 5px 0;">
          <b>Distance:</b> ${distance} km<br>
          <b>Time:</b> ${duration}<br>
          <b>Type:</b> ${activity.type}
        </div>
      `);

      this.routeLayers.addLayer(polyline);
    });

    // Fit map to show all routes
    if (hasRoutes && activitiesToShow.length > 0) {
      const allCoords: L.LatLng[] = [];
      activitiesToShow.forEach(activity => {
        if (activity.summaryPolyline) {
          const coords = decodePolyline(activity.summaryPolyline);
          coords.forEach(([lat, lng]) => {
            allCoords.push(L.latLng(lat, lng));
          });
        }
      });

      if (allCoords.length > 0) {
        const bounds = L.latLngBounds(allCoords);
        this.map.fitBounds(bounds, { padding: [20, 20] });
      }
    }
  }

  toggleActivityType(type: string): void {
    const activityType = this.activityTypes.find(t => t.type === type);
    if (activityType) {
      activityType.visible = !activityType.visible;
      this.drawRoutes();
    }
  }

  private formatDuration(seconds: number): string {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;

    if (hours > 0) {
      return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }
    return `${minutes}:${secs.toString().padStart(2, '0')}`;
  }

  getRoutesCount(): number {
    return this.activities.filter(a => a.summaryPolyline).length;
  }

  getVisibleRoutesCount(): number {
    const visibleTypes = this.activityTypes
      .filter(t => t.visible)
      .map(t => t.type);
    return this.activities.filter(
      a => a.summaryPolyline && visibleTypes.includes(a.type)
    ).length;
  }
}
