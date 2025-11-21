import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { StravaService } from '../../services/strava.service';

/**
 * Error response structure from backend
 */
interface StravaErrorResponse {
  error: boolean;
  errorCode: string;
  message: string;
  detailedMessage?: string;
}

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
  errorCode = '';
  detailedError = '';
  isAthleteLimitError = false;

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
        this.error = 'Autorisierung wurde verweigert oder ist fehlgeschlagen.';
        this.isProcessing = false;
        setTimeout(() => this.router.navigate(['/']), 3000);
        return;
      }

      if (!code) {
        this.error = 'Kein Autorisierungscode erhalten.';
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
          this.isProcessing = false;

          // Parse error response
          const errorBody = err.error as StravaErrorResponse;

          if (errorBody?.errorCode) {
            this.errorCode = errorBody.errorCode;
            this.error = errorBody.message;
            this.detailedError = errorBody.detailedMessage || '';

            // Check for athlete limit error
            if (errorBody.errorCode === 'ATHLETE_LIMIT_EXCEEDED') {
              this.isAthleteLimitError = true;
              // Don't auto-redirect for this error - let user read the message
              return;
            }
          } else {
            this.error = 'Verbindung mit Strava fehlgeschlagen. Bitte versuchen Sie es erneut.';
          }

          setTimeout(() => this.router.navigate(['/']), 5000);
        }
      });
    });
  }

  goHome(): void {
    this.router.navigate(['/']);
  }
}
