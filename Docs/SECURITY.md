# RAGE HUNTERS — Security

## Principles
1. Never trust client-reported data for authoritative operations
2. Never embed secrets in the game client
3. Filter and sanitize all user-generated content
4. Rate-limit sensitive operations

## Client Security
- No API keys or secret credentials in source code
- No database connection strings in the client
- Credentials stored in environment variables or secure config (server-side only)
- HTTPS for all network communication

## User-Generated Content (Villain Creator)
- Nicknames: max 20 characters, profanity filtered, no special characters that could be used for injection
- Backstories: max 200 characters, sanitized
- No photo upload of real people
- No PII collection beyond account email
- All villain content is explicitly fictional

## Server Authority (Planned)
- Damage calculations validated server-side
- Inventory changes authorized by server
- Match results determined by server
- XP/rewards granted only after server validation
- Duplicate reward claims prevented

## Account Security
- Password hashing (handled by PlayFab)
- Account recovery via email
- Rate limiting on login attempts
- Session token expiry

## Privacy
- Minimal data collection
- Account deletion support required before release
- Privacy policy required before store submission
- GDPR/CCPA compliance documentation needed

## Moderation (Planned)
- Report player functionality
- Mute/block functionality
- Username/nickname filtering
- Content review for reported violations

## Status
- [x] Nickname profanity filter (basic)
- [x] Input length limits
- [ ] Server-side validation (requires Photon/PlayFab)
- [ ] Report/mute/block system
- [ ] Privacy policy
- [ ] Account deletion flow
