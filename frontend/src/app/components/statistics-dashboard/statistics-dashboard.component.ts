import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivityService } from '../../services/activity.service';

@Component({
  selector: 'app-statistics-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './statistics-dashboard.component.html',
  styleUrls: ['./statistics-dashboard.component.css']
})
export class StatisticsDashboardComponent implements OnInit {
  statistics: any = null;
  loading = true;
  error = false;
  Object = Object; // Make Object available in template

  constructor(private activityService: ActivityService) {}

  ngOnInit(): void {
    this.loadStatistics();
  }

  loadStatistics(): void {
    this.loading = true;
    this.error = false;

    this.activityService.getStatistics().subscribe({
      next: (data) => {
        this.statistics = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading statistics:', err);
        this.error = true;
        this.loading = false;
      }
    });
  }

  formatTime(seconds: number): string {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    return `${hours}h ${minutes}m`;
  }

  formatPace(secondsPerKm: number): string {
    const minutes = Math.floor(secondsPerKm / 60);
    const seconds = Math.floor(secondsPerKm % 60);
    return `${minutes}:${seconds.toString().padStart(2, '0')} /km`;
  }

  getActivityTypeIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'Run': '🏃',
      'Ride': '🚴',
      'Walk': '🚶',
      'Hike': '⛰️',
      'Swim': '🏊'
    };
    return icons[type] || '🏃';
  }

  getMonthName(monthStr: string): string {
    const [year, month] = monthStr.split('-');
    const date = new Date(parseInt(year), parseInt(month) - 1);
    return date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
  }
}
