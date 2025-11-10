import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { ActivityService } from './services/activity.service';
import { Activity } from './models/activity.model';
import { ActivityTableComponent } from './components/activity-table/activity-table.component';
import { ActivityFormComponent } from './components/activity-form/activity-form.component';
import { StravaConnectComponent } from './components/strava-connect/strava-connect.component';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, ActivityTableComponent, ActivityFormComponent, StravaConnectComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  title = 'Running Dashboard';
  activities: Activity[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(private activityService: ActivityService) {}

  ngOnInit(): void {
    this.loadActivities();
  }

  /**
   * Load all activities from the backend
   */
  loadActivities(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.activityService.getActivities().subscribe({
      next: (data) => {
        this.activities = data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading activities:', error);
        this.errorMessage = 'Failed to load activities. Make sure the backend is running.';
        this.isLoading = false;
      }
    });
  }

  /**
   * Create a new activity
   */
  onCreateActivity(activity: Activity): void {
    this.activityService.createActivity(activity).subscribe({
      next: (createdActivity) => {
        console.log('Activity created:', createdActivity);
        this.loadActivities(); // Reload activities
      },
      error: (error) => {
        console.error('Error creating activity:', error);
        alert('Failed to create activity. Please try again.');
      }
    });
  }

  /**
   * Delete an activity
   */
  onDeleteActivity(id: number): void {
    this.activityService.deleteActivity(id).subscribe({
      next: () => {
        console.log('Activity deleted:', id);
        this.loadActivities(); // Reload activities
      },
      error: (error) => {
        console.error('Error deleting activity:', error);
        alert('Failed to delete activity. Please try again.');
      }
    });
  }

  /**
   * Handle activities imported from Strava
   */
  onActivitiesImported(): void {
    this.loadActivities(); // Reload activities after import
  }
}
