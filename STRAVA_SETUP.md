# Strava API Setup Guide

## 1. Create a Strava API Application

1. Visit https://www.strava.com/settings/api
2. Log in with your Strava account
3. If you don't have an app yet, you'll see "Create an App" button
4. Fill in the application form:
   - **Application Name**: `Sportify Dashboard` (or any name)
   - **Category**: Choose appropriate (e.g., "Training")
   - **Club**: Leave empty
   - **Website**: `http://localhost:4200`
   - **Authorization Callback Domain**: `localhost`
   - **Application Description**: "Personal running dashboard"
5. Click "Create" button
6. You'll be redirected to your application page where you'll see:
   - **Client ID**: A number (e.g., `123456`)
   - **Client Secret**: A long alphanumeric string
   - **Refresh Token**: (not needed initially)
   - **Access Token**: (not needed initially)

## 2. Configure Your Application

### Option A: Update appsettings.Development.json (Recommended for local development)

Open `backend/appsettings.Development.json` and replace the placeholders:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "Strava": {
    "ClientId": "123456",  // Replace with your actual Client ID
    "ClientSecret": "your_actual_client_secret_here"  // Replace with your actual Client Secret
  }
}
```

**Note**: Never commit `appsettings.Development.json` with real credentials to Git!

### Option B: Use User Secrets (More secure)

Run these commands in your backend directory:

```bash
cd backend
dotnet user-secrets init
dotnet user-secrets set "Strava:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Strava:ClientSecret" "YOUR_CLIENT_SECRET"
```

## 3. Restart Your Backend

After updating the configuration:

1. Stop your backend if it's running (Ctrl+C in the terminal)
2. Start it again:
   ```bash
   cd backend
   dotnet run
   ```

## 4. Test the Connection

1. Open your app at http://localhost:4200
2. Click "Connect to Strava"
3. You should be redirected to Strava's authorization page
4. Click "Authorize" to grant access
5. You'll be redirected back to your app with activities ready to import

## Troubleshooting

### "Bad Request" Error
- Check that your Client ID and Client Secret are correct
- Verify the callback domain is set to `localhost` in Strava settings

### "Unauthorized" Error (401)
- Your access token has expired
- Click "Disconnect" and "Connect to Strava" again to get a fresh token

### "Cannot connect to Strava"
- Make sure your backend is running on port 5023
- Check that the Strava API is accessible (not blocked by firewall)
- Verify your internet connection

## Important Notes

- **Never share your Client Secret** publicly
- Add `appsettings.Development.json` to `.gitignore` if it contains real credentials
- Strava access tokens expire after 6 hours - the app will automatically refresh them
- For production deployment, use environment variables or a secure secret management system
