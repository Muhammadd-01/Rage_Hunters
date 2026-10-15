# RAGE HUNTERS — Development Plan

## Milestones

### Milestone 1: Project Initialization ✅ COMPLETE
- [x] Inspect development environment
- [x] Initialize Git repository
- [x] Create .gitignore
- [x] Create directory structure
- [x] Configure project settings (landscape, bundle ID, internet permission)
- [x] Create core framework (GameManager, SceneLoader, Singleton, EventBus, ObjectPool)
- [x] Create player controller and camera
- [x] Create touch input controls
- [x] Create UI framework and screens
- [x] Create AI state machine foundation
- [x] Create combat and inventory foundations
- [x] Verify compilation and resolve all C# syntax/namespace errors
- [x] Create automated unit tests (DamageSystem, EventBus, Progression)
- [ ] Attempt Android build (blocked: requires user installing Android Build Support via Unity Hub)
- [x] Write initial documentation (12 comprehensive docs)
- [x] First Git commit

### Milestone 2: Playable Character + One Map
- [ ] Third-person character with placeholder model (capsule/cube)
- [ ] Touch controls functional on device
- [ ] Camera orbit working with touch
- [ ] Build first map: Abandoned Office Complex (greybox)
- [ ] NavMesh baked on the map
- [ ] Player can walk, run, sprint, crouch, jump in the map
- [ ] Basic lighting setup
- [ ] Working Android APK with playable character

### Milestone 3: Combat, Inventory, Extraction
- [ ] Health and stamina systems active
- [ ] Melee combat functional
- [ ] Hit detection working
- [ ] Loot drops spawning on map
- [ ] Inventory system functional
- [ ] Items can be picked up and equipped
- [ ] Extraction point defined and working
- [ ] Solo match: enter → fight → loot → extract → results

### Milestone 4: Villain Creator + AI
- [ ] Villain archetype ScriptableObjects defined
- [ ] Villain creator UI functional
- [ ] Save/load villain configurations
- [ ] AI enemies patrol, detect, chase, attack
- [ ] Different archetypes have different behaviors
- [ ] AI uses NavMesh for pathfinding
- [ ] Villain AI present in solo survival matches

### Milestone 5: Full Menu Flow + HUD
- [ ] Splash → Loading → Main Menu → Game Mode → Gameplay → Results
- [ ] HUD showing health, stamina, objectives, timer
- [ ] Pause menu functional
- [ ] Settings screen (graphics, audio, controls)
- [ ] Basic tutorial/onboarding
- [ ] All UI screens navigable

### Milestone 6: Online Multiplayer
- [ ] Photon Fusion 2 integrated
- [ ] Private lobby: create, join, ready, start
- [ ] Two-device movement synchronization verified
- [ ] Combat synchronization
- [ ] Match lifecycle over network
- [ ] Disconnect handling

### Milestone 7: Backend + Persistence
- [ ] PlayFab SDK integrated
- [ ] Guest and email authentication
- [ ] Player profile saving
- [ ] Villain configurations saved to cloud
- [ ] Match history recorded
- [ ] Server-side validation for rewards

### Milestone 8: Complete Match Flow
- [ ] Online matchmaking
- [ ] Role assignment (Hunter vs Villain)
- [ ] Team support
- [ ] Full extraction mode online
- [ ] Spectating on death
- [ ] Reconnection support

### Milestone 9: Content + Polish
- [ ] Map 2: Industrial District
- [ ] Additional villain archetypes
- [ ] Cosmetic system
- [ ] Achievements
- [ ] Audio: SFX, ambience, music
- [ ] Animation polish

### Milestone 10: Release Preparation
- [ ] Performance profiling on target devices
- [ ] 30 FPS stable on mid-range Android
- [ ] Regression testing
- [ ] Android release candidate (signed AAB)
- [ ] iOS build verification
- [ ] Store assets and metadata
- [ ] Privacy policy and compliance

## Timeline Estimate

Each milestone represents approximately 1-3 weeks of focused development, depending on asset creation requirements and testing scope.

## Dependencies

| Dependency | Required By | Status |
|-----------|-------------|--------|
| Android Build Support | Milestone 1 | ❌ Not installed |
| iOS Build Support | Milestone 10 | ❌ Not installed |
| Photon Fusion 2 SDK | Milestone 6 | Not yet imported |
| PlayFab SDK | Milestone 7 | Not yet imported |
| Character models | Milestone 2 | Not yet sourced |
| Environment assets | Milestone 2 | Not yet sourced |
| Sound effects | Milestone 9 | Not yet sourced |
| Music | Milestone 9 | Not yet sourced |
