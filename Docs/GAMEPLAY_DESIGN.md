# RAGE HUNTERS — Gameplay Design

## Core Loop

```
Launch → Sign In → Main Menu → Select Mode → Create/Select Villain
    → Enter Match → Explore → Loot → Fight → Complete Objectives
    → Extract / Die → Match Results → Rewards → Return to Menu
```

## Game Modes

### 1. Solo Survival
- Player vs AI enemies on a single map
- Objectives: collect items, eliminate targets, reach extraction
- AI difficulty scales with villain archetype settings
- No internet connection required
- Full match flow: spawn → objectives → extraction → results

### 2. Private Friends Lobby (Milestone 6)
- Host creates room with join code
- 2-8 players
- Host configures: map, time limit, AI count, game rules
- Lobby UI: player list, ready state, chat
- Host starts match when all ready

### 3. Online Survival (Milestone 8)
- Matchmaking for 4-8 players
- Mixed PvP + PvE
- Random map selection or vote
- Team or free-for-all

### 4. Hunter vs. Villain (Milestone 8)
- Asymmetric mode
- Hunters: track and eliminate assigned target villains
- Villains: survive, use stealth, reach extraction
- Victory: Hunters win by eliminating all villains; Villains win by extracting or surviving the timer

### 5. Extraction (Milestone 8)
- Collect high-value loot scattered on map
- Fight AI and other players for loot
- Reach extraction point with loot to keep it
- Death = lose all match-collected loot
- Permanent inventory/progression items are never lost

## Villain Archetypes

| Archetype | Behavior | Combat Style | Special |
|-----------|----------|-------------|---------|
| **The Office Tyrant** | Aggressive | Direct assault | Intimidation aura (debuff nearby players) |
| **The Backstabber** | Stealthy | Flanking attacks | Appears as ally on radar briefly |
| **The Manipulator** | Deceptive | Traps & decoys | Deploys decoy copies |
| **The Fake Friend** | Mixed | Ambush | Mimics friendly behavior then attacks |
| **The Ruthless Rival** | Aggressive | Competitive | Speed boost when target is nearby |
| **The Mastermind** | Intelligent | Strategic | Commands lesser AI, adapts tactics |

## Villain Creator

### Customization Options
- **Archetype**: Select from the 6 base types
- **Appearance**: Character model selection, color palette, accessories
- **Nickname**: Filtered fictional name (max 20 chars, profanity filtered)
- **Personality Traits**: 3 selectable traits affecting behavior weighting
- **Difficulty**: Easy / Normal / Hard / Nightmare
- **Special Abilities**: 1-2 from archetype-specific ability pool
- **Backstory**: Optional short text (max 200 chars)

### AI Behavior Configuration
Each archetype has base weights for:
- Aggressiveness (0.0-1.0)
- Stealthiness (0.0-1.0)
- Intelligence (0.0-1.0)
- Patrol radius
- Detection sensitivity
- Combat engagement range

Difficulty modifiers scale: health, damage, detection range, reaction speed.

## Maps

### Map 1: Abandoned Office Complex (Milestone 2)
- **Size**: ~100m x 80m playable area
- **Floors**: 2 (ground + upper)
- **Rooms**: Reception, open office (x2), meeting rooms (x3), hallways, stairwells (x2), server room, storage, security office, break room, executive office
- **Features**: Hiding spots (under desks, closets), loot containers (filing cabinets, safes), locked doors (key items), security cameras (environmental hazard)
- **Lighting**: Flickering fluorescents, dark storage areas, emergency lighting
- **Extraction**: Loading dock (rear of building)
- **Patrol routes**: 3-4 predefined routes covering major areas

### Map 2: Industrial District (Milestone 9)
### Map 3: Abandoned Urban District (Milestone 9+)
### Map 4: Overgrown Forest (Milestone 9+)

## Combat

### Melee
- Primary attack: punch/melee weapon swing
- Combo: up to 3-hit chain
- Damage based on weapon + player level
- Stamina cost per swing
- Cooldown between combos

### Health & Damage
- Base health: 100 HP
- Damage types: Physical, Environmental
- Healing: consumable items (medkits, bandages)
- No health regeneration (survival game feel)
- Death: drop match-collected loot, spectate or respawn (mode-dependent)

### Stamina
- Base stamina: 100
- Sprint: -15/sec
- Jump: -20 per jump
- Melee attack: -10 per swing
- Regen: +10/sec when idle, +5/sec when walking

## Items & Loot

### Item Categories
| Category | Examples | Stackable |
|----------|---------|-----------|
| Weapons | Pipe, Bat, Wrench, Knife | No |
| Armor | Hard Hat, Vest, Boots | No |
| Consumables | Medkit, Bandage, Energy Drink | Yes (x5) |
| Utility | Flashlight, Lockpick, Smoke Bomb | Yes (x3) |
| Key Items | Keycard, Safe Code, USB Drive | No |
| Loot | Documents, Cash, Electronics | Yes (x10) |

### Inventory
- 12 slots (expandable via equipment)
- Equipment slots: Weapon, Head, Body, Utility x2
- Quick-use bar: 3 slots for consumables/utilities

## Progression

### XP Sources
| Action | XP |
|--------|-----|
| Complete objective | 100 |
| Eliminate villain | 75 |
| Successful extraction | 150 |
| Survive 5 minutes | 25 |
| Collect loot item | 10 |
| Team revive | 50 |

### Level Rewards
- Cosmetic unlocks every 5 levels
- New villain archetypes at levels 10, 20, 30
- Equipment slots unlock at levels 5, 15
- New ability slots at levels 10, 25

## Monetization (Future, Optional)
- Cosmetic-only purchases (skins, emotes, villain appearances)
- Battle pass (seasonal cosmetics)
- No pay-to-win mechanics
- No loot boxes
- All gameplay content earnable through play
