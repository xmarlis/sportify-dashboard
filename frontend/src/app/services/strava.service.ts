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
   * Refresh access token using refresh token
   */
  refreshToken(): Observable<StravaToken> {
    const token = this.tokenSubject.value;
    if (!token || !token.refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<StravaToken>(`${this.apiUrl}/refresh-token`, { 
      refreshToken: token.refreshToken 
    }).pipe(
      tap(newToken => this.storeToken(newToken))
    );
  }

  /**
   * Check if token is expired or about to expire (within 5 minutes)
   */
  isTokenExpired(): boolean {
    const token = this.tokenSubject.value;
    if (!token || !token.expiresAt) return true;

    // Check if token expires within the next 5 minutes
    const expirationTime = token.expiresAt * 1000; // Convert to milliseconds
    const now = Date.now();
    const fiveMinutes = 5 * 60 * 1000;

    return (expirationTime - now) < fiveMinutes;
  }

  /**
   * Get a valid access token, refreshing if necessary
   */
  async getValidAccessToken(): Promise<string | null> {
    if (this.isTokenExpired()) {
      try {
        console.log('Token expired, refreshing...');
        const newToken = await this.refreshToken().toPromise();
        return newToken?.accessToken || null;
      } catch (error) {
        console.error('Failed to refresh token:', error);
        // If refresh fails, disconnect user
        this.disconnect();
        return null;
      }
    }

    return this.getAccessToken();
  }

  /**
   * Import activities from Strava
   */
  importActivities(accessToken?: string): Observable<ImportResult> {
    const token = accessToken || this.getAccessToken();
    if (!token) {
      return throwError(() => new Error('No access token available'));
    }

    return this.http.post<ImportResult>(`${this.apiUrl}/import-activities`, {
      accessToken: token,
      page: 1,
      perPage: 50
    });
  }

  /**
   * Sync new activities from Strava (only activities newer than the most recent one in DB)
   */
  syncNewActivities(accessToken?: string): Observable<ImportResult> {
    const token = accessToken || this.getAccessToken();
    if (!token) {
      return throwError(() => new Error('No access token available'));
    }

    return this.http.post<ImportResult>(`${this.apiUrl}/sync-new-activities`, {
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
    // Normalize token property names before storing
    const anyToken = token as any;
    if (!anyToken.accessToken && anyToken.access_token) {
      anyToken.accessToken = anyToken.access_token;
    }
    if (!anyToken.refreshToken && anyToken.refresh_token) {
      anyToken.refreshToken = anyToken.refresh_token;
    }
    if (!anyToken.expiresAt && anyToken.expires_at) {
      anyToken.expiresAt = anyToken.expires_at;
    }

    console.log('Storing token, expires at:', new Date(anyToken.expiresAt * 1000).toLocaleString());

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
      // Normalize common variants
      if (!parsed.accessToken && parsed.access_token) {
        parsed.accessToken = parsed.access_token;
        // Persist normalized shape back to localStorage so raw code sees it too
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

    // Accept multiple possible property names returned by different serializers/APIs
    return (token.accessToken || token.access_token || token.AccessToken) ?? null;
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
