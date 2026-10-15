# RAGE HUNTERS — Changelog

## [0.1.0] - 2026-10-12 (Milestone 1)

### Added
- Project initialization with Unity 6 (6000.6.5f1) and URP
- Git repository with Unity .gitignore
- Complete directory structure for all game modules
- Project settings: landscape orientation, bundle ID (com.ragehunters.game), internet permission

#### Core Framework (7 scripts)
- `Singleton<T>` — generic DontDestroyOnLoad singleton base
- `GameManager` — game state management (Initializing → MainMenu → InGame → Paused → MatchEnd)
- `SceneLoader` — async scene loading with progress callbacks
- `GameConstants` — centralized scene names, tags, layers, animation hashes, gameplay defaults
- `EventBus` — type-safe publish/subscribe event system with struct events
- `ObjectPool` — generic GameObject pool with pre-warm and auto-expand
- `GameBootstrapper` — initialization entry point

#### Player System (4 scripts)
- `PlayerController` — third-person CharacterController with walk/run/sprint/crouch/jump
- `PlayerInputHandler` — bridges Unity Input System + touch controls to game logic
- `PlayerAnimator` — drives Animator parameters from player state
- `PlayerStats` — health, stamina, damage, death, respawn

#### Camera (1 script)
- `ThirdPersonCamera` — orbit camera with collision avoidance and touch support

#### Touch Controls (5 scripts)
- `VirtualJoystick` — UI-based movement joystick with dead zone and modes
- `TouchButton` — reusable action button with hold/toggle, cooldown, visual feedback
- `TouchCameraRegion` — right-side touch region for camera look
- `TouchInputManager` — coordinates all touch inputs, visibility toggling
- `MobileInputActions` — platform detection and config helpers

#### AI System (11 scripts)
- `AIStateMachine` — generic FSM with enum-keyed states
- `AIState` — abstract base with lifecycle hooks
- `AIController` — NavMeshAgent + perception (vision cone + hearing) + combat
- States: Idle, Patrol, Detect, Chase, Attack, Search, Investigate, Retreat, ReturnToPatrol

#### Combat System (4 scripts)
- `IDamageable` — damage interface
- `DamageSystem` — static damage calculation
- `MeleeCombat` — sphere-cast melee with cooldown and crit
- `HitBox` — collider-based hit zone with damage multiplier

#### Inventory System (2 scripts)
- `InventorySystem` — slot-based inventory with equipment
- `LootDrop` — world pickup with interaction

#### ScriptableObjects (3 definitions)
- `ItemDataSO` — item definitions (name, type, rarity, stack)
- `WeaponDataSO` — weapon stats (damage, range, speed)
- `VillainArchetypeSO` — villain behavior configuration

#### UI Framework (10 scripts)
- `UIManager` — screen registry, show/hide, back stack
- `UIScreen` — base class with CanvasGroup transitions
- `SplashScreen`, `MainMenuScreen`, `GameModeScreen`
- `HUDScreen`, `PauseMenuScreen`, `SettingsScreen`
- `MatchResultsScreen`, `LoadingScreen`

#### Mission System (2 scripts)
- `MissionManager` — match lifecycle, objectives, extraction
- `ExtractionPoint` — timed extraction interaction

#### Villain Creator (1 script)
- `VillainCreator` — CRUD for villain configs with sanitization

#### Progression (1 script)
- `ProgressionManager` — XP, levels, match stats

#### Audio (1 script)
- `AudioManager` — SFX pool, music, ambience, volume controls

#### Save System (1 script)
- `SaveManager` — PlayerPrefs local persistence, guest ID

#### Build System (1 script)
- `BuildAndroid` — Editor build script for APK/AAB

#### Documentation
- README.md, ARCHITECTURE.md, DEVELOPMENT_PLAN.md
- GAMEPLAY_DESIGN.md, ANDROID_BUILD.md, KNOWN_ISSUES.md
- CHANGELOG.md

### Known Blockers
- Android Build Support module not installed
- iOS Build Support module not installed
- No character/environment art assets yet
- Scenes not yet configured in Unity Editor
