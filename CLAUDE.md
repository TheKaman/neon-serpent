# NeonSerpent — Claude Code Project Instructions

## Project Summary
Unity 6 Android Snake game with retro neon visuals. Three game modes: Classic Endless,
Time Attack, and Campaign (4 worlds × 5 levels). Power-ups, Google Play Games leaderboards,
Unity Ads (LevelPlay), and IAP. Target: Google Play Store.

## Tech Stack
- Unity 6 (6000.0.x), Universal Render Pipeline (URP) 2D
- C# .NET Standard 2.1
- Unity Input System (new — NOT the legacy Input class)
- Google Play Games Services (GPGS) Unity Plugin v11+
- Unity LevelPlay (IronSource) SDK for ads
- Unity IAP package (`com.unity.purchasing`)

## Project Identity
- Package name: `com.thekaman.neonserpent`
- GitHub: https://github.com/TheKaman/neon-serpent
- Keystore: `E:\Keys\neonserpent.keystore` — NEVER commit this file
- Build output: `Builds/Android/NeonSerpent.aab` — in .gitignore

## Naming Conventions
- MonoBehaviours: PascalCase + role suffix — `SnakeController`, `FoodSpawner`, `HUD`
- ScriptableObjects: PascalCase + `Data` or `Config` suffix — `LevelData`, `PowerUpConfig`
- Interfaces: `I` prefix — `ILeaderboardService`
- Enums: PascalCase singular — `GameState`, `PowerUpType`, `FoodType`
- C# Events/Actions: `On` prefix — `OnGameOver`, `OnScoreChanged`, `OnMoved`
- Constants: `UPPER_SNAKE_CASE` in `Constants.cs`
- Private fields: `_camelCase` (underscore prefix)
- Properties: `PascalCase`

## Folder Conventions
- All custom code and assets: `Assets/_Project/`
- Third-party SDKs: `Assets/Plugins/`
- ScriptableObject assets: `Assets/_Project/ScriptableObjects/`
- Prefabs mirror the `Scripts/` subfolder structure

## Architecture Rules
1. `GameManager` is the **single source of truth** for `GameState` — nothing else changes game state directly
2. Systems communicate via **C# events** (`Action`/`Func`), NOT direct `GetComponent` calls at runtime
3. No `FindObjectOfType` in production code — use `Singleton<T>` or inject via Inspector `[SerializeField]`
4. `GridSystem` is the **authority on what occupies each cell** — `SnakeController` must call `SetCell()` after every move
5. `SnakeController` owns all game logic — `SnakeVisuals` only reads state and renders, zero game logic in visuals
6. `SaveManager` handles **all persistence** — no raw `PlayerPrefs` except for volume/vibration settings
7. `Bootstrap` scene loads first and houses all `DontDestroyOnLoad` singletons

## When Claude Writes Code
1. Always include `using` directives at the top of every file
2. Add XML `/// <summary>` doc comments on all `public` methods
3. Power-up effects **must** implement both `Apply()` AND `Remove()` — never one without the other
4. All `StartCoroutine` calls must store the `Coroutine` reference and call `StopCoroutine` on cleanup/disable
5. Every ad call must be guarded: `if (!_playerData.isAdFree)`
6. Every ScriptableObject must have `[CreateAssetMenu(fileName = "...", menuName = "NeonSerpent/...")]`
7. Follow the event-driven pattern — raise events, don't reach across systems directly

## Android Build Settings
- Min API: 24 (Android 7.0)
- Target API: 35
- Scripting Backend: IL2CPP
- Architecture: ARM64
- Active Input Handling: Input System Package (New)
- Internet Access: Required

## Git Workflow
- `main` — production releases only, NEVER commit directly
- `develop` — default working branch
- `feature/<scope>-<name>` — branch from develop for each feature

### Commit Format (Conventional Commits)
```
feat(scope): short description
fix(scope): short description
chore(scope): short description
refactor(scope): short description
docs(scope): short description
```
**Scopes:** `snake` | `grid` | `powerup` | `level` | `ui` | `ads` | `leaderboard` | `audio` | `build`

### Never Commit
- `*.keystore` — store at `E:\Keys\` outside the repo
- `Library/`, `Temp/`, `Builds/`
- `*.apk`, `*.aab`
- Merge directly to `main`
