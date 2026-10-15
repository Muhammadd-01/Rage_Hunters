# RAGE HUNTERS — Backend Setup

## Architecture

PlayFab (Microsoft Azure PlayFab) is the planned backend service.

## Services Used

| PlayFab Feature | Purpose |
|----------------|---------|
| Authentication | Guest, email/password, device-based login |
| Player Data | Villain configs, settings, preferences |
| Player Statistics | XP, level, kills, wins, play time |
| Leaderboards | Global and friends rankings |
| Economy | Cosmetic items, in-game currency |
| Cloud Script | Server-side validation of rewards and purchases |
| Analytics | Player behavior, match metrics |

## Setup Steps

### 1. Create PlayFab Account
1. Go to https://playfab.com
2. Create a free account
3. Create a new Title (game)
4. Note your **Title ID** (required for SDK)

### 2. Install PlayFab Unity SDK
1. Download from https://github.com/PlayFab/UnitySDK
2. Import the `.unitypackage`
3. Set Title ID in PlayFab Editor Extensions

### 3. Configure Authentication
- Enable Guest authentication (for quick testing)
- Enable Email/Password authentication
- Configure password requirements

### 4. Set Up Player Data
- Create custom player data keys for villain storage
- Configure read-only vs read-write permissions

### 5. Configure Leaderboards
- Create statistic definitions for: TotalXP, TotalKills, TotalWins

## Security Rules

- Never embed PlayFab Developer Secret Key in the client
- Use Cloud Scripts for server-validated operations
- Rate-limit client API calls
- Validate all reward claims server-side
- Use HTTPS for all API calls (default with PlayFab)

## Status
- [ ] PlayFab account created
- [ ] PlayFab SDK imported
- [ ] Guest authentication working
- [ ] Player data saving/loading
- [ ] Statistics recording
- [ ] Leaderboard setup
- [ ] Cloud Script validation

## Cost
- Free tier: 100,000 MAU
- Pay-as-you-go beyond free tier
- See https://playfab.com/pricing for current rates
