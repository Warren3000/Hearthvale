# Hearthvale Project Plan

---

## 📅 Timeline

| Milestone                          | Est. Duration | Start Date  | Est. End Date | Current End Date |
|-------------------------------------|--------------|-------------|---------------|------------------|
| Combat System v1                    | 6–7 weeks    | 07/15/2025  | 09/02/2025    | 08/12/2025       |
| Weapon Leveling v1                  | 4 weeks      | 09/03/2025  | 09/30/2025    | 09/09/2025       |
| Dungeon Interactivity v1            | 9–10 weeks   | 10/01/2025  | 12/09/2025    | 11/04/2025       |
| Combat System v2                    | 4–5 weeks    | 12/10/2025  | 01/13/2026    | 12/09/2025       |
| Player Inventory & Stats Screen     | 2–3 weeks    | 01/14/2026  | 01/31/2026    | 12/30/2025       |
| Bosses & Advanced AI                | 3 weeks      | 02/01/2026  | 02/21/2026    | 01/20/2026       |
| City Building Prototype             | 6–7 weeks    | 02/22/2026  | 04/11/2026    | 03/10/2026       |
| Time/Story Progression v1           | 6 weeks      | 04/12/2026  | 05/23/2026    | 04/21/2026       |
| Global Game State & Save/Load       | 3 weeks      | 05/24/2026  | 06/13/2026    | 05/12/2026       |
| Dungeon Secrets & Lore              | 3 weeks      | 06/14/2026  | 07/04/2026    | 06/02/2026       |
| Town Events & NPCs                  | 3 weeks      | 07/05/2026  | 07/25/2026    | 06/23/2026       |
| Accessibility & Polish              | 3 weeks      | 07/26/2026  | 08/15/2026    | 07/14/2026       |
| Tutorial & Onboarding               | 1–2 weeks    | 08/16/2026  | 08/29/2026    | 07/28/2026       |
| Quest System & Journal              | 2–3 weeks    | 08/30/2026  | 09/19/2026    | 08/18/2026       |
| Map & Fast Travel                   | 2 weeks      | 09/20/2026  | 10/03/2026    | 09/01/2026       |
| Achievements & Progress Tracking    | 1–2 weeks    | 10/04/2026  | 10/17/2026    | 09/15/2026       |
| Endgame & Replayability             | 2–3 weeks    | 10/18/2026  | 11/07/2026    | 10/06/2026       |
| Multiplayer/Co-op Mode              | 4–6 weeks    | 11/08/2026  | 12/19/2026    | 11/10/2026       |
| Modding & Extensibility (Optional)  | 2–4 weeks    | 12/20/2026  | 01/16/2027    | 12/01/2026       |
| Live Events & Seasonal Content      | 2–3 weeks    | 01/17/2027  | 02/06/2027    | 12/22/2026       |
| Mini-Games & Side Activities        | 2–3 weeks    | 02/07/2027  | 02/27/2027    | 01/12/2027       |
| Relationship & Reputation System    | 2–3 weeks    | 02/28/2027  | 03/20/2027    | 02/02/2027       |
| Economy & Trading System            | 2–3 weeks    | 03/21/2027  | 04/10/2027    | 02/23/2027       |
| Procedural Generation               | 3–4 weeks    | 04/11/2027  | 05/08/2027    | 03/23/2027       |
| Expanded Lore & Storytelling        | 2–3 weeks    | 05/09/2027  | 05/29/2027    | 04/13/2027       |
| Endgame Raids & Challenges          | 2–3 weeks    | 05/30/2027  | 06/19/2027    | 05/04/2027       |
| Analytics & Telemetry (Optional)    | 1 week       | 06/20/2027  | 06/26/2027    | 05/11/2027       |
| Launch & Marketing                  | 3 weeks      | 06/27/2027  | 07/17/2027    | 06/01/2027       |
| Alpha Release                       | 1 week       | 07/18/2027  | 07/24/2027    | 06/08/2027       |
| Beta Release                        | 3 weeks      | 07/25/2027  | 08/14/2027    | 06/29/2027       |

---

## 🗂 Feature Areas

<details>
<summary>Click to expand</summary>

- [x] **Combat System**
    - Basic attacks/defeat; missing feedback, sound, and reactions.
    - Player/NPC health implemented. Player health bar added.
    - **Ranged attack mechanics added (projectile/arrow support)**
    - **Combat logging for debugging (damage, hit, defeat events)**
    - **NPC health values per type (e.g., knight: 20 HP, fatnun: 10 HP)**
    - **Sound effects for attacks, hits, and defeat implemented**
- [x] **Weapon System**
    - **Animated projectile support (arrow-atlas.xml)**
    - **Weapon can fire animated projectiles using atlas animation**
    - **Weapon constructor updated to accept projectile atlas**
- [ ] **Weapon Leveling** — Not started
- [ ] **Dungeon Interactivity** — Not started
- [ ] **City Building** — Not started
- [ ] **Time/Story** — Not started
- [ ] **Global Game State & Save** — Unified save/load for player, town, quests, etc.
- [ ] **Bosses & Advanced AI** — Unique boss fights and AI behaviors
- [ ] **Dungeon Secrets & Lore** — Secret rooms, collectibles, lore
- [ ] **Town Events & NPCs** — Town festivals, NPC recruitment
- [ ] **Accessibility & Polish** — UX, controller support, save/load, etc.
- [ ] **Launch & Marketing** — Demo, trailer, Steam integration, etc.
- [ ] **Tutorial & Onboarding** — Guided intro, tooltips, onboarding
- [ ] **Quest System & Journal** — Quest tracking, journal/log
- [ ] **Map & Fast Travel** — World map, fast travel, markers
- [ ] **Achievements & Progress Tracking** — In-game achievements, progress UI
- [ ] **Endgame & Replayability** — New Game+, challenge content
- [ ] **Modding & Extensibility** — Mod support, documentation
- [ ] **Analytics & Telemetry** — Player analytics, error reporting

</details>

---

## 🏗 Core Gameplay & Systems

### Combat System v1 (6–7 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [x] Implement player and NPC health/damage systems
- [x] Add health bar UI for player
- [x] Add health bar UI for NPCs
- [x] Basic attack mechanics (melee)
- [x] Basic enemy defeat logic
- [x] Damage feedback: smooth floating combat text and damage numbers
- [x] **Ranged attack mechanics (projectile/arrow firing)**
- [x] **Animated projectile support (arrow-atlas.xml)**
- [x] **Combat logging for debugging (damage, hit, defeat events)**
- [x] **NPC health values per type (e.g., knight: 20 HP, fatnun: 10 HP)**
- [x] **Sound effects for attacks, hits, and defeat**
- [x] Add reaction animations (hit, defeat, etc.)
- [x] Polish defeat logic (e.g., removal timing, effects)
- [x] Playtesting and bug fixing
**Remaining:**

</details>

---

### Dungeon Interactivity v1 (9–10 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Implement interactive dungeon elements
    - [ ] Doors (locked/unlocked, keys, pressure plates)
    - [ ] Switches (toggleable, timed, sequence-based)
    - [ ] Levers and pressure plates (single-use, reusable, combo logic)
    - [ ] Trap mechanisms (spikes, arrows, pitfalls, poison gas, reset/disable states)
    - [ ] Moving platforms and environmental hazards
    - [ ] Secret passages and hidden rooms
- [ ] Basic puzzle mechanics
    - [ ] Block-pushing puzzles
    - [ ] Light/mirror reflection puzzles
    - [ ] Multi-step switch or sequence puzzles
    - [ ] Riddle/text-based puzzles
    - [ ] Puzzle feedback (audio, UI, animation cues)
- [ ] Dungeon loot system
    - [ ] Randomized loot tables for chests
    - [ ] Key item placement and quest items
    - [ ] Hidden loot and secret rewards
    - [ ] Trapped chests and mimics
    - [ ] Loot UI/notification system
- [ ] Enemy encounters and spawners
    - [ ] Fixed enemy placements
    - [ ] Random spawner logic
    - [ ] Triggered encounters (by room, puzzle, or event)
    - [ ] Boss/mini-boss room triggers
    - [ ] Enemy waves and reinforcements
    - [ ] Encounter completion logic (unlock doors, reward drops)
- [ ] Dungeon state persistence (save/restore opened doors, solved puzzles, cleared encounters)
- [ ] Playtesting and polish for all dungeon mechanics

</details>

---

### Combat System v2 (4–5 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Add advanced combat moves/combos  
    - [ ] Implement multi-step attack combos for player weapons  
    - [ ] Add combo meter/UI indicators  
    - [ ] Enemy reactions to combos (interrupt, stagger, escape)  
    - [ ] Special effects for successful combos  
- [ ] Special weapon abilities  
    - [ ] Unique charged attacks or weapon skills  
    - [ ] Cooldown system for weapon abilities  
    - [ ] Visual/audio feedback for ability use  
    - [ ] Ability upgrades or branching choices  
- [ ] Status effects (poison, stun, etc.)  
    - [ ] Apply and display status effects on player/NPC (icons, timers)  
    - [ ] Implement core effects: poison, burn, freeze, stun, slow, bleed  
    - [ ] Visual and audio cues for each effect  
    - [ ] System for effect resistance, cure, and stacking  
- [ ] Enemy variety improvements  
    - [ ] New enemy types with unique attack patterns  
    - [ ] Enemy resistances and vulnerabilities  
    - [ ] Mini-bosses with special moves  
    - [ ] Improved AI: dodging, flanking, retreat, group tactics  
    - [ ] Enemy reactions to player special moves/combos  
- [ ] Combat log improvements and analytics  
    - [ ] Log advanced combat events (combos, status effects, ability use)  
    - [ ] Analytics for combat performance and challenge balancing  
- [ ] Playtesting and polish for new combat features

---

### Player Inventory & Stats Screen (2–3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design inventory and stats UI
- [ ] Implement inventory management logic
- [ ] Integrate with player data and save/load
- [ ] Add input handling to open/close inventory screen
- [ ] Playtesting and polish

</details>

---

### Bosses & Advanced AI (3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Implement at least 2 unique boss fights (multi-phase)
- [ ] Advanced enemy AI behaviors (evasion, combos, spells)
- [ ] Visual/sound polish for boss attacks/cues
- [ ] Boss health bars, intro animations

</details>

---

## 🌱 Progression & World Building

### City Building Prototype (6–7 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Placeable building system in town
- [ ] Resource collection mechanics
- [ ] Save/load town state
- [ ] Basic town upgrade logic

</details>

---

### Time/Story Progression v1 (6 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Story events and progression triggers
- [ ] Quest system (main and side quests)
- [ ] Time-of-day system
- [ ] Event scheduling

</details>

---

### Global Game State & Save/Load (3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design a global game state structure:
    - Player stats, inventory, equipment
    - Weapon state
    - Town/building state
    - Quest and story flags
    - NPC states and relationships
    - Dungeon progress, secrets found
    - Time-of-day, world events
- [ ] Implement serialization/deserialization (JSON or binary)
- [ ] Integrate save/load logic for all major systems
- [ ] Add UI for save/load slots and autosave
- [ ] Error handling for corrupted/missing saves
- [ ] Playtesting and validation for edge cases

</details>

---

### Dungeon Secrets & Lore (3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Secret rooms, hidden puzzles, rare collectibles
- [ ] Lore pickups (diaries, relics, ancient books)
- [ ] UI for discovered secrets/collectibles

</details>

---

### Town Events & NPCs (3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Town events (festivals, merchant visits, attacks)
- [ ] Recruitable NPCs (specialists, shopkeepers, quest givers)
- [ ] NPC schedule system
- [ ] Town reputation/affinity mechanics

</details>

---

## 🎨 Polish & Accessibility

### Accessibility & Polish (3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Custom keybindings/controller support
- [ ] Save/load UI polish (linked to global system)
- [ ] Difficulty settings and assist modes
- [ ] UI/sound/menu polish

</details>

---

### Tutorial & Onboarding (1–2 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design and implement tutorial sequence
- [ ] Add contextual tooltips/help screens
- [ ] Onboarding for controls, combat, inventory, progression

</details>

---

## 🔄 Retention & Replayability

### Quest System & Journal (2–3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Quest tracking UI
- [ ] Journal/log for story, lore, player notes
- [ ] Integrate quest triggers and rewards

</details>

---

### Map & Fast Travel (2 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] World map UI
- [ ] Fast travel system
- [ ] Map markers for POIs, quests, NPCs

</details>

---

### Achievements & Progress Tracking (1–2 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] In-game achievement system
- [ ] Progress tracking for collectibles, secrets, quests, bosses
- [ ] UI for achievements and progress

</details>

---

### Endgame & Replayability (2–3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] New Game+ mode
- [ ] Challenge dungeons/bosses
- [ ] Randomized events/challenges

</details>

---

## 🌐 Community & Advanced Systems

### Multiplayer/Co-op Mode (4–6 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design multiplayer architecture (online/local, host/join, sync)
- [ ] Implement player connection and session management
- [ ] Add co-op gameplay features (shared progression, trading, chat)
- [ ] Integrate multiplayer with combat, dungeons, and town building
- [ ] Add UI for multiplayer (lobby, invite, status)
- [ ] Playtesting, bug fixing, and network optimization

</details>

---

### Modding & Extensibility (Optional, 2–4 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Mod support (custom maps, NPCs, items)
- [ ] Documentation for modders
- [ ] In-game mod browser/loader

</details>

---

### Live Events & Seasonal Content (2–3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design time-limited quests and world changes
- [ ] Implement special bosses and community challenges
- [ ] Add seasonal events (festivals, holidays, weather)
- [ ] Integrate live events with quest and reward systems
- [ ] Add UI for event notifications and participation
- [ ] Playtesting and polish

</details>

---

### Mini-Games & Side Activities (2–3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design and implement mini-games (card games, fishing, puzzles, arena battles)
- [ ] Add leaderboards and rewards for mini-games
- [ ] Integrate mini-games with town and NPCs
- [ ] Add UI for mini-game access and progress
- [ ] Playtesting and polish

</details>

---

### Relationship & Reputation System (2–3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design NPC relationship mechanics (friendship, romance, rivalry)
- [ ] Implement reputation system (town, factions, global)
- [ ] Integrate relationship/reputation with quest outcomes and prices
- [ ] Add UI for relationship/reputation status
- [ ] Add dialogue and event triggers based on relationship/reputation
- [ ] Playtesting and polish

</details>

---

### Economy & Trading System (2–3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design dynamic market prices and trading mechanics
- [ ] Implement player-run shops and auctions
- [ ] Add bartering and trade caravans
- [ ] Integrate economy with crafting, loot, and NPCs
- [ ] Add UI for trading, shop management, and market status
- [ ] Playtesting and polish

</details>

---

### Procedural Generation (3–4 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design procedural generation algorithms for dungeons and/or overworld
- [ ] Implement random dungeon layouts, loot, and enemy placement
- [ ] Add seed-based world sharing
- [ ] Integrate procedural content with quest and event systems
- [ ] Playtesting and polish

</details>

---

### Expanded Lore & Storytelling (2–3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design branching storylines and multiple endings
- [ ] Add in-game books, diaries, and lore pickups
- [ ] Implement cutscenes and/or voice acting
- [ ] Integrate lore with quests, NPCs, and world events
- [ ] Add UI for lore and story tracking
- [ ] Playtesting and polish

</details>

---

### Endgame Raids & Challenges (2–3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Design large-scale boss fights and timed dungeons
- [ ] Implement leaderboard competitions and challenge modes
- [ ] Add unique loot and cosmetic rewards for endgame content
- [ ] Integrate raids/challenges with multiplayer and progression
- [ ] Add UI for raid/challenge access and tracking
- [ ] Playtesting and polish

</details>

---

### Analytics & Telemetry (Optional, 1 week)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Basic analytics for progression, deaths, quest completion
- [ ] Error/crash reporting integration

</details>

---

## 🚀 Launch & Post-Release

### Launch & Marketing (3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Build and distribute demo
- [ ] Prepare trailer and promotional materials
- [ ] Achievements/trophies (Steam, PC)
- [ ] Steamworks integration (if applicable)
- [ ] Localization (if feasible)
- [ ] Launch and post-release support

</details>

---

### Alpha Release (1 week)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Internal testing and feedback
- [ ] Collect bug reports and iterate

</details>

---

### Beta Release (3 weeks)
<details>
<summary>Details</summary>

**Completed:**
- [ ] (none yet)

**Remaining:**
- [ ] Public playtesting
- [ ] Final bug fixes and polish
- [ ] Prepare for launch

</details>

---

## 💡 Additional Recommendations

- Playtest after major milestones and before launch
- Regular code refactoring (20 min/day) included in schedule
- Document all features and systems for future support and updates
- Adjust milestone durations as you progress and gain confidence