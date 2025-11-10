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

- ✅ **Strava Integration** - Connect your Strava account and automatically import activities
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
- ✅ OAuth 2.0 authentication with Strava
- ✅ Duplicate detection when importing from Strava

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

## Strava Integration Setup

To enable Strava integration and automatically import your activities:

### 1. Create a Strava API Application

1. Go to [Strava API Settings](https://www.strava.com/settings/api)
2. Click "Create App" (or use an existing app)
3. Fill in the application details:
   - **Application Name**: Running Dashboard (or your choice)
   - **Category**: Your choice
   - **Website**: `http://localhost:4200`
   - **Authorization Callback Domain**: `localhost`
4. After creating, note your **Client ID** and **Client Secret**

### 2. Configure Backend

Open `backend/appsettings.json` and update the Strava configuration:

```json
"Strava": {
  "ClientId": "YOUR_STRAVA_CLIENT_ID",
  "ClientSecret": "YOUR_STRAVA_CLIENT_SECRET",
  "RedirectUri": "http://localhost:4200/strava/callback",
  "AuthorizationEndpoint": "https://www.strava.com/oauth/authorize",
  "TokenEndpoint": "https://www.strava.com/oauth/token",
  "ApiBaseUrl": "https://www.strava.com/api/v3"
}
```

Replace `YOUR_STRAVA_CLIENT_ID` and `YOUR_STRAVA_CLIENT_SECRET` with your actual values from Step 1.

### 3. Using Strava Integration

1. Start both backend and frontend applications
2. In the Running Dashboard UI, click **"Connect to Strava"** button
3. You'll be redirected to Strava to authorize the application
4. After authorization, you'll be redirected back to the dashboard
5. Click **"Import Activities"** to sync your recent Strava activities
6. Activities are automatically imported with duplicate detection

**Note**: The free Strava API has rate limits. Be mindful when importing large numbers of activities.

## API Endpoints

### Base URL
```
http://localhost:5023/api/activities
```

### Activity Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/activities` | Get all activities |
| GET | `/api/activities/{id}` | Get a specific activity by ID |
| POST | `/api/activities` | Create a new activity |
| PUT | `/api/activities/{id}` | Update an existing activity |
| DELETE | `/api/activities/{id}` | Delete an activity |

### Strava Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/strava/auth-url` | Get Strava OAuth authorization URL |
| POST | `/api/strava/exchange-token` | Exchange authorization code for access token |
| POST | `/api/strava/import-activities` | Import activities from Strava |
| POST | `/api/strava/athlete` | Get athlete profile from Strava |

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
    public string? StravaId { get; set; }        // Strava activity ID (for imported activities)
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
- **Strava API**: Client ID and secret configured in `appsettings.json`

### Frontend Configuration

- **API URL**: Configured in `ActivityService` as `http://localhost:5023/api/activities`
- **Bootstrap**: Configured in `angular.json` styles array
- **HttpClient**: Configured as a provider in `app.config.ts`
- **Strava Token**: Stored in browser localStorage for persistent connection

## Future Enhancements
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
