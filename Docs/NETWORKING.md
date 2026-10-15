# RAGE HUNTERS — Networking Architecture

## Overview

RAGE HUNTERS uses Photon Fusion 2 for real-time multiplayer networking (planned for Milestone 6).

## Network Model

### Development Phase (Current)
- **Solo mode**: All game logic runs locally, no networking required
- **Editor testing**: Keyboard/mouse + touch simulation

### Production Architecture (Milestone 6+)
- **Photon Fusion 2 Shared Mode**: For private lobbies (one player is the host)
- **Photon Fusion 2 Host Mode**: For competitive matches with server authority
- **Photon Cloud**: Managed relay and matchmaking infrastructure

## Authority Model

| State | Authority | Prediction | Notes |
|-------|-----------|-----------|-------|
| Player movement | Server-validated | Client-predicted | Rollback on mismatch |
| Combat damage | Server-authoritative | None | Server calculates all damage |
| Inventory changes | Server-authoritative | Optimistic UI | Server validates ownership |
| Match outcomes | Server-authoritative | None | Cannot be forged by client |
| Rewards/XP | Server-validated | None | Backend validates before persisting |
| AI behavior | Host/Server | None | Runs on authoritative host |

## Session Lifecycle

```
Connect → Authenticate → Join Lobby → Ready Up → Match Start
→ Gameplay (synced) → Match End → Results → Return to Lobby
```

## Anti-Cheat Protections (Planned)

- Server-side damage validation
- Movement speed limiting
- Reward claim deduplication
- Rate limiting on sensitive operations
- Inventory change validation
- Match result server authority

## Cost Estimates

| Tier | CCU | Monthly Cost |
|------|-----|-------------|
| Free | 20 | $0 |
| Starter | 100 | ~$95 |
| Growth | 500 | ~$395 |

## Status
- [ ] Photon Fusion 2 SDK imported
- [ ] NetworkRunner configured
- [ ] Player spawning over network
- [ ] Movement synchronization
- [ ] Combat synchronization
- [ ] Lobby system
- [ ] Matchmaking
- [ ] Disconnect handling
