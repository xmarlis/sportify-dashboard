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
  isSyncing = false;
  importMessage = '';
  syncMessage = '';
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
  async importActivities(): Promise<void> {
    this.isImporting = true;
    this.importMessage = '';

    try {
      // Get a valid access token (will refresh if expired)
      const accessToken = await this.stravaService.getValidAccessToken();
      
      if (!accessToken) {
        this.importMessage = 'Authentication expired. Please reconnect to Strava.';
        this.isImporting = false;
        this.stravaService.disconnect();
        return;
      }

      this.stravaService.importActivities(accessToken).subscribe({
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
    } catch (error) {
      console.error('Error getting valid token:', error);
      this.importMessage = 'Authentication error. Please reconnect to Strava.';
      this.isImporting = false;
    }
  }

  /**
   * Sync new activities from Strava
   */
  async syncNewActivities(): Promise<void> {
    this.isSyncing = true;
    this.syncMessage = '';

    try {
      // Get a valid access token (will refresh if expired)
      const accessToken = await this.stravaService.getValidAccessToken();
      
      if (!accessToken) {
        this.syncMessage = 'Authentication expired. Please reconnect to Strava.';
        this.isSyncing = false;
        this.stravaService.disconnect();
        return;
      }

      this.stravaService.syncNewActivities(accessToken).subscribe({
        next: (result) => {
          console.log('Sync result:', result);
          
          if (result.imported === 0 && result.skipped === 0) {
            this.syncMessage = result.message || 'No new activities found';
          } else if (result.imported === 0 && result.skipped > 0) {
            this.syncMessage = `Found ${result.skipped} activities, but all were already imported`;
          } else {
            this.syncMessage = result.message || `Synced ${result.imported} new activities`;
          }
          
          this.isSyncing = false;

          if (result.imported > 0) {
            // Notify parent component to reload activities
            this.activitiesImported.emit();
          }

          // Clear message after 5 seconds
          setTimeout(() => {
            this.syncMessage = '';
          }, 5000);
        },
        error: (error) => {
          console.error('Error syncing activities:', error);
          this.syncMessage = 'Failed to sync activities. Please try again.';
          this.isSyncing = false;

          setTimeout(() => {
            this.syncMessage = '';
          }, 5000);
        }
      });
    } catch (error) {
      console.error('Error getting valid token:', error);
      this.syncMessage = 'Authentication error. Please reconnect to Strava.';
      this.isSyncing = false;
    }
  }
}
