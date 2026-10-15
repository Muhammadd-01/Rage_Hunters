# RAGE HUNTERS — Testing Guide

## Automated Tests

### Edit Mode Tests (Unit Tests)
Location: `Assets/Game/Tests/EditMode/`
- Test damage calculations
- Test inventory add/remove/capacity
- Test XP and level calculations
- Test villain name sanitization
- Test event bus subscribe/publish/unsubscribe

### Play Mode Tests (Integration Tests)
Location: `Assets/Game/Tests/PlayMode/`
- Test player controller movement
- Test scene loading
- Test UI screen transitions
- Test AI state transitions

### Running Tests
- Unity Editor: Window → General → Test Runner
- CLI: `Unity -batchmode -runTests -testResults results.xml`

## Manual Test Procedures

### Movement Test
1. Enter gameplay scene
2. Use joystick to move in all directions
3. Verify sprint button increases speed
4. Verify crouch reduces height and speed
5. Verify jump works with gravity

### Combat Test
1. Approach an AI enemy
2. Press attack button
3. Verify hit detection and damage numbers
4. Verify enemy health decreases
5. Verify death state on zero health

### Match Flow Test
1. Start Solo Survival from main menu
2. Complete at least one objective
3. Navigate to extraction point
4. Hold interact to extract
5. Verify match results screen shows correct data
6. Return to main menu

### Device Test Checklist
| Test | Android | iOS | Notes |
|------|---------|-----|-------|
| App launches | ⬜ | ⬜ | |
| Touch controls respond | ⬜ | ⬜ | |
| 30 FPS stable | ⬜ | ⬜ | |
| No crash in 10min session | ⬜ | ⬜ | |
| Audio plays | ⬜ | ⬜ | |
| Settings save/load | ⬜ | ⬜ | |

## Status
- [ ] Edit mode test framework configured
- [ ] First unit tests written
- [ ] Play mode tests running
- [ ] Manual test on Android device
