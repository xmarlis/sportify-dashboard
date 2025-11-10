# Running Dashboard

A full-stack web application for tracking and visualizing running and cycling activities.

## Tech Stack

### Backend
- **.NET 9 Web API** (C#)
- **Entity Framework Core** with **SQLite**
- **Swagger** for API documentation and testing

### Frontend
- **Angular** (latest) with TypeScript
- **Bootstrap 5** for responsive UI styling
- **RxJS** for reactive programming

### Database
- **SQLite** for local development

## Features

- ✅ View all running/cycling activities in a responsive table
- ✅ Create new activities with detailed metrics:
  - Activity type (Run, Ride, Walk, Hike)
  - Distance (in kilometers)
  - Moving time (hours, minutes, seconds)
  - Elevation gain
  - Average heart rate (optional)
  - Start date and time
- ✅ Automatic pace calculation (min/km)
- ✅ Delete activities
- ✅ RESTful API with full CRUD operations
- 🔄 Future: Strava API integration

## Project Structure

```
running-dashboard/
├── backend/                    # .NET 9 Web API
│   ├── Controllers/
│   │   └── ActivitiesController.cs
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Models/
│   │   └── Activity.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── RunningDashboard.csproj
├── frontend/                   # Angular Application
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   │   ├── activity-form/
│   │   │   │   └── activity-table/
│   │   │   ├── models/
│   │   │   │   └── activity.model.ts
│   │   │   ├── services/
│   │   │   │   └── activity.service.ts
│   │   │   ├── app.ts
│   │   │   ├── app.html
│   │   │   └── app.config.ts
│   │   └── index.html
│   ├── angular.json
│   └── package.json
└── README.md
```

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (v18 or later)
- [Angular CLI](https://angular.io/cli) (optional, but recommended)

### Running the Backend

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the API:
   ```bash
   dotnet run
   ```

   The API will start on `https://localhost:5023` (or `http://localhost:5023`)

4. Access Swagger UI for API documentation:
   ```
   http://localhost:5023/swagger
   ```

### Running the Frontend

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   ng serve
   ```
   Or if you don't have Angular CLI globally installed:
   ```bash
   npm start
   ```

4. Open your browser and navigate to:
   ```
   http://localhost:4200
   ```

## API Endpoints

### Base URL
```
http://localhost:5023/api/activities
```

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/activities` | Get all activities |
| GET | `/api/activities/{id}` | Get a specific activity by ID |
| POST | `/api/activities` | Create a new activity |
| PUT | `/api/activities/{id}` | Update an existing activity |
| DELETE | `/api/activities/{id}` | Delete an activity |

### Example Activity JSON

```json
{
  "type": "Run",
  "distanceMeters": 5000,
  "movingTimeSeconds": 1500,
  "totalElevationGain": 50,
  "startDate": "2025-11-10T08:30:00",
  "averageHeartRate": 150
}
```

## Database

The application uses SQLite for local data storage. The database file (`running-dashboard.db`) is automatically created in the `backend/` directory when you first run the API.

### Activity Model

```csharp
public class Activity
{
    public int Id { get; set; }
    public string? StravaId { get; set; }        // Optional, for future Strava integration
    public string Type { get; set; }             // "Run", "Ride", etc.
    public double DistanceMeters { get; set; }
    public int MovingTimeSeconds { get; set; }
    public double? AverageHeartRate { get; set; }
    public double TotalElevationGain { get; set; }
    public DateTime StartDate { get; set; }
}
```

## Development

### Backend Configuration

- **Database connection**: Configured in `appsettings.json`
- **CORS**: Configured to allow requests from `http://localhost:4200`
- **Logging**: Configured for development with detailed Entity Framework logs

### Frontend Configuration

- **API URL**: Configured in `ActivityService` as `http://localhost:5023/api/activities`
- **Bootstrap**: Configured in `angular.json` styles array
- **HttpClient**: Configured as a provider in `app.config.ts`

## Future Enhancements

- 🔄 Strava API integration for automatic activity import
- 📊 Advanced statistics and charts (weekly/monthly summaries)
- 🗺️ Map visualization of routes
- 🏃 Pace zones and heart rate zones analysis
- 📱 Mobile-responsive enhancements
- 👤 User authentication and multi-user support
- 🎯 Goal tracking and progress monitoring

## Contributing

1. Create a feature branch from `main`
2. Make your changes with clear, conventional commit messages
3. Test thoroughly
4. Submit a pull request

### Commit Message Convention

```
feat(component): add new feature
fix(component): fix bug
chore: update dependencies
docs: update README
style(component): improve styling
```

## License

This project is open source and available under the MIT License.

## Authors

Built with ❤️ using Angular and .NET 9

---

**Note**: This is a local development application. For production deployment, additional security, authentication, and deployment configurations would be required.
