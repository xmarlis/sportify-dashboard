import { Routes } from '@angular/router';
import { StravaCallbackComponent } from './components/strava-callback/strava-callback.component';

export const routes: Routes = [
  {
    path: 'strava/callback',
    component: StravaCallbackComponent
  }
];
