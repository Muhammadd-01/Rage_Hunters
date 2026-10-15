# RAGE HUNTERS — Known Issues

## Critical Blockers

| Issue | Impact | Resolution |
|-------|--------|-----------|
| Android Build Support not installed | Cannot produce APK/AAB | Install via Unity Hub → Add Modules |
| iOS Build Support not installed | Cannot produce iOS builds | Install via Unity Hub → Add Modules |
| No Android SDK on system | No device testing possible | Installed with Android Build Support module |

## Compilation Issues (To Verify)

| Issue | File | Status |
|-------|------|--------|
| Need to verify all cross-namespace references compile | All scripts | Pending Unity compilation test |

## Missing Assets

| Asset | Purpose | Priority |
|-------|---------|----------|
| Character models | Player and villain 3D models | High (Milestone 2) |
| Environment assets | Office complex map geometry | High (Milestone 2) |
| Animation clips | Character movement/combat anims | High (Milestone 2) |
| UI icons and sprites | Button icons, item icons, HUD elements | Medium (Milestone 5) |
| Sound effects | Combat, footsteps, interactions | Medium (Milestone 9) |
| Music tracks | Menu and gameplay music | Low (Milestone 9) |
| App icon | Android/iOS launcher icon | Medium (Milestone 10) |

## Not Yet Implemented

| Feature | Milestone |
|---------|-----------|
| Photon Fusion 2 multiplayer integration | 6 |
| PlayFab backend integration | 7 |
| Character model rendering | 2 |
| Environment map creation | 2 |
| NavMesh baking | 2 |
| Scene setup in Unity Editor | 2 |
| Prefab creation | 2 |
| Animator controller setup | 2 |
| Friends and invitation system | 8 |
| Report/mute/block system | 8 |
| Achievements system | 9 |
| Leaderboards | 9 |
| Tutorial/onboarding | 5 |

## Design Decisions Needed

- [ ] Character art style: realistic, stylized, or low-poly?
- [ ] Should we use free Unity Asset Store assets for prototyping?
- [ ] Photon Fusion 2 vs Unity NGO final decision (currently Photon)
- [ ] PlayFab title setup and credentials
- [ ] Audio licensing source (freesound.org, custom, asset store?)

## Last Updated
2026-10-12 — Milestone 1 in progress
