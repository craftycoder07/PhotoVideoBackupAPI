# Circular Reference Fix - Summary

## Problem
When uploading media, a `JsonException` was thrown due to circular references:
- `User` → `BackupSessions` → `User` → `BackupSessions` → ...
- `BackupSession` → `User` → `BackupSessions` → ...
- `MediaItem` → `User` → `BackupSessions` → ...

## Solution Applied

### 1. Configured JSON Serialization Options (Program.cs)
- Added `ReferenceHandler.IgnoreCycles` to handle circular references at the JSON level
- This provides a fallback if navigation properties are accidentally serialized

### 2. Added JsonIgnore Attributes to Navigation Properties
- **User.cs**: Added `[JsonIgnore]` to `BackupSessions` navigation property
- **BackupSession.cs**: Added `[JsonIgnore]` to `User` navigation property (Items is serializable)
- **MediaItem.cs**: Added `[JsonIgnore]` to `User` and `Session` navigation properties

**Note**: `BackupSession.Items` is NOT marked with `[JsonIgnore]` because we want to include media items when returning a session. However, the items themselves have `[JsonIgnore]` on their `User` and `Session` properties to prevent cycles.

## Test Cases to Verify

### 1. Media Upload
- **Endpoint**: `POST /api/media/upload/{sessionId}`
- **Test**: Upload a media file to a session
- **Expected**: Returns MediaItem without circular reference error
- **Verify**: Response contains MediaItem data without User/Session navigation properties

### 2. Get Media Item
- **Endpoint**: `GET /api/media/{mediaId}`
- **Test**: Retrieve a media item by ID
- **Expected**: Returns MediaItem without circular reference error
- **Verify**: Response contains MediaItem data without User/Session navigation properties

### 3. Get User Media
- **Endpoint**: `GET /api/media?page=1&pageSize=50`
- **Test**: Get paginated list of user's media items
- **Expected**: Returns list of MediaItems without circular reference error
- **Verify**: Response contains array of MediaItems without navigation properties

### 4. Start Backup Session
- **Endpoint**: `POST /api/session/start`
- **Test**: Start a new backup session
- **Expected**: Returns BackupSession without circular reference error
- **Verify**: Response contains BackupSession data without User/Items navigation properties

### 5. Get Backup Session
- **Endpoint**: `GET /api/session/{sessionId}`
- **Test**: Get backup session details
- **Expected**: Returns BackupSession with Items array but without User navigation property
- **Verify**: Response contains BackupSession with Items array, and Items don't have User/Session properties (preventing cycles)

### 6. Get User Backup Sessions
- **Endpoint**: `GET /api/session`
- **Test**: Get all backup sessions for current user
- **Expected**: Returns list of BackupSessions without circular reference error
- **Verify**: Response contains array of BackupSessions without User navigation properties

### 7. Get Current User
- **Endpoint**: `GET /api/user`
- **Test**: Get current user information
- **Expected**: Returns User without circular reference error
- **Verify**: Response contains User data without BackupSessions navigation property

### 8. Update User Settings
- **Endpoint**: `PUT /api/user/settings`
- **Test**: Update user settings
- **Expected**: Returns updated User without circular reference error
- **Verify**: Response contains User data without BackupSessions navigation property

### 9. Search User Media
- **Endpoint**: `GET /api/media/search?query=test`
- **Test**: Search user's media items
- **Expected**: Returns list of MediaItems without circular reference error
- **Verify**: Response contains array of MediaItems without navigation properties

### 10. Get Media by Date Range
- **Endpoint**: `GET /api/media/date-range?fromDate=2024-01-01&toDate=2024-12-31`
- **Test**: Get media items within date range
- **Expected**: Returns list of MediaItems without circular reference error
- **Verify**: Response contains array of MediaItems without navigation properties

## Files Modified

1. **Program.cs**: Added JSON serialization options with `ReferenceHandler.IgnoreCycles`
2. **Models/User.cs**: Added `[JsonIgnore]` to `BackupSessions` property
3. **Models/BackupSession.cs**: Added `[JsonIgnore]` to `User` and `Items` properties
4. **Models/MediaItem.cs**: Added `[JsonIgnore]` to `User` and `Session` properties

## Verification Steps

1. Build the project: `dotnet build`
2. Run the application: `dotnet run`
3. Test each endpoint using Swagger UI or Postman
4. Verify responses don't contain navigation properties that would cause cycles
5. Check logs for any serialization errors

## Notes

- Navigation properties are still available for Entity Framework queries and relationships
- They are simply excluded from JSON serialization to prevent circular references
- The `ReferenceHandler.IgnoreCycles` provides an additional safety net
- All entity IDs (UserId, SessionId) are still included in responses for reference

