/**
 * Activity model matching the backend Activity entity
 */
export interface Activity {
  id?: number;
  stravaId?: string;
  name?: string;
  type: string;
  distanceMeters: number;
  movingTimeSeconds: number;
  averageHeartRate?: number;
  totalElevationGain: number;
  startDate: string | Date;
  summaryPolyline?: string;
  startLatitude?: number;
  startLongitude?: number;
  endLatitude?: number;
  endLongitude?: number;
}

/**
 * Activity with decoded route coordinates for map display
 */
export interface ActivityWithRoute extends Activity {
  route?: [number, number][]; // Array of [lat, lng] coordinates
}
