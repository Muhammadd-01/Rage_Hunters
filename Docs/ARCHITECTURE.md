# RAGE HUNTERS — Architecture

## Overview

RAGE HUNTERS follows a modular, layered architecture designed for mobile-first multiplayer gameplay.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    UNITY ENGINE                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐              │
│  │  Scenes  │  │ Prefabs  │  │ScriptObj │              │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘              │
│       │              │              │                    │
│  ┌────┴──────────────┴──────────────┴────┐              │
│  │           GAME FRAMEWORK              │              │
│  │  GameManager · SceneLoader · EventBus │              │
│  │  ObjectPool · GameBootstrapper        │              │
│  └────┬──────────────┬──────────────┬────┘              │
│       │              │              │                    │
│  ┌────┴────┐   ┌─────┴─────┐  ┌────┴─────┐             │
│  │ PLAYER  │   │    AI     │  │  COMBAT  │             │
│  │Controller│  │StateMachine│  │DamageCalc│             │
│  │InputHndl│   │Controller │  │MeleeCombat│            │
│  │Animator │   │States[]   │  │HitBox    │             │
│  │Stats    │   │Detection  │  │IDamageable│            │
│  └────┬────┘   └─────┬─────┘  └────┬─────┘             │
│       │              │              │                    │
│  ┌────┴────┐   ┌─────┴─────┐  ┌────┴─────┐             │
│  │  INPUT  │   │ INVENTORY │  │ MISSIONS │             │
│  │Joystick │   │System     │  │Manager   │             │
│  │TouchBtn │   │LootDrop   │  │Objectives│             │
│  │CamRegion│   │Equipment  │  │Extraction│             │
│  └─────────┘   └───────────┘  └──────────┘              │
│                                                         │
│  ┌─────────────────────────────────────────┐            │
│  │              UI LAYER                   │            │
│  │  UIManager · UIScreen (base)            │            │
│  │  SplashScreen · MainMenuScreen          │            │
│  │  HUDScreen · PauseMenu · Settings       │            │
│  │  MatchResults · LoadingScreen           │            │
│  └─────────────────────────────────────────┘            │
│                                                         │
│  ┌──────────────────┐  ┌───────────────────┐            │
│  │   MULTIPLAYER    │  │     BACKEND       │            │
│  │  (Photon Fusion) │  │   (PlayFab)       │            │
│  │  NetworkManager  │  │  Auth · Data      │            │
│  │  LobbyManager    │  │  Leaderboards     │            │
│  │  MatchManager    │  │  Economy          │            │
│  └──────────────────┘  └───────────────────┘            │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## Namespace Convention

All game code lives under the `RageHunters` root namespace:

| Namespace | Purpose | Directory |
|-----------|---------|-----------|
| `RageHunters.Core` | Singletons, events, managers, pooling | `Scripts/Core/` |
| `RageHunters.Player` | Player controller, stats, animations | `Scripts/Player/` |
| `RageHunters.Camera` | Camera controller, effects | `Scripts/Camera/` |
| `RageHunters.Input` | Touch controls, input bridging | `Scripts/Input/` |
| `RageHunters.Combat` | Damage, weapons, hit detection | `Scripts/Combat/` |
| `RageHunters.Survival` | Health, stamina, extraction | `Scripts/Survival/` |
| `RageHunters.Villains` | Villain creator, data, archetypes | `Scripts/Villains/` |
| `RageHunters.AI` | State machine, enemy behaviors | `Scripts/AI/` |
| `RageHunters.Inventory` | Items, equipment, loot | `Scripts/Inventory/` |
| `RageHunters.Missions` | Objectives, tracking, extraction | `Scripts/Missions/` |
| `RageHunters.UI` | All UI screens and framework | `Scripts/UI/` |
| `RageHunters.Multiplayer` | Networking, lobbies, matches | `Scripts/Multiplayer/` |
| `RageHunters.Progression` | XP, levels, achievements | `Scripts/Progression/` |
| `RageHunters.Audio` | Audio management | `Scripts/Audio/` |
| `RageHunters.SaveSystem` | Local and cloud persistence | `Scripts/SaveSystem/` |

## Design Patterns

### Singleton Pattern
Core managers use a generic `Singleton<T>` base class that:
- Ensures single instance via `DontDestroyOnLoad`
- Destroys duplicate instances
- Provides static `Instance` accessor

### Event Bus
Decoupled communication via `EventBus` with type-safe struct events.
Systems publish events; other systems subscribe without direct references.

### State Machine
AI enemies use a finite state machine with discrete states (Idle → Patrol → Detect → Chase → Attack → Search → Retreat).

### ScriptableObject Configuration
All game data (items, weapons, villain archetypes, missions) is defined in ScriptableObjects for designer-friendly editing without code changes.

### Object Pooling
Frequently instantiated objects (projectiles, effects, loot drops) use `ObjectPool` to avoid garbage collection spikes.

## Scene Flow

```
SplashScene → MainMenuScene → GameplayScene
                 ↑                    │
                 │    (match end)     │
                 └────────────────────┘
```

## Build Targets

| Platform | Status | Build Output |
|----------|--------|-------------|
| Android (APK) | Primary | `Build/Android/RageHunters.apk` |
| Android (AAB) | Release | `Build/Android/RageHunters.aab` |
| iOS | Secondary | Xcode project in `Build/iOS/` |
| macOS Standalone | Dev testing | Editor play mode |

## Threading Model

- Game logic runs on Unity's main thread
- Networking callbacks marshalled to main thread by Photon/NGO
- Async scene loading uses Unity coroutines / async-await
- Backend calls use async UnityWebRequest or PlayFab SDK callbacks
