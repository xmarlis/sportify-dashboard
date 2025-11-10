import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Activity } from '../../models/activity.model';

/**
 * Component for creating new activities
 */
@Component({
  selector: 'app-activity-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './activity-form.component.html',
  styleUrls: ['./activity-form.component.css']
})
export class ActivityFormComponent {
  @Output() createActivity = new EventEmitter<Activity>();

  isFormVisible = false;

  // Form model
  newActivity: Activity = {
    type: 'Run',
    distanceMeters: 0,
    movingTimeSeconds: 0,
    totalElevationGain: 0,
    startDate: new Date().toISOString().slice(0, 16) // Format for datetime-local input
  };

  // Temporary fields for easier input
  distanceKm: number = 0;
  hours: number = 0;
  minutes: number = 0;
  seconds: number = 0;

  constructor() {}

  /**
   * Toggle form visibility
   */
  toggleForm(): void {
    this.isFormVisible = !this.isFormVisible;
    if (!this.isFormVisible) {
      this.resetForm();
    }
  }

  /**
   * Handle form submission
   */
  onSubmit(): void {
    // Convert km to meters
    this.newActivity.distanceMeters = this.distanceKm * 1000;

    // Convert hours, minutes, seconds to total seconds
    this.newActivity.movingTimeSeconds =
      (this.hours * 3600) + (this.minutes * 60) + this.seconds;

    // Validate
    if (this.newActivity.distanceMeters <= 0) {
      alert('Distance must be greater than 0');
      return;
    }

    if (this.newActivity.movingTimeSeconds <= 0) {
      alert('Time must be greater than 0');
      return;
    }

    // Emit the event
    this.createActivity.emit({ ...this.newActivity });

    // Reset form
    this.resetForm();
    this.isFormVisible = false;
  }

  /**
   * Reset form to initial state
   */
  private resetForm(): void {
    this.newActivity = {
      type: 'Run',
      distanceMeters: 0,
      movingTimeSeconds: 0,
      totalElevationGain: 0,
      startDate: new Date().toISOString().slice(0, 16)
    };
    this.distanceKm = 0;
    this.hours = 0;
    this.minutes = 0;
    this.seconds = 0;
  }
}
