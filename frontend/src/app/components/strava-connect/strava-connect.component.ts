import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StravaService, StravaToken } from '../../services/strava.service';

/**
 * Component for Strava connection and activity import
 */
@Component({
  selector: 'app-strava-connect',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './strava-connect.component.html',
  styleUrls: ['./strava-connect.component.css']
})
export class StravaConnectComponent implements OnInit {
  @Output() activitiesImported = new EventEmitter<void>();

  isConnected = false;
  isImporting = false;
  importMessage = '';
  athleteName = '';
  token: StravaToken | null = null;

  constructor(private stravaService: StravaService) {}

  ngOnInit(): void {
    // Check if user is connected
    this.isConnected = this.stravaService.isConnected();

    // Subscribe to token changes
    this.stravaService.token$.subscribe(token => {
      this.token = token;
      this.isConnected = token !== null;
      if (token?.athlete) {
        this.athleteName = `${token.athlete.firstname} ${token.athlete.lastname}`.trim();
      }
    });
  }

  /**
   * Connect to Strava
   */
  connectToStrava(): void {
    this.stravaService.connectToStrava();
  }

  /**
   * Disconnect from Strava
   */
  disconnect(): void {
    if (confirm('Are you sure you want to disconnect from Strava?')) {
      this.stravaService.disconnect();
      this.athleteName = '';
      this.importMessage = '';
    }
  }

  /**
   * Import activities from Strava
   */
  importActivities(): void {
    this.isImporting = true;
    this.importMessage = '';

    this.stravaService.importActivities().subscribe({
      next: (result) => {
        this.importMessage = result.message;
        this.isImporting = false;

        if (result.imported > 0) {
          // Notify parent component to reload activities
          this.activitiesImported.emit();
        }

        // Clear message after 5 seconds
        setTimeout(() => {
          this.importMessage = '';
        }, 5000);
      },
      error: (error) => {
        console.error('Error importing activities:', error);
        this.importMessage = 'Failed to import activities. Please try again.';
        this.isImporting = false;

        setTimeout(() => {
          this.importMessage = '';
        }, 5000);
      }
    });
  }
}
