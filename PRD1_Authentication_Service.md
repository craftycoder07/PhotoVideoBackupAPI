# PRD 1 — Authentication Service & Unified Backup by User

## 1. Overview

Currently, the backup sessions are tied to a `deviceId`. This means that if a user owns multiple devices (e.g., iPhone and iPad), their media gets stored separately for each device.  
The goal of this change is to:

- Introduce a **user authentication system (username/password)**.  
- Associate backup sessions and stored media with the **user account**, not the device.  
- Allow devices to register under a single user account.  
- Design authentication as a **separate service** so it can be swapped (e.g., replaced with OAuth, JWT provider, or external identity service later).

---

## 2. Goals & Success Criteria

| Goal | Success Criteria |
|------|------------------|
| **Implement secure authentication service** | JWT-based or token-based login/logout flow working |
| **Backup unified per user** | Media uploaded from multiple devices appears under one user account |
| **Maintain device registration for metadata** | Device-specific info (model, name, battery, network) remains logged |
| **Authentication modular** | Can be replaced by an external identity provider with minimal refactor |

---

## 3. Functional Requirements

### 3.1 Authentication Endpoints

| Endpoint | Method | Description |
|-----------|---------|-------------|
| `/api/auth/register` | POST | Register new user with username, password, email |
| `/api/auth/login` | POST | Authenticate user and issue access + refresh tokens |
| `/api/auth/refresh` | POST | Refresh access token using refresh token |
| `/api/auth/logout` | POST | Invalidate refresh token |

**Request Example – Register**
```json
{
  "username": "sidchalke",
  "email": "sid@example.com",
  "password": "StrongPassword123"
}
```

**Response Example**
```json
{
  "userId": "U12345",
  "username": "sidchalke",
  "token": "jwt-token-here"
}
```

---

### 3.2 Device Registration

- Devices will now be **registered under a user account**.  
- Each device must include the **Authorization** header containing a JWT.  
- Device table structure should include `UserId` foreign key.

**Updated Endpoint**
```
POST /api/mediabackup/devices/register
```
- Auth required.  
- Automatically assigns device to `UserId`.

---

### 3.3 Backup Session Changes

| Old Behavior | New Behavior |
|---------------|---------------|
| Sessions tied to `deviceId` only | Sessions tied to authenticated `userId` + `deviceId` |
| No authentication | Requires valid JWT |

**Updated Endpoint**
```
POST /api/mediabackup/sessions/start
```

**Request Example**
```json
{
  "deviceId": "D1234",
  "networkType": "WiFi",
  "isCharging": true,
  "batteryLevel": 85
}
```

**Response Example**
```json
{
  "sessionId": "S1234",
  "userId": "U12345",
  "deviceId": "D1234",
  "status": "Active"
}
```

---

## 4. Non-Functional Requirements

- **Security:** Use password hashing (e.g., PBKDF2 or bcrypt), JWT token signing with HMAC-SHA256.  
- **Extensibility:** Auth layer implemented via interface `IAuthService`, allowing alternative providers (e.g., Azure AD, Auth0).  
- **Scalability:** Stateless token validation suitable for load-balanced environments.  
- **Backward Compatibility:** Existing devices can continue using `deviceId`-only flow temporarily via fallback mode.  

---

## 5. Database Changes

- **Add `Users` table:**  
  - `UserId (PK)`, `Username`, `Email`, `PasswordHash`, `CreatedAt`  
- **Update `Devices` table:**  
  - Add `UserId (FK)`  
- **Update `Sessions` table:**  
  - Add `UserId (FK)`  

---

## 6. Future Considerations

- Support social logins (Google, Apple).  
- Allow 2FA for sensitive data.  
- Enable per-device permissions (e.g., child device under parent account).  

---

## 7. Acceptance Criteria

- ✅ User can register/login/logout successfully.  
- ✅ Devices registered under a user appear in `/devices` endpoint filtered by user.  
- ✅ Backups from multiple devices show up in a unified media library.  
- ✅ API fails with `401 Unauthorized` when invalid or missing token.  

---
