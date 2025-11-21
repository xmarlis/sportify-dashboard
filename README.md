# Sportify Dashboard

A full-stack web application for tracking and visualizing your sports activities with Strava integration.

![Angular](https://img.shields.io/badge/Angular-20-red?logo=angular)
![.NET](https://img.shields.io/badge/.NET-9-purple?logo=dotnet)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5-blue?logo=bootstrap)
![Leaflet](https://img.shields.io/badge/Leaflet-1.9-green?logo=leaflet)

## Features

### Strava Integration
- Connect your Strava account with OAuth 2.0
- Automatically import all your activities
- Duplicate detection prevents re-importing existing activities
- Syncs activity details including GPS routes

### Interactive Activity Map
- View all your activity routes on an interactive map
- Color-coded routes by activity type (Run, Ride, Walk, Hike, etc.)
- Filter routes by activity type
- Click routes to see activity details (distance, time, date)
- **Fullscreen mode** - click the map to expand for better viewing

### Statistics Dashboard
- Total distance, time, and elevation stats
- Activity breakdown by type
- Monthly activity trends
- Average pace calculations

### Activity Management
- View all activities in a sortable table
- Create new activities manually
- Delete activities
- Detailed metrics: distance, time, pace, elevation, heart rate

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | Angular 20, TypeScript, Bootstrap 5 |
| Maps | Leaflet with OpenStreetMap |
| Backend | .NET 9 Web API (C#) |
| Database | SQLite with Entity Framework Core |
| Auth | OAuth 2.0 (Strava) |

## Screenshots

### Dashboard View
- Statistics cards showing total distance, time, and activities
- Interactive map with all your routes
- Activity table with full details

### Fullscreen Map
- Click the map to open fullscreen view
- Filter activities by type
- Press Escape or click X to close

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (v18 or later)
- [Angular CLI](https://angular.io/cli) (optional)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/xmarlis/sportify-dashboard.git
   cd sportify-dashboard
   ```

2. **Start the Backend**
   ```bash
   cd backend
   dotnet restore
   dotnet run
   ```
   API runs on `http://localhost:5023`

3. **Start the Frontend**
   ```bash
   cd frontend
   npm install
   npm start
   ```
   App runs on `http://localhost:4200`

4. **Open in browser**
   ```
   http://localhost:4200
   ```

## Strava Setup

To import activities from Strava:

1. **Create a Strava API Application**
   - Go to [Strava API Settings](https://www.strava.com/settings/api)
   - Create a new application
   - Set callback domain to `localhost`

2. **Configure the Backend**

   Edit `backend/appsettings.json`:
   ```json
   "Strava": {
     "ClientId": "YOUR_CLIENT_ID",
     "ClientSecret": "YOUR_CLIENT_SECRET",
     "RedirectUri": "http://localhost:4200/strava/callback"
   }
   ```

3. **Connect in the App**
   - Click "Connect to Strava" in the dashboard
   - Authorize the application
   - Click "Import Activities" to sync

See [STRAVA_SETUP.md](STRAVA_SETUP.md) for detailed instructions.

## Project Structure

```
sportify-dashboard/
├── backend/                    # .NET 9 Web API
│   ├── Controllers/            # API endpoints
│   ├── Models/                 # Data models
│   ├── Services/               # Strava service
│   └── Data/                   # Database context
├── frontend/                   # Angular Application
│   └── src/app/
│       ├── components/
│       │   ├── activity-map/       # Interactive map with routes
│       │   ├── activity-table/     # Activity list table
│       │   ├── activity-form/      # New activity form
│       │   ├── statistics-dashboard/  # Stats cards
│       │   └── strava-connect/     # Strava OAuth
│       ├── models/             # TypeScript interfaces
│       └── services/           # API & Strava services
└── README.md
```

## API Endpoints

### Activities
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/activities` | Get all activities |
| GET | `/api/activities/{id}` | Get activity by ID |
| POST | `/api/activities` | Create activity |
| DELETE | `/api/activities/{id}` | Delete activity |

### Strava
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/strava/auth-url` | Get OAuth URL |
| POST | `/api/strava/exchange-token` | Exchange auth code |
| POST | `/api/strava/import-activities` | Import from Strava |

## Activity Types

The dashboard supports these activity types with color-coded routes:

| Type | Color |
|------|-------|
| Run | Orange |
| Ride | Blue |
| Walk | Green |
| Hike | Light Green |
| Swim | Cyan |
| Virtual Ride | Purple |
| Virtual Run | Pink |

## Development

### Backend
```bash
cd backend
dotnet watch run  # Hot reload enabled
```

### Frontend
```bash
cd frontend
ng serve          # Hot reload enabled
```

### API Documentation
Swagger UI available at `http://localhost:5023/swagger`

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is open source under the MIT License.

---

Built with Angular and .NET 9
