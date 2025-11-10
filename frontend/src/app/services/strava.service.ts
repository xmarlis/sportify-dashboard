import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { tap } from 'rxjs/operators';

/**
 * Service for Strava API integration
 */
@Injectable({
  providedIn: 'root'
})
export class StravaService {
  private apiUrl = 'http://localhost:5023/api/strava';
  private tokenSubject = new BehaviorSubject<StravaToken | null>(this.getStoredToken());
  public token$ = this.tokenSubject.asObservable();

  constructor(private http: HttpClient) {}

  /**
   * Get Strava authorization URL
   */
  getAuthorizationUrl(): Observable<{ authorizationUrl: string }> {
    return this.http.get<{ authorizationUrl: string }>(`${this.apiUrl}/auth-url`);
  }

  /**
   * Exchange authorization code for access token
   */
  exchangeToken(code: string): Observable<StravaToken> {
    return this.http.post<StravaToken>(`${this.apiUrl}/exchange-token`, { code }).pipe(
      tap(token => this.storeToken(token))
    );
  }

  /**
   * Import ALL activities from Strava (with automatic pagination)
   */
  importActivities(accessToken?: string): Observable<ImportResult> {
    const token = accessToken || this.getAccessToken();
    if (!token) {
      return throwError(() => new Error('No access token available'));
    }

    return this.http.post<ImportResult>(`${this.apiUrl}/import-activities`, {
      accessToken: token
    });
  }

  /**
   * Get athlete profile
   */
  getAthlete(accessToken?: string): Observable<any> {
    const token = accessToken || this.getAccessToken();
    if (!token) {
      return throwError(() => new Error('No access token available'));
    }

    return this.http.post<any>(`${this.apiUrl}/athlete`, {
      accessToken: token
    });
  }

  /**
   * Store token in local storage
   */
  private storeToken(token: StravaToken): void {
    // Normalize token property names (handle both camelCase and PascalCase)
    const anyToken = token as any;
    if (!anyToken.accessToken && anyToken.AccessToken) {
      anyToken.accessToken = anyToken.AccessToken;
    }

    localStorage.setItem('strava_token', JSON.stringify(anyToken));
    this.tokenSubject.next(anyToken as StravaToken);
  }

  /**
   * Get stored token from local storage
   */
  private getStoredToken(): StravaToken | null {
    const tokenStr = localStorage.getItem('strava_token');
    if (!tokenStr) return null;

    try {
      const parsed = JSON.parse(tokenStr) as any;
      // Normalize property names
      if (!parsed.accessToken && parsed.AccessToken) {
        parsed.accessToken = parsed.AccessToken;
        // Persist normalized version
        localStorage.setItem('strava_token', JSON.stringify(parsed));
      }
      return parsed as StravaToken;
    } catch {
      return null;
    }
  }

  /**
   * Get access token
   */
  getAccessToken(): string | null {
    const token = this.tokenSubject.value as any;
    if (!token) return null;

    // Handle multiple possible property name variants
    return (token.accessToken || token.AccessToken) ?? null;
  }

  /**
   * Check if user is connected to Strava
   */
  isConnected(): boolean {
    return this.getAccessToken() !== null;
  }

  /**
   * Disconnect from Strava
   */
  disconnect(): void {
    localStorage.removeItem('strava_token');
    this.tokenSubject.next(null);
  }

  /**
   * Initiate Strava OAuth flow
   */
  connectToStrava(): void {
    this.getAuthorizationUrl().subscribe({
      next: (response) => {
        window.location.href = response.authorizationUrl;
      },
      error: (error) => {
        console.error('Error getting Strava authorization URL:', error);
      }
    });
  }
}

export interface StravaToken {
  tokenType?: string;
  expiresAt: number;
  expiresIn: number;
  refreshToken?: string;
  accessToken?: string;
  athlete?: {
    id: number;
    username?: string;
    firstname?: string;
    lastname?: string;
  };
}

export interface ImportResult {
  imported: number;
  skipped: number;
  total: number;
  message: string;
}
