# Past Player Characters as Mobs — Design Doc (v1)

A mod for *Everything is Crab* that snapshots your build every 5 levels (starting at level 20) and, in subsequent runs, occasionally spawns past-self ghosts as Alpha-tier mobs roaming the world.

- **Mod name:** Past Player Characters as Mobs
- **Plugin slug:** `PastPlayerCharactersAsMobs`
- **GUID:** `com.bungus.everythingiscrab.pastplayercharactersasmobs`

---

## Configuration (locked-in from product Q&A)

| Param | Value | Notes |
|---|---|---|
| Snapshot cadence | every **5** levels | starting at level 20 (so 20, 25, 30, 35, …) |
| Retention | up to **200 per level bucket** | 200 at lv20, 200 at lv25, etc. |
| Eviction | when bucket is full, pick a random snapshot from the **oldest 50%** of that bucket and delete | random-from-old keeps variety while biasing toward newer |
| Save trigger | end of run | on win: all in-memory snapshots from the run committed; on loss/quit: each snapshot rolled independently at 25% |
| Spawn type | Alpha-rank, Shiny-state mob | not a boss — wanders the world as a high-visibility encounter |
| Spawn rate | **0.5% per enemy spawn**, gated | gated on (a) player level within ±2 of a snapshot's level, (b) at most 1 past-self alive at a time. Tunable in config. |
| Persistence | JSON file in BepInEx config dir | `BepInEx/config/com.bungus.everythingiscrab.pastplayercharactersasmobs/snapshots.json` |

---

## Recon summary (the hooks we have)

### Snapshot side

| Need | Hook |
|---|---|
| Capture stats | `World.Stats.PlayerStats.Instance.Clone()` (game-provided clone method) |
| Get current level | `EvolutionHandler.EvolutionLevel` (int property) |
| Get chosen evolutions + their levels | `EvolutionHandler._abilitiesCache`, `_abilityLevels`, `_abilityRarityMappings` |
| Detect level-up | Postfix patch on `EvolutionHandler.IncreasePlayerEvolutionLevel()` |
| Get genetic | `RunData.Genetic_*` |
| Get difficulty | `RunData.Difficulty` |
| Specialisations / Affinities | Storage TBD — defer to Phase 4 |

### Run lifecycle

- `RunData.MarkRunStarted` — postfix → clear in-memory snapshot buffer for this run, generate fresh `RunId`
- `RunData.MarkRunEnded(ERunEndReason reason)` — postfix → commit buffer to disk with win/loss rules

### Spawn side

- `EnemyDirector.CreateEnemy(EnemySpawnInfo info, bool)` — prefix-patch. `EnemySpawnInfo = { Archetype, GridCellId, Rank, ShinyState }`. We override Rank=Alpha, ShinyState=Shiny, then post-spawn attach our ghost data.
- `EnemyLoadout._alphaShinyEnemy` — the prefab we'll inherit visually.
- `WorldStats.EnemyAlphasAppearingMultiplier`, `_chanceToSpawnShinyEnemy` — we leave these alone; our roll is independent.
- `CHEAT_SetNormalizedProgressTime(float)` — built-in cheat to fast-forward a run; great for testing.

---

## Snapshot data model

```csharp
public class BuildSnapshot {
	public int schemaVersion = 1;
	public string Id;                                // GUID
	public string RunId;                             // links to the run that produced it
	public DateTime CapturedAt;
	public int EvolutionLevel;                       // the level snapshot bucket
	public int Difficulty;
	public string Genetic;
	public Dictionary<string, float> Stats;          // flat PlayerStats values (whitelist-filtered)
	public Dictionary<string, int> AbilityLevels;    // ability name -> level
	// future: Specialisations, Affinities, Cosmetics
}

public class SnapshotStore {
	const int PerBucketCap = 200;
	const double OldestHalfFraction = 0.5;

	// snapshots.json on disk = { buckets: { "20": [...], "25": [...] } }
	public void Add(BuildSnapshot s);   // appends; evicts random-from-oldest-50% if bucket >= 200
	public IReadOnlyList<BuildSnapshot> ForLevel(int level, int tolerance);
}
```

**On the eviction:** when bucket has 200 entries and a new one comes in:
1. Sort by `CapturedAt` ascending
2. Take the first 100 (oldest half)
3. Pick one uniformly at random, delete
4. Append the new one

---

## Phased plan

### Phase 1 — Save half (no spawning yet) — v0.1.0
1. New repo `EverythingIsCrab-PastPlayerCharactersAsMobs`, scaffolded from the Skip Intro template (Directory.Build.props, Packaging/, publish.ps1 reused).
2. `BuildSnapshot` + `SnapshotStore` with JSON persistence and the eviction rule.
3. Postfix on `EvolutionHandler.IncreasePlayerEvolutionLevel` → if level ≥ 20 and `(level - 20) % 5 == 0`, capture and buffer in memory.
4. Postfix on `RunData.MarkRunStarted` → reset buffer + generate RunId.
5. Postfix on `RunData.MarkRunEnded(reason)` → commit buffer per win/loss rule.
6. **Done = play a run, level past 20, win it, see snapshots in the JSON file. Play another run, die at level 23, see ~25% of snapshots committed.**

### Phase 2 — Spawn a placeholder ghost — v0.2.0
1. Prefix-patch `EnemyDirector.CreateEnemy(EnemySpawnInfo, bool)`.
2. Roll 0.5% gated on level proximity + alive-count cap.
3. On hit: force `info.Rank = Alpha; info.ShinyState = Shiny;`, let original CreateEnemy run, attach a marker MonoBehaviour to the result.
4. Override the spawned mob's display name to "Past Self — Lv N".
5. No stat application yet — we just want a visually distinct alpha shiny showing up at the right level threshold.

### Phase 3 — Apply snapshot to ghost — v0.3.0
1. After spawn, apply a whitelist of stats from the snapshot to the mob's `IStatCharacter`.
2. Skip stats that don't make sense on an enemy (`RerollEvolutionDiscount`, `NumberOfBranchingEvolutionChoices`, etc.).
3. Scale HP/damage so ghosts feel like real fights, not pushovers or insta-kills.
4. Tune via config (multipliers per stat category).

### Phase 4 — Polish — v0.4.0+
- Add specialisations / affinities / cosmetics to snapshots once we find their storage
- Per-snapshot biome/region preference (spawn the swamp-build ghost in swamp regions)
- Custom name plate, optional UI prefix ("👻 Past Self")
- BepInEx config entries for cadence, retention, spawn rate, level gating tolerance

---

## Open risks

1. **`IncreasePlayerEvolutionLevel` might not catch all level-ups.** Some flows might call the `EvolutionLevel` setter directly. Belt-and-suspenders: also postfix the setter, dedupe by snapshot ID.
2. **Stat application complexity.** Lots of `PlayerStats` fields don't map to enemies. Phase 3 will involve a stat-by-stat audit.
3. **Per-mob attached data.** Attaching our `PastSelfMarker` MonoBehaviour to an IL2CPP-spawned enemy requires `ClassInjector.RegisterTypeInIl2Cpp<T>()` (Il2CppInterop API). Slight risk it interferes with the game's own component management.
4. **EnemyLoadout per-archetype.** The visual baseline (`_alphaShinyEnemy`) is per archetype. If we let the game pick the archetype and we just override Rank/Shiny, the visual is correct. If we want a *consistent* "ghost crab" look, we'd need to override `Archetype` too — but then it might not fit the spawn region. Recommendation: ride whatever archetype the game picked.
5. **Save corruption resistance.** JSON file is brittle to bad writes. Phase 1 should write to a `.tmp` file then atomic-rename.
6. **Snapshot bloat.** 200 per bucket × N buckets × ~5KB each ≈ low MB. Fine. But evicted snapshots are gone forever — no recycle bin. Acceptable.

---

## Open product questions (round 2)

These don't block Phase 1 but worth thinking about:

- **What gets killed when the player kills a past-self?** Currency drop? Snapshot deleted from the library? Achievement unlock? Or nothing for now?
- **Does the player ever see *which* past run a ghost came from?** Could be cool — "Past Self from Run #42 (Genetic: Shark)".
- **Past selves vs. each other.** If two ghosts could spawn at once (we cap at 1 for now), would they fight each other or just both target the player? (Game's `IAttackableEntity` interface might give us "both target player" for free.)

---

## Q&A round 2 — answers

**Reward for killing a past-self:** Food drops + evolution points. Mechanically, evolution points in this game come from *eating food*, not from kills directly (`FoodData.BaseEvolutionPointsGivenWithEachBite`). Food drops are defined by per-enemy `FoodDropChancesData` ScriptableObjects with weighted distributions (`_howMuchFoodToDropWeights`, `_foodRarityDropMappings`, etc.). Since we're spawning as Alpha+Shiny, **the past-self automatically inherits the alpha-tier drop table** — better food, more food, rarer rolls. No custom drop code needed in Phase 2. If alpha drops don't feel rewarding enough in playtesting, Phase 3+ can override `FoodDropChancesData` on the spawned ghost.

**Naming:** Default name format will be detailed — e.g. `"Past Self — Lv 25 (Run #42, Shark)"` — pulled from snapshot metadata. Config option for "random person name from a big list" added to Phase 4 polish. Doesn't affect snapshot schema, which already captures `RunId`, `EvolutionLevel`, and `Genetic`.
