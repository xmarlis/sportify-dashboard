import { Component, OnInit, OnDestroy, Input, AfterViewInit, OnChanges, SimpleChanges } from '@angular/core';
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
 * Get color for activity type - matching Python script colors
 */
function getActivityColor(type: string): string {
  const colors: { [key: string]: string } = {
    'Run': '#ef4444',      // Red
    'Ride': '#3b82f6',     // Blue
    'Walk': '#10b981',     // Green
    'Hike': '#84cc16',     // Lime
    'Swim': '#06b6d4',     // Cyan
    'VirtualRide': '#8b5cf6', // Purple
    'VirtualRun': '#ec4899',  // Pink
    'Workout': '#f59e0b',     // Amber
    'WeightTraining': '#78716c', // Stone
    'Yoga': '#14b8a6',        // Teal
    'Rowing': '#0ea5e9',      // Sky
    'Kayaking': '#0891b2',    // Cyan dark
    'StandUpPaddling': '#22d3ee', // Cyan light
    'Surfing': '#2dd4bf',     // Teal light
    'Snowboard': '#a78bfa',   // Violet
    'Ski': '#c084fc',         // Purple light
    'IceSkate': '#67e8f9',    // Cyan lightest
    'Skateboard': '#fbbf24',  // Yellow
    'InlineSkate': '#fb923c', // Orange
    'Crossfit': '#f97316',    // Orange dark
    'Elliptical': '#a3e635',  // Lime light
    'RockClimbing': '#854d0e', // Amber dark
    'Golf': '#4ade80',        // Green light
    'Handcycle': '#7c3aed',   // Violet dark
    'Wheelchair': '#6366f1',  // Indigo
  };
  return colors[type] || '#64748b'; // Slate default
}

@Component({
  selector: 'app-activity-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activity-map.component.html',
  styleUrls: ['./activity-map.component.css']
})
export class ActivityMapComponent implements OnInit, AfterViewInit, OnDestroy, OnChanges {
  @Input() activities: Activity[] = [];
  @Input() height = '500px';
  @Input() filterTypes: string[] = [];

  private map: L.Map | null = null;
  private routeLayers: L.LayerGroup = L.layerGroup();
  private mapInitialized = false;

  activityTypes: { type: string; color: string; count: number; visible: boolean }[] = [];
  totalDistance = 0;
  totalActivities = 0;

  ngOnInit(): void {
    this.updateStats();
  }

  ngAfterViewInit(): void {
    this.initMap();
    this.mapInitialized = true;
    this.updateActivityTypes();
    this.drawRoutes();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['activities'] && this.mapInitialized) {
      this.updateStats();
      this.updateActivityTypes();
      this.drawRoutes();
    }
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
  }

  private updateStats(): void {
    const activitiesWithRoutes = this.activities.filter(a => a.summaryPolyline);
    this.totalActivities = activitiesWithRoutes.length;
    this.totalDistance = activitiesWithRoutes.reduce((sum, a) => sum + (a.distanceMeters || 0), 0) / 1000;
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

    // Use CartoDB Positron tiles (cleaner look like Python example)
    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>',
      subdomains: 'abcd',
      maxZoom: 20
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

    this.activityTypes = Array.from(typeMap.entries())
      .sort((a, b) => b[1] - a[1]) // Sort by count descending
      .map(([type, count]) => ({
        type,
        color: getActivityColor(type),
        count,
        visible: true
      }));
  }

  drawRoutes(): void {
    if (!this.map) return;

    this.routeLayers.clearLayers();
    let hasRoutes = false;

    const visibleTypes = this.activityTypes
      .filter(t => t.visible)
      .map(t => t.type);

    const activitiesToShow = this.activities.filter(
      activity => activity.summaryPolyline && visibleTypes.includes(activity.type)
    );

    console.log(`Drawing ${activitiesToShow.length} routes out of ${this.activities.length} activities`);

    activitiesToShow.forEach(activity => {
      if (!activity.summaryPolyline) return;

      try {
        const coordinates = decodePolyline(activity.summaryPolyline);
        if (coordinates.length === 0) return;

        hasRoutes = true;
        const color = getActivityColor(activity.type);

        const polyline = L.polyline(coordinates as L.LatLngExpression[], {
          color: color,
          weight: 2,
          opacity: 0.7
        });

        // Add popup with activity info
        const date = new Date(activity.startDate).toLocaleDateString('en-US', {
          year: 'numeric',
          month: 'short',
          day: 'numeric'
        });
        const distance = (activity.distanceMeters / 1000).toFixed(2);
        const duration = this.formatDuration(activity.movingTimeSeconds);

        polyline.bindPopup(`
          <div style="min-width: 180px; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;">
            <div style="font-weight: 600; font-size: 14px; margin-bottom: 4px;">${activity.name || activity.type}</div>
            <div style="color: #64748b; font-size: 12px; margin-bottom: 8px;">${date}</div>
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 8px; font-size: 13px;">
              <div><span style="color: #64748b;">Distance:</span><br><strong>${distance} km</strong></div>
              <div><span style="color: #64748b;">Time:</span><br><strong>${duration}</strong></div>
            </div>
            <div style="margin-top: 8px; padding-top: 8px; border-top: 1px solid #e2e8f0;">
              <span style="display: inline-block; padding: 2px 8px; background: ${color}; color: white; border-radius: 4px; font-size: 11px;">${activity.type}</span>
            </div>
          </div>
        `);

        this.routeLayers.addLayer(polyline);
      } catch (e) {
        console.error('Error drawing route:', e);
      }
    });

    // Fit map to show all routes
    if (hasRoutes && activitiesToShow.length > 0) {
      const allCoords: L.LatLng[] = [];
      activitiesToShow.forEach(activity => {
        if (activity.summaryPolyline) {
          try {
            const coords = decodePolyline(activity.summaryPolyline);
            coords.forEach(([lat, lng]) => {
              allCoords.push(L.latLng(lat, lng));
            });
          } catch (e) {
            // Skip invalid polylines
          }
        }
      });

      if (allCoords.length > 0) {
        const bounds = L.latLngBounds(allCoords);
        this.map.fitBounds(bounds, { padding: [30, 30] });
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

  showAll(): void {
    this.activityTypes.forEach(t => t.visible = true);
    this.drawRoutes();
  }

  hideAll(): void {
    this.activityTypes.forEach(t => t.visible = false);
    this.drawRoutes();
  }

  private formatDuration(seconds: number): string {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;

    if (hours > 0) {
      return `${hours}h ${minutes}m`;
    }
    return `${minutes}m ${secs}s`;
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

  getTotalDistance(): string {
    return this.totalDistance.toFixed(0);
  }
}
