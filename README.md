# RAGE HUNTERS

A mobile-first 3D multiplayer survival game built with Unity 6.

## Game Premise

Players enter a fictional survival world where they create custom villains inspired by frustrating everyday experiences. Hunt targets, evade hunters, collect loot, complete missions, and survive dangerous environments.

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Engine | Unity 6 (6000.6.5f1) |
| Language | C# |
| Rendering | Universal Render Pipeline (URP) |
| Input | Unity Input System + Custom Touch Controls |
| Navigation | Unity AI Navigation (NavMesh) |
| Multiplayer | Photon Fusion 2 (planned) |
| Backend | PlayFab (planned) |
| Primary Platform | Android |
| Secondary Platform | iOS |

## Project Structure

```
Assets/Game/Scripts/     - All game C# code (namespaced under RageHunters.*)
Assets/Game/Scenes/      - Unity scenes
Assets/Game/Prefabs/     - Reusable prefabs
Assets/Game/ScriptableObjects/ - Data-driven configuration assets
Assets/Game/Art/         - Textures, models, shaders
Assets/Game/Audio/       - Sound effects and music
Assets/Game/Animations/  - Animator controllers and clips
Docs/                    - Project documentation
BuildScripts/            - Build automation scripts
```

## Getting Started

### Prerequisites
- Unity 6 (6000.6.5f1) via Unity Hub
- Android Build Support module (install via Unity Hub → Add Modules)
- iOS Build Support module (optional, for iOS builds)
- Git

### Setup
1. Clone this repository
2. Open the project in Unity Hub (select Unity 6000.6.5f1)
3. Wait for package resolution and asset import
4. Open `Assets/Game/Scenes/SplashScene.unity` (or `Assets/Scenes/SampleScene.unity` for initial testing)

### Building for Android
See [ANDROID_BUILD.md](Docs/ANDROID_BUILD.md)

### Building for iOS
See [IOS_BUILD.md](Docs/IOS_BUILD.md)

## Documentation

- [Architecture](Docs/ARCHITECTURE.md)
- [Development Plan](Docs/DEVELOPMENT_PLAN.md)
- [Gameplay Design](Docs/GAMEPLAY_DESIGN.md)
- [Networking](Docs/NETWORKING.md)
- [Backend Setup](Docs/BACKEND_SETUP.md)
- [Android Build](Docs/ANDROID_BUILD.md)
- [iOS Build](Docs/IOS_BUILD.md)
- [Testing](Docs/TESTING.md)
- [Security](Docs/SECURITY.md)
- [Asset Licenses](Docs/ASSET_LICENSES.md)
- [Deployment](Docs/DEPLOYMENT.md)
- [Known Issues](Docs/KNOWN_ISSUES.md)
- [Changelog](Docs/CHANGELOG.md)

## License

Proprietary. All rights reserved.
