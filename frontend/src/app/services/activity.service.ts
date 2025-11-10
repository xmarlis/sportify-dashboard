import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Activity } from '../models/activity.model';

/**
 * Service for managing activities via the backend API
 */
@Injectable({
  providedIn: 'root'
})
export class ActivityService {
  private apiUrl = 'http://localhost:5023/api/activities';

  constructor(private http: HttpClient) {}

  /**
   * Get all activities
   */
  getActivities(): Observable<Activity[]> {
    return this.http.get<Activity[]>(this.apiUrl);
  }

  /**
   * Get a single activity by ID
   */
  getActivity(id: number): Observable<Activity> {
    return this.http.get<Activity>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create a new activity
   */
  createActivity(activity: Activity): Observable<Activity> {
    return this.http.post<Activity>(this.apiUrl, activity);
  }

  /**
   * Update an existing activity
   */
  updateActivity(id: number, activity: Activity): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, activity);
  }

  /**
   * Delete an activity
   */
  deleteActivity(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get comprehensive statistics for all activities
   */
  getStatistics(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/statistics`);
  }
}
