# Sohojatri

Sohojatri is an MVP backend for **dynamic ride pooling / community commute matching** in Bangladesh.

## MVP Scope (Phase-1)
- OTP login (mobile number)
- Profile setup and trust level
- Journey request creation
- Real-time matching (radius + destination + time window)
- Temporary commute group creation (10-30 mins)
- Group chat + live location + fare proposal support
- Trip start/end and rider rating
- Safety report and block user flow
- Instant Commute Circle suggestions

## Deferred to Phase-2
- NID verification workflow
- SOS SMS automation
- Voice calling
- Corporate commute and subscription features
- Premium verification monetization

## Stack
- Backend: ASP.NET Core 10 (Minimal APIs + SignalR)
- Database: PostgreSQL (EF Core)
- Cache/Realtime support: Redis
- Map provider integration target: OpenStreetMap
- Frontend target: Flutter modules (Auth, Map, Journey, Group, Profile)

## Local Setup
1. Start infra:
   ```bash
   docker compose up -d
   ```
2. Update connection strings in:
   - `Sohojatri.Api/appsettings.json`
   - `Sohojatri.Api/appsettings.Development.json`
3. Run API:
   ```bash
   cd Sohojatri.Api
   dotnet run
   ```

## Realtime
- SignalR hub: `/hubs/commute`
- Events:
  - `group_message`
  - `location_update`

## Core API Surface
- `POST /api/auth/request-otp`
- `POST /api/auth/verify-otp`
- `GET /api/profile/me`
- `PUT /api/profile/me`
- `POST /api/journeys`
- `GET /api/journeys/instant-circle`
- `POST /api/groups/{groupId}/messages`
- `POST /api/groups/{groupId}/location`
- `POST /api/groups/{groupId}/start-trip`
- `POST /api/groups/{groupId}/complete-trip`
- `POST /api/groups/{groupId}/ratings`
- `POST /api/safety/reports`
- `POST /api/safety/block/{blockedUserId}`
- `GET /api/admin/moderation/reports`
- `GET /api/metrics/overview`

## Trust & Safety Model
- Verification levels: `MobileVerified -> NIDVerified -> FrequentTraveller`
- Reputation score recalculated from submitted ratings
- Moderation queue through report status lifecycle
- User block list enforced in matching

## Operations & Monitoring
`GET /api/metrics/overview` provides:
- Match success rate
- Cancellation rate
- Estimated response time metric
- Active circles
- Completed trips

## Pilot Rollout Suggestion
- Start with a single route (e.g., Agrabad ↔ GEC)
- Run closed beta and tune matching radius/time window
- Expand safety automation and premium features in Phase-2
