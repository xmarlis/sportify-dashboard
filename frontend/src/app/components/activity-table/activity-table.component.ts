import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Activity } from '../../models/activity.model';

/**
 * Component for displaying activities in a table
 */
@Component({
  selector: 'app-activity-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activity-table.component.html',
  styleUrls: ['./activity-table.component.css']
})
export class ActivityTableComponent implements OnInit {
  @Input() activities: Activity[] = [];
  @Output() deleteActivity = new EventEmitter<number>();

  constructor() {}

  ngOnInit(): void {}

  /**
   * Convert distance from meters to kilometers
   */
  getDistanceInKm(distanceMeters: number): string {
    return (distanceMeters / 1000).toFixed(2);
  }

  /**
   * Format time in seconds to hh:mm:ss
   */
  formatTime(seconds: number): string {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;

    return `${this.pad(hours)}:${this.pad(minutes)}:${this.pad(secs)}`;
  }

  /**
   * Calculate average pace in min/km
   */
  getAveragePace(distanceMeters: number, movingTimeSeconds: number): string {
    if (distanceMeters === 0) return 'N/A';

    const distanceKm = distanceMeters / 1000;
    const timeMinutes = movingTimeSeconds / 60;
    const paceMinPerKm = timeMinutes / distanceKm;

    const minutes = Math.floor(paceMinPerKm);
    const seconds = Math.floor((paceMinPerKm - minutes) * 60);

    return `${minutes}:${this.pad(seconds)}`;
  }

  /**
   * Format date for display
   */
  formatDate(date: string | Date): string {
    const d = new Date(date);
    return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  /**
   * Get badge class for activity type
   */
  getTypeBadgeClass(type: string): string {
    switch (type.toLowerCase()) {
      case 'run':
        return 'badge bg-primary';
      case 'ride':
        return 'badge bg-success';
      default:
        return 'badge bg-secondary';
    }
  }

  /**
   * Pad number with leading zero
   */
  private pad(num: number): string {
    return num < 10 ? '0' + num : '' + num;
  }

  /**
   * Handle delete button click
   */
  onDelete(id: number | undefined): void {
    if (id && confirm('Are you sure you want to delete this activity?')) {
      this.deleteActivity.emit(id);
    }
  }
}
