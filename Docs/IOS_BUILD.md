# RAGE HUNTERS — iOS Build Guide

## Prerequisites
- macOS with Xcode 15+ (currently Xcode 26.0.1 ✅)
- Unity iOS Build Support module (NOT YET INSTALLED)
- Apple Developer account ($99/year)
- Provisioning profile and signing certificate

## Configuration
| Setting | Value |
|---------|-------|
| Bundle Identifier | `com.ragehunters.game` |
| Target iOS Version | 15.0 |
| Scripting Backend | IL2CPP |
| Architecture | ARM64 |

## Build Steps
1. Install iOS Build Support via Unity Hub
2. File → Build Settings → iOS → Switch Platform
3. Build → generates Xcode project
4. Open Xcode project → configure signing
5. Build and run on device or TestFlight

## Status
- [ ] iOS Build Support installed
- [ ] Xcode project generated
- [ ] Builds on simulator
- [ ] Builds on device
- [ ] TestFlight build
