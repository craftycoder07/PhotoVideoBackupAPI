# Mobile Media Backup API

A .NET 8 Web API that acts like Google Photos for local backup. Mobile devices can automatically backup their photos and videos to your local server over WiFi during the night, providing a private, self-hosted alternative to cloud photo services.

## Features

- **User Authentication System**: Secure JWT-based authentication with user registration and login
- **Multi-Device Support**: Users can register multiple devices under a single account
- **Unified Media Library**: All media from multiple devices appears in one user account
- **Mobile Device Management**: Register and manage multiple mobile devices per user
- **Automatic Night Backup**: Configure backup windows (e.g., 10 PM - 6 AM)
- **Session-based Uploads**: Secure backup sessions with progress tracking
- **Media Organization**: Automatic categorization of photos and videos
- **Thumbnail Generation**: Fast preview generation for media items
- **Search & Filtering**: Find media by date, tags, or text search
- **Statistics & Analytics**: Track backup progress and storage usage
- **Modular API Design**: Clean, RESTful endpoints organized by domain
- **File Integrity**: SHA256 hash verification for uploaded files
- **Flexible Settings**: Per-device backup preferences and restrictions

## Supported File Types

### Photos
- JPG, JPEG, PNG, GIF, BMP, TIFF, WebP, HEIC, HEIF

### Videos
- MP4, AVI, MOV, WMV, FLV, WebM, MKV, M4V, 3GP, MPG, MPEG

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- macOS, Windows, or Linux
- Mobile device with WiFi capability

### Installation

1. Clone or download the project
2. Navigate to the project directory:
   ```bash
   cd PhotoVideoBackupAPI
   ```

3. Restore dependencies:
   ```bash
   dotnet restore
   ```

4. Run the application:
   ```bash
   dotnet run --launch-profile https
   ```

5. Open your browser and navigate to `https://localhost:7109` to access the Swagger UI.

## Configuration

This application uses **environment variables** for all configuration. Environment variables automatically override values in `appsettings.json`.

### Quick Setup

Set the required environment variables:

```bash
# Database connection
export ConnectionStrings__DefaultConnection="Host=localhost;Database=PhotoVideoBackup;Username=your_user;Password=your_password;Port=5432"

# JWT Secret (required in production)
export Jwt__Secret="your-super-secret-key-at-least-32-characters-long"

# Storage path
export StorageSettings__BasePath="/path/to/media/backups"
```

### Environment Variable Format

For nested configuration properties, use double underscore (`__`):

- `ConnectionStrings__DefaultConnection` → `ConnectionStrings:DefaultConnection`
- `Jwt__Secret` → `Jwt:Secret`
- `StorageSettings__BasePath` → `StorageSettings:BasePath`

### Required Variables

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `Jwt__Secret` - JWT signing key (required in production)
- `StorageSettings__BasePath` - Path for media storage

### Documentation

See [ENVIRONMENT_VARIABLES.md](./ENVIRONMENT_VARIABLES.md) for complete documentation of all environment variables.

**Note**: The `appsettings.json` file is tracked in git with safe defaults. Always use environment variables for sensitive data like passwords and secrets.

## API Architecture

The API follows a modular design with separate controllers for each domain:

- **AuthController** (`/api/auth/*`): Authentication and token management
- **DeviceController** (`/api/device/*`): Device registration and management
- **SessionController** (`/api/session/*`): Backup session management
- **MediaController** (`/api/media/*`): Media upload and retrieval
- **StatsController** (`/api/stats/*`): Statistics and analytics

This modular approach provides:
- **Better Organization**: Each controller handles a specific domain
- **Easier Maintenance**: Smaller, focused controllers are easier to test and modify
- **RESTful Design**: Clean, intuitive URL structure
- **Future Versioning**: Easy to add version prefixes (e.g., `/v2/`) in the future

## API Endpoints

The API is organized into modular controllers for better maintainability and RESTful design:

### Authentication (`/api/auth/*`)

#### User Registration
**POST** `/api/auth/register`

Register a new user account.

```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePassword123"
}
```

#### User Login
**POST** `/api/auth/login`

Authenticate user with username and password.

```json
{
  "username": "john_doe",
  "password": "SecurePassword123"
}
```

**Response:**
```json
{
  "userId": "user-123",
  "username": "john_doe",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh-token-here",
  "expiresAt": "2024-01-01T12:00:00Z"
}
```

#### Refresh Token
**POST** `/api/auth/refresh`

Refresh authentication token.

```json
{
  "refreshToken": "refresh-token-here"
}
```

#### Logout
**POST** `/api/auth/logout`

Logout and invalidate token.

```json
{
  "refreshToken": "refresh-token-here"
}
```

### Device Management (`/api/device/*`)

#### Register Device
**POST** `/api/device/register`

Register a new mobile device for backup. **Requires JWT authentication.**

**Headers:**
```
Authorization: Bearer <jwt-token>
```

```json
{
  "deviceName": "iPhone 15 Pro",
  "deviceModel": "iPhone15,2",
  "deviceId": "optional-custom-id",
  "settings": {
    "autoBackupEnabled": true,
    "backupStartTime": "22:00:00",
    "backupEndTime": "06:00:00",
    "backupOnlyOnWifi": true,
    "backupOnlyWhenCharging": false,
    "allowedExtensions": [".jpg", ".jpeg", ".png", ".heic", ".mp4"],
    "maxFileSize": 104857600,
    "compressImages": false,
    "imageQuality": 85
  }
}
```

#### Get Device Info
**GET** `/api/device/{deviceId}`

**Requires JWT authentication.** Returns device info only if it belongs to the authenticated user.

#### Update Device Settings
**PUT** `/api/device/{deviceId}/settings`

**Requires JWT authentication.** Update settings for a device owned by the authenticated user.

#### Get All Devices
**GET** `/api/device`

**Requires JWT authentication.** Returns all devices belonging to the authenticated user.

#### Delete Device
**DELETE** `/api/device/{deviceId}`

**Requires JWT authentication.** Delete a device owned by the authenticated user.

### Backup Sessions (`/api/session/*`)

#### Start Backup Session
**POST** `/api/session/start`

**Requires JWT authentication.** Start a backup session for a device owned by the authenticated user.

**Headers:**
```
Authorization: Bearer <jwt-token>
```

```json
{
  "deviceId": "device-123",
  "sessionInfo": {
    "deviceName": "iPhone 15 Pro",
    "deviceModel": "iPhone15,2",
    "networkType": "WiFi",
    "isCharging": true,
    "batteryLevel": 85,
    "appVersion": "1.0.0",
    "osVersion": "iOS 17.1"
  }
}
```

#### Get Session Details
**GET** `/api/session/{sessionId}`

**Requires JWT authentication.** Returns session details only if it belongs to the authenticated user.

#### Update Session Progress
**PUT** `/api/session/{sessionId}`

**Requires JWT authentication.** Update session progress for a session owned by the authenticated user.

```json
{
  "processedItems": 45,
  "successfulBackups": 43,
  "failedBackups": 2,
  "skippedItems": 0,
  "totalSize": 104857600,
  "status": "InProgress"
}
```

#### Get Device Sessions
**GET** `/api/session/device/{deviceId}`

**Requires JWT authentication.** Returns sessions for a device owned by the authenticated user.

### Media Management (`/api/media/*`)

#### Upload Media
**POST** `/api/media/upload/{sessionId}`

**Requires JWT authentication.** Upload media files to an active backup session owned by the authenticated user.

#### Get Media Item
**GET** `/api/media/{mediaId}`

**Requires JWT authentication.** Returns media item only if it belongs to the authenticated user.

#### Get Device Media
**GET** `/api/media/device/{deviceId}?page=1&pageSize=50`

**Requires JWT authentication.** Returns media for a device owned by the authenticated user.

#### Get Thumbnail
**GET** `/api/media/{mediaId}/thumbnail`

**Requires JWT authentication.** Returns thumbnail for media owned by the authenticated user.

#### Search Media
**GET** `/api/media/device/{deviceId}/search?query=beach&fromDate=2024-01-01&toDate=2024-12-31`

**Requires JWT authentication.** Search media for a device owned by the authenticated user.

#### Get Media by Date Range
**GET** `/api/media/device/{deviceId}/date-range?fromDate=2024-01-01&toDate=2024-12-31`

**Requires JWT authentication.** Get media by date range for a device owned by the authenticated user.

#### Delete Media Item
**DELETE** `/api/media/{mediaId}`

**Requires JWT authentication.** Delete media item owned by the authenticated user.

### Statistics (`/api/stats/*`)

#### Get Device Stats
**GET** `/api/stats/device/{deviceId}`

**Requires JWT authentication.** Returns stats for a device owned by the authenticated user.

#### Get System Stats
**GET** `/api/stats/system`

**Requires JWT authentication.** Returns system-wide statistics.

## Mobile App Integration

### Typical Backup Flow

1. **User Registration/Login**: User creates account or logs in to get JWT token
2. **Device Registration**: Mobile app registers device under user account
3. **Session Creation**: App starts a backup session when conditions are met
4. **Media Upload**: App uploads photos/videos in batches
5. **Progress Updates**: App reports progress to the server
6. **Session Completion**: App marks session as complete

### Example Mobile App Code (JavaScript)

```javascript
class MediaBackupClient {
    constructor(serverUrl, username, password) {
        this.serverUrl = serverUrl;
        this.username = username;
        this.password = password;
        this.accessToken = null;
    }

    async login() {
        const response = await fetch(`${this.serverUrl}/api/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                username: this.username,
                password: this.password
            })
        });
        
        const authData = await response.json();
        this.accessToken = authData.accessToken;
        return authData;
    }

    async register(username, email, password) {
        const response = await fetch(`${this.serverUrl}/api/auth/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                username,
                email,
                password
            })
        });
        
        const authData = await response.json();
        this.accessToken = authData.accessToken;
        return authData;
    }

    async registerDevice(deviceName, deviceModel, deviceId) {
        const response = await fetch(`${this.serverUrl}/api/device/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.accessToken}`
            },
            body: JSON.stringify({
                deviceName,
                deviceModel,
                deviceId
            })
        });
        
        return await response.json();
    }

    async startBackupSession(deviceId, sessionInfo) {
        const response = await fetch(`${this.serverUrl}/api/session/start`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.accessToken}`
            },
            body: JSON.stringify({
                deviceId,
                sessionInfo
            })
        });
        
        return await response.json();
    }

    async uploadMedia(sessionId, file) {
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${this.serverUrl}/api/media/upload/${sessionId}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.accessToken}`
            },
            body: formData
        });
        
        return await response.json();
    }

    async updateSessionProgress(sessionId, progress) {
        const response = await fetch(`${this.serverUrl}/api/session/${sessionId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.accessToken}`
            },
            body: JSON.stringify(progress)
        });
        
        return await response.json();
    }
}

// Usage example
const backupClient = new MediaBackupClient('https://localhost:7109', 'john_doe', 'SecurePassword123');

// Authenticate first
const authResponse = await backupClient.login();
if (!authResponse.accessToken) {
    throw new Error('Authentication failed');
}

// Register device
const device = await backupClient.registerDevice('iPhone 15 Pro', 'iPhone15,2', 'iphone-15-001');

// Start backup when conditions are met
if (isWifiConnected && isCharging && isNightTime) {
    const session = await backupClient.startBackupSession(device.id, {
        networkType: 'WiFi',
        isCharging: true,
        batteryLevel: getBatteryLevel()
    });

    // Upload media files
    for (const mediaFile of mediaFiles) {
        await backupClient.uploadMedia(session.id, mediaFile);
    }

    // Mark session as complete
    await backupClient.updateSessionProgress(session.id, {
        status: 'Completed',
        processedItems: mediaFiles.length
    });
}
```

## Configuration

The API can be configured through `appsettings.json`:

```json
{
  "StorageSettings": {
    "BasePath": "/Users/craftycoder07/MediaBackup",
    "MaxFileSize": 104857600,
    "EnableThumbnails": true,
    "ThumbnailSize": 300,
    "CompressionQuality": 85
  },
  "BackupSettings": {
    "DefaultBackupWindow": {
      "StartTime": "22:00:00",
      "EndTime": "06:00:00"
    },
    "MaxConcurrentSessions": 5,
    "SessionTimeoutMinutes": 30,
    "RetentionDays": 90
  },
  "Jwt": {
    "Secret": "your-super-secret-key-that-is-at-least-32-characters-long",
    "Issuer": "PhotoVideoBackupAPI",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  }
}
```

## Security Considerations

- **User Authentication**: Secure JWT-based authentication with username/password
- **Password Security**: Passwords are hashed using PBKDF2 with random salts
- **Token-based Security**: JWT tokens for secure API access with refresh capabilities
- **User Data Isolation**: Each user can only access their own devices and media
- **HTTPS Required**: All communications should use HTTPS in production
- **File Validation**: Uploaded files are validated and hashed for integrity
- **Session Management**: Backup sessions have timeouts and limits
- **CORS Configuration**: Configure CORS appropriately for your mobile apps
- **Multi-Device Support**: Users can securely manage multiple devices under one account

## Performance

- **Asynchronous Processing**: All operations are non-blocking
- **Batch Uploads**: Support for uploading multiple files efficiently
- **Thumbnail Caching**: Thumbnails are generated once and cached
- **Pagination**: Large media collections are paginated
- **File Streaming**: Large files are streamed efficiently

## Storage Structure

```
/Users/craftycoder07/MediaBackup/
├── user-id-1/
│   ├── device-id-1/
│   │   ├── media-file-1.jpg
│   │   ├── media-file-2.mp4
│   │   └── ...
│   └── device-id-2/
│       └── ...
├── user-id-2/
│   └── ...
└── Thumbnails/
    ├── media-id-1_thumb.jpg
    └── ...
```

## Troubleshooting

### Common Issues

1. **Permission Denied**: Ensure the API has write access to the storage directory
2. **File Size Limits**: Check the MaxFileSize setting in configuration
3. **Session Timeouts**: Increase SessionTimeoutMinutes if needed
4. **Network Issues**: Ensure stable WiFi connection for mobile devices

### Debug Mode

Run the application in debug mode for detailed logging:

```bash
dotnet run --environment Development
```

## Future Enhancements

- **Image Processing**: Automatic image enhancement and filters
- **Face Recognition**: Tag people in photos
- **Location Services**: Map integration for photo locations
- **Sharing**: Share photos with other users
- **Backup Scheduling**: More sophisticated backup scheduling
- **Cloud Sync**: Optional cloud backup integration
- **Web Interface**: Web-based photo viewer and management

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License. 