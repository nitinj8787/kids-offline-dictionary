# Manual Testing Guide — Visual Studio (Windows)

This guide walks you through launching **KidsDictionaryApi** and **KidsDictionaryApp**
(Windows target) side-by-side from Visual Studio so you can exercise the
**cloud profile sync** feature end-to-end on your local machine.

---

## 1. Prerequisites

| Requirement | Version |
|-------------|---------|
| Visual Studio 2022 | 17.9+ (Community, Professional, or Enterprise) |
| .NET MAUI workload | Installed via VS Installer → "Mobile development with .NET" |
| .NET 9 SDK | Required by the API project (`net9.0`) |
| .NET 10 SDK | Required by the MAUI app (`net10.0-windows`) |

Verify SDKs are installed:

```powershell
dotnet --list-sdks
```

Both `9.x.x` and `10.x.x` lines should appear in the output.

---

## 2. Open the Solution

1. Open Visual Studio 2022.
2. Choose **Open a project or solution**.
3. Navigate to the repository root and open **`KidsDictionaryApp.slnx`**.

Visual Studio will load four projects:

| Project | Purpose |
|---------|---------|
| `KidsDictionaryApi` | REST API — OTP auth, profile sync (ASP.NET Core Minimal API) |
| `KidsDictionaryApp` | Cross-platform MAUI Blazor app (target: Windows) |
| `KidsDictionaryApi.Tests` | xUnit tests for the API |
| `KidsDictionaryApp.IntegrationTests` | xUnit tests for the MAUI SQLite layer |

---

## 3. Configure Multiple Startup Projects

To launch both the API and the app simultaneously:

1. Right-click the **Solution** node in Solution Explorer → **Set Startup Projects…**
2. Select **Multiple startup projects**.
3. Set:
   - `KidsDictionaryApi` → **Start**
   - `KidsDictionaryApp` → **Start**
   - The two test projects → **None**
4. Click **OK**.

---

## 4. Point the MAUI App at the Local API

Open **`KidsDictionaryApp/MauiProgram.cs`** and find this line (around line 178):

```csharp
var apiBaseUrl = ""; // e.g. "https://your-api.azurewebsites.net"
```

Change it to the local API URL:

```csharp
var apiBaseUrl = "http://localhost:5150";
```

> **Why port 5150?**  
> The API's `Properties/launchSettings.json` pins HTTP to port 5150 so the URL
> is always predictable during local development. If you prefer HTTPS, use
> `https://localhost:7150` instead (the self-signed dev certificate must be
> trusted first — run `dotnet dev-certs https --trust` once if needed).

> **Offline-only mode:** Set `apiBaseUrl` back to `""` at any time to disable
> sync. The app runs fully offline — no API connection required.

---

## 5. Select the Windows Target

In the Visual Studio toolbar, make sure the target for `KidsDictionaryApp` is
set to **Windows Machine** (not Android Emulator or iOS Simulator):

```
Solution Platforms: Any CPU  |  Target: Windows Machine
```

---

## 6. Run the Projects

Press **F5** (Start Debugging) or **Ctrl+F5** (Start Without Debugging).

Two things start:

1. **API console window** — look for output like:
   ```
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: http://localhost:5150
   ```
2. **KidsDictionaryApp** — the MAUI Blazor app opens in a Windows window.

---

## 7. Test the Profile Sync Flow

### 7.1 Create a profile with a parent email

1. In the app, navigate to **👤 Profiles** (sidebar or home menu).
2. In the **Create New Profile** form:
   - Enter an **Avatar name** (e.g., `Alice`).
   - Enter a **Parent email** (e.g., `test@example.com`).
     > Any valid email format works — no real email is sent in development.
   - Pick an avatar emoji.
3. Click **Create Profile**.

### 7.2 Sync the profile to the API

1. Click the **🔄** (sync) button on the newly created profile card.
2. The app calls `POST /api/auth/request-otp` and shows the **OTP verification panel**.

### 7.3 Find the OTP code

The development configuration has `"ReturnOtpInResponse": true`, so the OTP
is returned directly in the API response. Read it from the **API console window**:

```
info: KidsDictionaryApi.Endpoints.AuthEndpoints.RequestOtpDto[0]
      OTP requested for test@example.com. Code: 482031
```

Alternatively, use the Swagger UI (see Section 8) to call
`POST /api/auth/request-otp` yourself and read the `otp` field in the response
body.

### 7.4 Enter the OTP

1. Type the 6-digit code (e.g., `482031`) into the verification panel in the app.
2. Click **Verify**.

On success the panel closes and the app calls `SyncProfileAsync`, which:
- `POST /api/profiles` — creates a remote profile record.
- Updates the local `UserProfile` with `RemoteId` and `LastSyncedAt`.
- Shows **"☁️ Profile 'Alice' synced successfully!"**

The profile card now shows a **☁️ Synced** badge.

### 7.5 Sync again (update)

1. Navigate away and come back (or look up a word to increase `TotalScore`).
2. Click **🔄** again on the same profile.
   - Because the session JWT is still valid, no OTP prompt appears.
   - The app calls `PUT /api/profiles/{id}` to update the remote score.

---

## 8. Explore the API with Swagger UI

The API exposes an OpenAPI endpoint in the `Development` environment.

Open a browser and navigate to:

```
http://localhost:5150/openapi/v1.json
```

For a full interactive Swagger UI, install the
[Scalar](https://scalar.com/) VS extension or paste the URL into
[editor.swagger.io](https://editor.swagger.io).

### Quick API test with PowerShell

```powershell
# 1. Request an OTP (returns the code in dev mode)
$r = Invoke-RestMethod -Uri http://localhost:5150/api/auth/request-otp `
     -Method Post `
     -ContentType 'application/json' `
     -Body '{"email":"test@example.com"}'
$r   # otp field contains the 6-digit code

# 2. Verify OTP and get a JWT token
$v = Invoke-RestMethod -Uri http://localhost:5150/api/auth/verify-otp `
     -Method Post `
     -ContentType 'application/json' `
     -Body "{`"email`":`"test@example.com`",`"code`":`"$($r.otp)`"}"
$token = $v.token

# 3. List profiles (authenticated)
Invoke-RestMethod -Uri http://localhost:5150/api/profiles `
     -Headers @{ Authorization = "Bearer $token" }

# 4. Health check (no auth required)
Invoke-RestMethod -Uri http://localhost:5150/health
```

---

## 9. Verify the SQLite Database

The API creates **`kidsdictionary_api_dev.db`** in the project's working
directory (`KidsDictionaryApi/bin/Debug/net9.0/`) when running under the
`Development` profile.

You can inspect it with any SQLite viewer (e.g.,
[DB Browser for SQLite](https://sqlitebrowser.org/)):

| Table | Contents |
|-------|----------|
| `UserAccount` | One row per parent email |
| `OtpRecord` | OTP history (`IsUsed = 1` after verification) |
| `CentralProfile` | Synced profiles with `AvatarName`, `TotalScore`, `LastSyncedAt` |

The MAUI app database lives in the Windows app-data folder:
```
%LOCALAPPDATA%\Packages\com.companyname.kidsdictionaryapp_...\LocalState\dictionary.db
```
Open it the same way to confirm `RemoteId` and `LastSyncedAt` were written back
after a successful sync.

---

## 10. Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| "Could not reach the server" | `apiBaseUrl` is empty or wrong | Set `apiBaseUrl = "http://localhost:5150"` in `MauiProgram.cs` |
| "Could not reach the server" | API not running | Start the API project (F5 on `KidsDictionaryApi`) |
| OTP panel appears but Verify fails | Wrong code entered | Read the 6-digit code from the API console log |
| HTTPS certificate error | Dev cert not trusted | Run `dotnet dev-certs https --trust` in a terminal |
| Port 5150 already in use | Another process | Change `applicationUrl` in `KidsDictionaryApi/Properties/launchSettings.json` and update `apiBaseUrl` to match |
| Windows app doesn't build | Missing MAUI workload | Open VS Installer → Modify → add "Mobile development with .NET" |
