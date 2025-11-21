/**
 * Activity model matching the backend Activity entity
 */
export interface Activity {
  id?: number;
  stravaId?: string;
  type: string;
  distanceMeters: number;
  movingTimeSeconds: number;
  averageHeartRate?: number;
  totalElevationGain: number;
  startDate: string | Date;
}
