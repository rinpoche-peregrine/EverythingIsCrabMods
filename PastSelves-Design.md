# Past Selves — Design Doc (v0)

A *Ghosts of Past Runs* mod for *Everything is Crab*: every N levels (after level 20) the game saves a snapshot of your current build. On subsequent runs there's a chance one of those past-self snapshots spawns as a miniboss-tier enemy when you reach the same level.

Working name: **Past Selves**. Open to renaming (alternates: *Echoes*, *Carcinisation Ghosts*, *Past Lives*).

---

## Recon findings

### Player class

- **`World.Characters.PlayerCharacter`** is the player MonoBehaviour.
- Singleton: `PlayerCharacter.Instance` (via `_Instance_k__BackingField`).
- Implements `IAttackableEntity` and `IStatCharacter` — these are the interfaces enemies attack/interact with the player through. **Important:** the player and enemies share these interfaces, which means an enemy can in principle be wired to use player-shaped data without changing the attack/damage subsystems.
- Key sub-objects on the player:
	- `_evolutionHandler` — chosen evolutions and ability levels
	- `_playerStats` — back-reference to `PlayerStats.Instance`
	- `_movementController`, `_rigidbody` — physics
	- `_playerCharmController` — charm/dominate system (might be useful — see "spawning ghosts" below)
	- `_visualController` — visuals
	- Bunch of audio state objects (Wwise RTPCs) we don't need to touch

### Build composition (what makes up a "snapshot")

Build state is distributed across several singletons, not centralized:

| Layer | Class | Holds |
|---|---|---|
| Stats | `World.Stats.PlayerStats` (Instance) | All numeric stat modifiers — `DodgeChance`, `DamageReductionPercentage`, `EffectAreaSize`, food multipliers, etc. **Has a public `Clone()` method.** |
| Evolutions | `EvolutionHandler` (on PlayerCharacter) | `_abilitiesCache`, `_abilityLevels`, `_abilityRarityMappings` — i.e. which evolutions you've picked and their levels |
| Level | `EvolutionHandler._EvolutionLevel_k__BackingField` (int) | The player's current "level" (EvolutionLevel) |
| Genetic | `GameFlow.RunData.Genetic_*` + `GeneticsRuntimeManager` | The chosen genetic for this run |
| Difficulty | `RunData.Difficulty` | Run difficulty setting |
| Specialisations | `CharacterScreenUI`/`AffinityInfoUI` reference them; backing storage TBD | Need to dig — likely on PlayerCharacter or RunData |
| Affinities | `AffinityInfoUI` references them; same situation | TBD |

**For a v1 snapshot we can probably ship with: `PlayerStats.Clone()` + `EvolutionLevel` + chosen evolutions + genetic + difficulty.** Specialisations and affinities can be Phase-2 additions once we've located the storage.

### Level-up hook

- **`EvolutionHandler.IncreasePlayerEvolutionLevel()`** is the public method that runs on level-up. Postfix here = "snapshot on level up". Single method, no params, easy.
- `EvolutionLevel` has both private and public setters depending on the type, but `IncreasePlayerEvolutionLevel` is the natural-language entry point.

### Run lifecycle

- **`MarkNewRunStarted` / `MarkRunStarted` / `MarkRunEnded`** — all public static methods on a run-tracking class (likely `RunData` or `RunIDTracker`). Easy patch targets for run start/end.
- `_timeRunStarted` field — run timestamp.
- `ERunEndReason` enum exists — death/quit/win/etc. — useful so we can avoid snapshotting on a quit.

### Enemy + boss system (our spawn injection point)

- **`World.Characters.Enemies.EnemyDirector`** runs the enemy/boss spawning.
- Two `CreateEnemy` overloads, both public:
	- `CreateEnemy(EnemySpawnInfo, bool)`
	- `CreateEnemy(GridCellId, EEnemyArchetype, EEnemyRank, EEnemyShinyState, bool)`
- **`DetermineWhichBossWillSpawn(IReadOnlyList<EBoss>) → EBoss`** — public method that picks the boss to spawn from an allowed list. We could prefix-patch this to substitute our ghost when one is queued up.
- **`IsAllowedToSpawnBoss(EBoss) → bool`** — public static, controls whether a boss is in the eligible pool.
- `BanBossByItsGroup(EBoss)` / `ClearBannedBossesOnEndless` — there's already infrastructure for banning bosses per run, so adding/removing our ghost from the pool is a natural extension.
- `_chanceToSpawnBoss`, `_HasCurrentMinibossOverrideState_k__BackingField` — there's an existing "miniboss override" concept; this is likely the cleanest hook for a special one-off enemy.
- **`CHEAT_SetNormalizedProgressTime(float)`** — built-in cheat to fast-forward the run timer. **Huge** for testing.

### Save data infrastructure

- Game has its own `SaveSystem.SaveManager` and a family of `*SaveDataSnapshot` classes following an `ASaveDataSnapshot` base. Snapshots are part of the game's persistent save.
- **Decision: we won't plug into the game's save system for v1.** Too much coupling risk. Instead, store snapshots as JSON in `BepInEx/config/com.bungus.everythingiscrab.pastselves/snapshots.json` — easy to inspect, easy to wipe, no save-file corruption risk.

---

## Snapshot data model (v1)

```csharp
// Persisted as JSON in BepInEx/config/com.bungus.everythingiscrab.pastselves/snapshots.json
public class BuildSnapshot {
	public string Id;              // GUID
	public string RunId;           // links to which run produced it
	public DateTime CapturedAt;    // wall-clock timestamp
	public int EvolutionLevel;     // the player level when snapshotted
	public int Difficulty;         // EDifficulty as int
	public string Genetic;         // EGenetic as string
	public Dictionary<string, float> Stats;       // flattened PlayerStats.Instance values
	public Dictionary<string, int> EvolutionLevels; // ability name -> level
	// Future: Specialisations, Affinities, Cosmetics
}
```

A single file, list of snapshots, append-only. Old snapshots stay forever (or we add a config-driven cap later).

---

## Hook points

| Need | Class | Method | Patch type |
|---|---|---|---|
| Snapshot on level-up | `EvolutionHandler` | `IncreasePlayerEvolutionLevel` | Postfix |
| Detect run start (for loading + tracking RunId) | `RunData` (or `RunIDTracker`) | `MarkRunStarted` | Postfix |
| Detect run end (for cleanup) | `RunData` | `MarkRunEnded` | Postfix |
| Inject a ghost as a boss option | `EnemyDirector` | `DetermineWhichBossWillSpawn` | Prefix (intercept and override return) |
| Spawn the actual ghost enemy | `EnemyDirector` | `CreateEnemy` | Called from our `Update`/coroutine, not a patch |
| Apply build to spawned enemy | TBD — on the spawned `IEnemyCharacter` | TBD | Likely a postfix on enemy init |

---

## Phased delivery plan

### Phase 1 — Snapshot save/load (this should be its own release v0.1)

1. New project `PastSelvesPlugin/` in a new repo `EverythingIsCrab-PastSelves`.
2. Define `BuildSnapshot` and a `SnapshotStore` that reads/writes the JSON file.
3. Postfix patch `EvolutionHandler.IncreasePlayerEvolutionLevel` → if new level > 20 and `(level - 20) % 5 == 0`, capture a snapshot, append to the JSON.
4. Log each snapshot to BepInEx log so we can verify.
5. **No spawning yet.** Done = run a game, level past 20, see snapshots accumulating in the JSON file.

### Phase 2 — Spawn a placeholder ghost (v0.2)

1. Prefix-patch `EnemyDirector.DetermineWhichBossWillSpawn` to occasionally inject our ghost.
2. For the actual spawn, start by reusing an existing boss prefab (Crabtaur or SpiderCrabette) with a custom display name like "Past Self — Lv 25".
3. No stat-application yet — just confirm we can make a custom-tagged boss appear at the right level threshold.

### Phase 3 — Apply build to ghost (v0.3)

1. After `CreateEnemy` returns, set the spawned enemy's stats to match the snapshot.
2. Apply scaling so ghosts don't trivially-destroy or get one-shot by the current player.
3. Optional: try to use a player-shaped attack pattern (would need investigation into `PlayerAttackManager`).

### Phase 4 — Polish (v0.4+)

- Specialisations / Affinities / Cosmetics added to snapshot
- Config-driven tuning (N levels between snapshots, spawn chance, min level threshold)
- UI: name plate showing "Past Self" + the build characteristics
- Optional: per-snapshot biome/region preference

---

## Risks / open questions

1. **Spawning a player-as-enemy may not be straightforward.** Player has player-specific subsystems (charm, dash, evolution-driven abilities). The cleanest path is to spawn a *normal enemy* and apply player-derived stats — not actually instantiate a `PlayerCharacter` GameObject as an enemy. Phase 3 will tell us if stat-application is enough or if we need more.
2. **The build is huge and stat application is complex.** `PlayerStats` has 20+ multipliers; applying them to an enemy may have unexpected interactions (e.g., `RerollEvolutionDiscount` makes no sense on an enemy). We'll need to whitelist which stats actually transfer.
3. **`IncreasePlayerEvolutionLevel` may not fire on every level — verify.** Some level-up paths might use `set_EvolutionLevel` directly. Belt-and-suspenders: hook the setter too.
4. **The `_HasCurrentMinibossOverrideState` flag exists.** Worth investigating — there may be a cleaner "spawn override" mechanism than patching `DetermineWhichBossWillSpawn`.
5. **Testing roughly costs a full run.** Mitigate with `CHEAT_SetNormalizedProgressTime` to fast-forward, and seed-pinning if it exists.
6. **Save file format will need migration logic eventually.** Add a `schemaVersion` field to `BuildSnapshot` from day one.

---

## Open product questions for Bungus

- **Mod name** — Past Selves? Echoes? Carcinisation Ghosts? Past Lives?
- **N levels between snapshots** — you said ~5. Confirm? And start-after-level-20 stays?
- **Snapshot retention** — keep all forever, or cap at e.g. 50 most recent?
- **Spawn rate** — when a snapshot-ghost is eligible to spawn, what's the chance per boss-fight? 100% (always replaces a boss)? 25% (occasional treat)?
- **Anti-snapshot conditions** — skip if you died early, or only snapshot wins? Skip if you've never beaten the level?
