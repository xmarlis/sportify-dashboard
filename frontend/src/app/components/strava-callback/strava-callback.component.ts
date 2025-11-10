import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { StravaService } from '../../services/strava.service';

/**
 * Component to handle Strava OAuth callback
 */
@Component({
  selector: 'app-strava-callback',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './strava-callback.component.html',
  styleUrls: ['./strava-callback.component.css']
})
export class StravaCallbackComponent implements OnInit {
  isProcessing = true;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private stravaService: StravaService
  ) {}

  ngOnInit(): void {
    // Get authorization code from URL parameters
    this.route.queryParams.subscribe(params => {
      const code = params['code'];
      const error = params['error'];

      if (error) {
        this.error = 'Authorization denied or failed.';
        this.isProcessing = false;
        setTimeout(() => this.router.navigate(['/']), 3000);
        return;
      }

      if (!code) {
        this.error = 'No authorization code received.';
        this.isProcessing = false;
        setTimeout(() => this.router.navigate(['/']), 3000);
        return;
      }

      // Exchange code for access token
      this.stravaService.exchangeToken(code).subscribe({
        next: (token) => {
          console.log('Successfully connected to Strava', token);
          this.isProcessing = false;

          // Redirect to home page after successful connection
          setTimeout(() => this.router.navigate(['/']), 1500);
        },
        error: (err) => {
          console.error('Error exchanging Strava token:', err);
          this.error = 'Failed to connect to Strava. Please try again.';
          this.isProcessing = false;
          setTimeout(() => this.router.navigate(['/']), 3000);
        }
      });
    });
  }
}
