# PRD 2 — API Modularization (Controller Separation)

## 1. Overview

All media backup endpoints currently reside under a single controller, reducing readability and maintainability.  
This change will modularize the API into separate logical controllers, aligning with REST best practices.

---

## 2. Goals

| Goal | Description |
|------|--------------|
| **Improve API organization** | Split API endpoints by logical domain |
| **Increase maintainability** | Smaller controllers, easier testing |
| **Prepare for versioning** | Easier to apply version upgrades in future (e.g., `/v2/`) |

---

## 3. Functional Requirements

### Controller Breakdown

| Controller | Description | Endpoints |
|-------------|--------------|------------|
| **AuthController** | Manages login, registration, token refresh | `/api/auth/*` |
| **DeviceController** | Device registration and management | `/api/device/*` |
| **SessionController** | Start/update/get backup sessions | `/api/session/*` |
| **MediaController** | Media management and retrieval | `/api/media/*` |
| **StatsController** | Device/system statistics | `/api/stats/*` |

---

## 4. API Routing Changes

Refactor existing endpoints accordingly. For example:

**Old:**
```
POST /api/mediabackup/devices/{deviceId}/sessions
```

**New:**
```
POST /api/session/start
GET /api/session/{sessionId}
```

---

## 5. Technical Requirements

- Update startup routing to register controllers dynamically via convention.  
- Use **feature folders** in .NET 8 for clean organization:

```
/Features
  /Devices
  /Sessions
  /Media
  /Stats
```

- Move DTOs and validation into `/Models` per feature.

---

## 6. Acceptance Criteria

- ✅ Each controller handles only its domain.  
- ✅ API documentation (Swagger/OpenAPI) shows distinct sections for each.  
- ✅ No changes to endpoint paths (backward-compatible routing aliases supported).  

---
