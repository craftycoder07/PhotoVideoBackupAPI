# Mobile Media Backup API

A .NET 8 Web API that acts like Google Photos for local backup. Mobile devices can automatically backup their photos and videos to your local server over WiFi during the night, providing a private, self-hosted alternative to cloud photo services.

## Features

- **Mobile Device Management**: Register and manage multiple mobile devices
- **Automatic Night Backup**: Configure backup windows (e.g., 10 PM - 6 AM)
- **Session-based Uploads**: Secure backup sessions with progress tracking
- **Media Organization**: Automatic categorization of photos and videos
- **Thumbnail Generation**: Fast preview generation for media items
- **Search & Filtering**: Find media by date, tags, or text search
- **Statistics & Analytics**: Track backup progress and storage usage
- **RESTful API**: Clean, intuitive endpoints for mobile apps
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

## API Endpoints

### Device Management

#### Register Device
**POST** `/api/mediabackup/devices/register`

Register a new mobile device for backup.

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
**GET** `/api/mediabackup/devices/{deviceId}`

#### Update Device Settings
**PUT** `/api/mediabackup/devices/{deviceId}/settings`

#### Get All Devices
**GET** `/api/mediabackup/devices`

### Backup Sessions

#### Start Backup Session
**POST** `/api/mediabackup/devices/{deviceId}/sessions`

```json
{
  "deviceName": "iPhone 15 Pro",
  "deviceModel": "iPhone15,2",
  "networkType": "WiFi",
  "isCharging": true,
  "batteryLevel": 85,
  "appVersion": "1.0.0",
  "osVersion": "iOS 17.1"
}
```

#### Upload Media
**POST** `/api/mediabackup/sessions/{sessionId}/upload`

Upload media files to an active backup session.

#### Update Session Progress
**PUT** `/api/mediabackup/sessions/{sessionId}`

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

#### Get Session Details
**GET** `/api/mediabackup/sessions/{sessionId}`

### Media Management

#### Get Device Media
**GET** `/api/mediabackup/devices/{deviceId}/media?page=1&pageSize=50`

#### Get Media Item
**GET** `/api/mediabackup/media/{mediaId}`

#### Get Thumbnail
**GET** `/api/mediabackup/media/{mediaId}/thumbnail`

#### Search Media
**GET** `/api/mediabackup/devices/{deviceId}/search?query=beach&fromDate=2024-01-01&toDate=2024-12-31`

#### Get Media by Date Range
**GET** `/api/mediabackup/devices/{deviceId}/media/date-range?fromDate=2024-01-01&toDate=2024-12-31`

### Statistics

#### Get Device Stats
**GET** `/api/mediabackup/devices/{deviceId}/stats`

#### Get System Stats
**GET** `/api/mediabackup/stats`

## Mobile App Integration

### Typical Backup Flow

1. **Device Registration**: Mobile app registers with the server
2. **Session Creation**: App starts a backup session when conditions are met
3. **Media Upload**: App uploads photos/videos in batches
4. **Progress Updates**: App reports progress to the server
5. **Session Completion**: App marks session as complete

### Example Mobile App Code (JavaScript)

```javascript
class MediaBackupClient {
    constructor(serverUrl, deviceId, apiKey) {
        this.serverUrl = serverUrl;
        this.deviceId = deviceId;
        this.apiKey = apiKey;
    }

    async registerDevice(deviceName, deviceModel) {
        const response = await fetch(`${this.serverUrl}/api/mediabackup/devices/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.apiKey}`
            },
            body: JSON.stringify({
                deviceName,
                deviceModel,
                deviceId: this.deviceId
            })
        });
        
        return await response.json();
    }

    async startBackupSession(sessionInfo) {
        const response = await fetch(`${this.serverUrl}/api/mediabackup/devices/${this.deviceId}/sessions`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.apiKey}`
            },
            body: JSON.stringify(sessionInfo)
        });
        
        return await response.json();
    }

    async uploadMedia(sessionId, file) {
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${this.serverUrl}/api/mediabackup/sessions/${sessionId}/upload`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.apiKey}`
            },
            body: formData
        });
        
        return await response.json();
    }

    async updateSessionProgress(sessionId, progress) {
        const response = await fetch(`${this.serverUrl}/api/mediabackup/sessions/${sessionId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.apiKey}`
            },
            body: JSON.stringify(progress)
        });
        
        return await response.json();
    }
}

// Usage example
const backupClient = new MediaBackupClient('https://localhost:7109', 'my-device-id', 'api-key');

// Start backup when conditions are met
if (isWifiConnected && isCharging && isNightTime) {
    const session = await backupClient.startBackupSession({
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
  }
}
```

## Security Considerations

- **API Key Authentication**: Each device gets a unique API key
- **HTTPS Required**: All communications should use HTTPS
- **File Validation**: Uploaded files are validated and hashed
- **Session Management**: Backup sessions have timeouts and limits
- **CORS Configuration**: Configure CORS appropriately for your mobile apps

## Performance

- **Asynchronous Processing**: All operations are non-blocking
- **Batch Uploads**: Support for uploading multiple files efficiently
- **Thumbnail Caching**: Thumbnails are generated once and cached
- **Pagination**: Large media collections are paginated
- **File Streaming**: Large files are streamed efficiently

## Storage Structure

```
/Users/craftycoder07/MediaBackup/
├── device-id-1/
│   ├── media-file-1.jpg
│   ├── media-file-2.mp4
│   └── ...
├── device-id-2/
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