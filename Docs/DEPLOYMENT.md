# RAGE HUNTERS — Deployment Guide

## Environments

| Environment | Purpose | Backend | Multiplayer |
|------------|---------|---------|-------------|
| Development | Local testing | PlayerPrefs local | None / LAN |
| Staging | Integration testing | PlayFab (dev title) | Photon (20 CCU free) |
| Production | Live release | PlayFab (prod title) | Photon (paid tier) |

## Android Deployment

### Internal Testing (APK)
1. Build development APK (see ANDROID_BUILD.md)
2. Install via `adb install -r RageHunters-dev.apk`
3. Test on physical device

### Google Play Internal Testing
1. Create app in Google Play Console
2. Upload signed AAB
3. Configure internal test track
4. Distribute to testers

### Production Release
1. Complete store listing (screenshots, description, privacy policy)
2. Set content rating questionnaire
3. Upload production-signed AAB
4. Submit for review

## Backend Deployment (PlayFab)
- No server deployment required — PlayFab is fully managed
- Configure Title Settings in PlayFab dashboard
- Deploy Cloud Scripts via PlayFab Game Manager
- Set up webhooks for events if needed

## Multiplayer Server (Photon)
- Photon Cloud is fully managed
- Configure app in Photon Dashboard (dashboard.photonengine.com)
- Set regions, CCU limits, webhooks
- No self-hosted servers needed for initial release

## Cost Summary (Launch Estimate)

| Service | Monthly Cost | Annual |
|---------|-------------|--------|
| Photon 100 CCU | ~$95 | ~$1,140 |
| PlayFab (free tier) | $0 | $0 |
| Google Play Developer | — | $25 (one-time) |
| Apple Developer | — | $99/year |
| **Total (first year)** | | **~$1,264** |

## Monitoring
- PlayFab Analytics for player metrics
- Photon Dashboard for connection metrics
- Unity Cloud Diagnostics for crash reports (optional)
- Custom logging via PlayFab Events

## Backup & Recovery
- PlayFab handles data backups automatically
- Export player data periodically via PlayFab Admin API
- Version all Cloud Scripts in Git
