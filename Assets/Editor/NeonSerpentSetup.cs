// ============================================================
// NeonSerpentSetup.cs
// Unity Editor script that auto-builds the entire project:
// sprites, materials, prefabs, Bootstrap scene, Game scene.
//
// HOW TO USE:
//   1. Open Unity and wait for compilation to finish.
//   2. In the top menu go to:  NeonSerpent → Setup Project
//   3. Click OK on the dialog. Done. Press Play.
// ============================================================

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NeonSerpent.Core;
using NeonSerpent.Grid;
using NeonSerpent.Snake;
using NeonSerpent.Food;
using NeonSerpent.PowerUps;
using NeonSerpent.Scoring;
using NeonSerpent.Levels;
using NeonSerpent.Audio;
using NeonSerpent.Ads;
using NeonSerpent.IAP;
using NeonSerpent.SaveData;
using NeonSerpent.Input;
using NeonSerpent.UI;
using NeonSerpent.Leaderboard;
using NeonSerpent.VFX;

public class NeonSerpentSetup : EditorWindow
{
    // ─────────────────────────────────────────────────────────
    // ENTRY POINT
    // ─────────────────────────────────────────────────────────

    [MenuItem("NeonSerpent/Setup Project  ← Run This First!")]
    public static void SetupProject()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Neon Serpent — Project Setup",
            "This will:\n\n" +
            "• Configure Android Player Settings\n" +
            "• Create sprite textures & materials\n" +
            "• Create snake, food & power-up prefabs\n" +
            "• Build all 6 scenes (Bootstrap, Game, MainMenu, Leaderboard, Shop, Settings)\n" +
            "• Add all scenes to Build Settings\n\n" +
            "Any existing scenes with the same names will be overwritten.",
            "Yes, set everything up!", "Cancel");

        if (!confirm) return;

        EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Starting...", 0f);

        try
        {
            ConfigurePlayerSettings();
            EnsureFolder("Assets/_Project/Audio");
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Creating textures...", 0.15f);

            var sprites = CreateSprites();
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Creating materials...", 0.30f);

            var mats = CreateMaterials();
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Creating prefabs...", 0.45f);

            var prefabs = CreatePrefabs(sprites);
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Building Bootstrap scene...", 0.60f);

            CreateBootstrapScene();
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Building Game scene...", 0.75f);

            CreateGameScene(prefabs, mats);
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Building MainMenu scene...", 0.78f);

            CreateMainMenuScene();
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Building Level Select scene...", 0.82f);

            CreateLevelSelectScene();
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Building Leaderboard scene...", 0.84f);

            CreateLeaderboardScene();
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Building Shop scene...", 0.91f);

            CreateShopScene();
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Building Settings scene...", 0.94f);

            CreateSettingsScene();
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Configuring build settings...", 0.97f);

            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog(
            "Setup Complete!",
            "Project is ready.\n\n" +
            "Scenes created: Bootstrap, Game, MainMenu, Leaderboard, Shop, Settings\n\n" +
            "Next steps:\n" +
            "  1. Run NeonSerpent > Generate Campaign Levels\n" +
            "  2. Run NeonSerpent > Fix Android Build Settings\n" +
            "  3. Open Game scene and press Play to test\n\n" +
            "Scene path:\n  Assets/_Project/Scenes/Game.unity",
            "Let's go!");
    }

    // ─────────────────────────────────────────────────────────
    // STEP 1 — PLAYER SETTINGS
    // ─────────────────────────────────────────────────────────

    static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName  = "TheKaman";
        PlayerSettings.productName  = "Neon Serpent";

        PlayerSettings.SetApplicationIdentifier(
            BuildTargetGroup.Android, "com.thekaman.neonserpent");

        PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35;

        PlayerSettings.SetScriptingBackend(
            BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Use new Input System
        PlayerSettings.SetNormalMapEncoding(BuildTargetGroup.Android, NormalMapEncoding.XYZ);

        // Internet access required (for GPGS + ads)
        PlayerSettings.Android.forceInternetPermission = true;

        // Default screen orientation
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        Debug.Log("[Setup] Player Settings configured.");
    }

    // ─────────────────────────────────────────────────────────
    // STEP 2 — SPRITES (programmatically generated 32×32 PNGs)
    // ─────────────────────────────────────────────────────────

    struct SpriteSet
    {
        public Sprite SnakeHead, SnakeBody;
        public Sprite FoodNormal, FoodBonus, FoodPoison;
        public Sprite PowerUpSpeed, PowerUpShield, PowerUpMultiplier;
        public Sprite PowerUpGhost, PowerUpShrink, PowerUpPoison;
    }

    static SpriteSet CreateSprites()
    {
        var set = new SpriteSet
        {
            SnakeHead         = NeonSpriteGenerator.MakeSnakeHead(),
            SnakeBody         = NeonSpriteGenerator.MakeSnakeBody(),
            FoodNormal        = NeonSpriteGenerator.MakeFoodNormal(),
            FoodBonus         = NeonSpriteGenerator.MakeFoodBonus(),
            FoodPoison        = NeonSpriteGenerator.MakeFoodPoison(),
            PowerUpSpeed      = NeonSpriteGenerator.MakePUSpeed(),
            PowerUpShield     = NeonSpriteGenerator.MakePUShield(),
            PowerUpMultiplier = NeonSpriteGenerator.MakePUMultiplier(),
            PowerUpGhost      = NeonSpriteGenerator.MakePUGhost(),
            PowerUpShrink     = NeonSpriteGenerator.MakePUShrink(),
            PowerUpPoison     = NeonSpriteGenerator.MakePUPoison(),
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Setup] Pixel-art sprites generated.");
        return set;
    }

    // ─────────────────────────────────────────────────────────
    // STEP 3 — MATERIALS
    // ─────────────────────────────────────────────────────────

    struct MaterialSet
    {
        public Material GridLines;
    }

    static MaterialSet CreateMaterials()
    {
        EnsureFolder("Assets/_Project/Materials");

        var gridMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
        {
            name  = "NeonGridLines",
            color = new Color(0.08f, 0.22f, 0.10f, 1f)
        };

        string matPath = "Assets/_Project/Materials/NeonGridLines.mat";
        if (File.Exists(Path.GetFullPath(matPath)))
            AssetDatabase.DeleteAsset(matPath);
        AssetDatabase.CreateAsset(gridMat, matPath);

        Debug.Log("[Setup] Materials created.");
        return new MaterialSet { GridLines = gridMat };
    }

    // ─────────────────────────────────────────────────────────
    // STEP 4 — PREFABS
    // ─────────────────────────────────────────────────────────

    struct PrefabSet
    {
        public GameObject SnakeHead, SnakeBody;
        public GameObject FoodNormal, FoodBonus, FoodPoison;
        public GameObject[] PowerUps; // indexed by PowerUpType enum
    }

    static PrefabSet CreatePrefabs(SpriteSet sprites)
    {
        EnsureFolder("Assets/_Project/Prefabs/Snake");
        EnsureFolder("Assets/_Project/Prefabs/Food");
        EnsureFolder("Assets/_Project/Prefabs/PowerUps");

        var set = new PrefabSet();

        // ── Snake ──────────────────────────────────────────
        set.SnakeHead = MakeSpritePrefab("SnakeHead", sprites.SnakeHead,
            "Assets/_Project/Prefabs/Snake/SnakeHead.prefab",
            go => go.AddComponent<SnakeSegment>());

        set.SnakeBody = MakeSpritePrefab("SnakeBody", sprites.SnakeBody,
            "Assets/_Project/Prefabs/Snake/SnakeBody.prefab",
            go => go.AddComponent<SnakeSegment>());

        // ── Food ───────────────────────────────────────────
        set.FoodNormal = MakeSpritePrefab("FoodNormal", sprites.FoodNormal,
            "Assets/_Project/Prefabs/Food/FoodNormal.prefab",
            go => {
                var fi = go.AddComponent<FoodItem>();
                SetField(fi, "_type", (int)FoodType.Normal);
            });

        set.FoodBonus = MakeSpritePrefab("FoodBonus", sprites.FoodBonus,
            "Assets/_Project/Prefabs/Food/FoodBonus.prefab",
            go => {
                var fi = go.AddComponent<FoodItem>();
                SetField(fi, "_type", (int)FoodType.Bonus);
            });

        set.FoodPoison = MakeSpritePrefab("FoodPoison", sprites.FoodPoison,
            "Assets/_Project/Prefabs/Food/FoodPoison.prefab",
            go => {
                var fi = go.AddComponent<FoodItem>();
                SetField(fi, "_type", (int)FoodType.Poison);
            });

        // ── Power-Ups (ordered by PowerUpType enum) ────────
        //  0=SpeedBoost, 1=Shield, 2=ScoreMultiplier, 3=GhostMode, 4=ShrinkPill, 5=Poison
        var puSprites = new[]
        {
            sprites.PowerUpSpeed, sprites.PowerUpShield, sprites.PowerUpMultiplier,
            sprites.PowerUpGhost, sprites.PowerUpShrink, sprites.PowerUpPoison
        };
        var puTypes = new[]
        {
            PowerUpType.SpeedBoost, PowerUpType.Shield, PowerUpType.ScoreMultiplier,
            PowerUpType.GhostMode, PowerUpType.ShrinkPill, PowerUpType.Poison
        };
        var puNames = new[]
        {
            "PU_SpeedBoost","PU_Shield","PU_Multiplier","PU_Ghost","PU_Shrink","PU_Poison"
        };

        set.PowerUps = new GameObject[6];
        for (int i = 0; i < 6; i++)
        {
            int idx = i;
            set.PowerUps[i] = MakeSpritePrefab(puNames[i], puSprites[i],
                $"Assets/_Project/Prefabs/PowerUps/{puNames[i]}.prefab",
                go => {
                    var pu = go.AddComponent<PowerUpItem>();
                    SetField(pu, "_type", (int)puTypes[idx]);
                });
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Setup] Prefabs created.");
        return set;
    }

    static LevelData CreateTimeAttackLevelData()
    {
        // Prefer the handcrafted asset (18×18, wall pillars, 90-second timer)
        const string handcraftedPath = "Assets/Resources/TimeAttack/TimeAttack_Standard.asset";
        var handcrafted = AssetDatabase.LoadAssetAtPath<LevelData>(handcraftedPath);
        if (handcrafted != null)
        {
            Debug.Log("[Setup] Using handcrafted TimeAttack_Standard.asset.");
            return handcrafted;
        }

        // Fall back to programmatically-created asset
        const string path = "Assets/_Project/ScriptableObjects/TimeAttackLevel.asset";
        EnsureFolder("Assets/_Project/ScriptableObjects");

        // Reuse existing asset if already created
        var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<LevelData>();
        data.LevelName      = "Time Attack";
        data.WorldName      = "Time Attack";
        data.Mode           = GameMode.TimeAttack;
        data.GridWidth      = 20;
        data.GridHeight     = 20;
        data.TimeLimit      = 120f;   // 2 minutes
        data.ScoreTarget    = 0;      // no score target — survive until timer ends
        data.InitialSpeed   = 4f;    // matches Constants.DEFAULT_SPEED
        data.SpeedIncrement = 0.25f; // matches Constants.SPEED_INCREMENT
        data.WallPositions  = System.Array.Empty<Vector2Int>();

        AssetDatabase.CreateAsset(data, path);
        Debug.Log("[Setup] TimeAttack LevelData asset created (fallback — handcrafted asset not found).");
        return data;
    }

    static GameObject CreateFloatingTextPrefab()
    {
        const string path = "Assets/_Project/Prefabs/UI/FloatingScoreText.prefab";
        EnsureFolder("Assets/_Project/Prefabs/UI");

        if (File.Exists(Path.GetFullPath(path)))
            AssetDatabase.DeleteAsset(path);

        var go  = new GameObject("FloatingScoreText");
        var tmp = go.AddComponent<TMPro.TextMeshPro>();
        tmp.fontSize       = 2f;
        tmp.alignment      = TMPro.TextAlignmentOptions.Center;
        tmp.color          = Color.white;
        tmp.enableWordWrapping = false;
        go.AddComponent<FloatingScoreText>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[Setup] FloatingScoreText prefab created.");
        return prefab;
    }

    static GameObject MakeSpritePrefab(string goName, Sprite sprite, string path,
        System.Action<GameObject> configure)
    {
        if (File.Exists(Path.GetFullPath(path)))
            AssetDatabase.DeleteAsset(path);

        var go = new GameObject(goName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // Add circle collider for future trigger detection
        go.AddComponent<CircleCollider2D>().isTrigger = true;

        configure?.Invoke(go);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ─────────────────────────────────────────────────────────
    // STEP 5 — BOOTSTRAP SCENE
    // ─────────────────────────────────────────────────────────

    static void CreateBootstrapScene()
    {
        string path = "Assets/_Project/Scenes/Bootstrap.unity";
        EnsureFolder("Assets/_Project/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Managers root ──────────────────────────────────
        var managersGO = new GameObject("[Managers]");

        // GameManager
        var gmGO = CreateChild("[GameManager]", managersGO);
        gmGO.AddComponent<GameManager>();

        // SaveManager
        var smGO = CreateChild("[SaveManager]", managersGO);
        smGO.AddComponent<SaveManager>();

        // AudioManager (needs an AudioSource for music)
        var amGO = CreateChild("[AudioManager]", managersGO);
        var audioMgr = amGO.AddComponent<AudioManager>();
        var musicSrc = amGO.AddComponent<AudioSource>();
        musicSrc.playOnAwake = false;
        musicSrc.loop        = true;
        SetRef(audioMgr, "_musicSource", musicSrc);

        // SoundLibrary — load the asset and wire it to AudioManager.
        // Clip assignment is handled automatically by SoundLibrarySetup after every compile.
        {
            const string slPath = "Assets/_Project/ScriptableObjects/SoundLibrary.asset";
            EnsureFolder("Assets/_Project/ScriptableObjects");
            var soundLib = AssetDatabase.LoadAssetAtPath<ScriptableObject>(slPath);
            if (soundLib == null)
            {
                soundLib = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(soundLib, slPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Setup] SoundLibrary asset created at " + slPath);
            }
            SetRef(audioMgr, "_soundLibrary", soundLib);
        }

        // MusicManager — crossfades between menu and game music tracks.
        // Music clip refs are wired separately via the Inspector or MusicManager's own fields.
        var musicMgrGO  = CreateChild("[MusicManager]", managersGO);
        var musicMgr    = musicMgrGO.AddComponent<MusicManager>();

        // AdManager
        var adGO = CreateChild("[AdManager]", managersGO);
        adGO.AddComponent<AdManager>();

        // IAPManager
        var iapGO = CreateChild("[IAPManager]", managersGO);
        iapGO.AddComponent<IAPManager>();

        // SceneLoader (needs a Canvas + fade overlay)
        var slGO   = CreateChild("[SceneLoader]", managersGO);
        var loader = slGO.AddComponent<SceneLoader>();

        var canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(slGO.transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        ConfigureCanvasScaler(canvasGO.AddComponent<CanvasScaler>());
        canvasGO.AddComponent<GraphicRaycaster>();

        var overlayGO = new GameObject("FadeOverlay");
        overlayGO.transform.SetParent(canvasGO.transform, false);
        var img = overlayGO.AddComponent<Image>();
        img.color = Color.black;
        var rt = overlayGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var cg = overlayGO.AddComponent<CanvasGroup>();
        cg.alpha           = 0f;
        cg.blocksRaycasts  = false;

        SetRef(loader, "_fadeOverlay", cg);

        // LeaderboardServiceProvider — wraps GPGS or local fallback
        var lbGO = CreateChild("[LeaderboardService]", managersGO);
        lbGO.AddComponent<LeaderboardServiceProvider>();

        // ApplicationController (loads MainMenu on start — add last)
        var acGO = CreateChild("[ApplicationController]", managersGO);
        acGO.AddComponent<ApplicationController>();

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[Setup] Bootstrap scene created.");
    }

    // ─────────────────────────────────────────────────────────
    // STEP 6 — GAME SCENE
    // ─────────────────────────────────────────────────────────

    static void CreateGameScene(PrefabSet prefabs, MaterialSet mats)
    {
        string path = "Assets/_Project/Scenes/Game.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ─────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 12f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.02f, 0.03f, 0.05f); // near-black
        cam.transform.position = new Vector3(9.5f, 9.5f, -10f);
        camGO.AddComponent<AudioListener>();
        camGO.AddComponent<CameraShake>();
        // Screen adaptation: CameraFit adjusts orthographic size to the grid at runtime.
        camGO.AddComponent<CameraFit>();

        // URP camera data is auto-added by Unity when URP is active

        // ── EventSystem (required for button clicks) ───────
        var eventSysGO = new GameObject("EventSystem");
        eventSysGO.AddComponent<EventSystem>();
        eventSysGO.AddComponent<InputSystemUIInputModule>();

        // ── Score Manager ──────────────────────────────────
        var scoreMgrGO  = new GameObject("[ScoreManager]");
        var scoreMgr    = scoreMgrGO.AddComponent<ScoreManager>();

        // ── Systems root ───────────────────────────────────
        var systemsGO = new GameObject("[Systems]");

        // GridSystem
        var gridGO  = CreateChild("[GridSystem]", systemsGO);
        var gridSys = gridGO.AddComponent<GridSystem>();

        // NeonGridRenderer
        var gridRenGO  = CreateChild("[GridRenderer]", systemsGO);
        var gridRen    = gridRenGO.AddComponent<NeonGridRenderer>();
        var gridMeshR  = gridRenGO.GetComponent<MeshRenderer>();
        gridMeshR.sharedMaterial = mats.GridLines;
        gridMeshR.sortingOrder   = -10;
        SetRef(gridRen, "_grid", gridSys);

        // FoodSpawner
        var foodGO  = CreateChild("[FoodSpawner]", systemsGO);
        var foodSp  = foodGO.AddComponent<FoodSpawner>();
        SetRef(foodSp, "_grid",         gridSys);
        SetRef(foodSp, "_scoreManager", scoreMgr);
        // _powerUpManager wired after puMgr is created below
        SetPrefabRef(foodSp, "_normalFoodPrefab", prefabs.FoodNormal.GetComponent<FoodItem>());
        SetPrefabRef(foodSp, "_bonusFoodPrefab",  prefabs.FoodBonus.GetComponent<FoodItem>());
        SetPrefabRef(foodSp, "_poisonFoodPrefab", prefabs.FoodPoison.GetComponent<FoodItem>());

        // PowerUpManager (needs snake + score + visuals — wired after snake is created)
        var puMgrGO = CreateChild("[PowerUpManager]", systemsGO);
        var puMgr   = puMgrGO.AddComponent<PowerUpManager>();
        SetRef(puMgr, "_score", scoreMgr);

        // PowerUpSpawner
        var puSpawnGO = CreateChild("[PowerUpSpawner]", systemsGO);
        var puSpawn   = puSpawnGO.AddComponent<PowerUpSpawner>();
        SetRef(puSpawn, "_grid",           gridSys);
        SetRef(puSpawn, "_powerUpManager", puMgr);
        // Set prefabs array (ordered by PowerUpType enum 0-5)
        {
            var so   = new SerializedObject(puSpawn);
            var prop = so.FindProperty("_prefabs");
            prop.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                var pu = prefabs.PowerUps[i].GetComponent<PowerUpItem>();
                prop.GetArrayElementAtIndex(i).objectReferenceValue = pu;
            }
            // Allowed types: all except Poison (poison comes from food, not spawner)
            var typesProp = so.FindProperty("_allowedTypes");
            typesProp.arraySize = 5;
            typesProp.GetArrayElementAtIndex(0).enumValueIndex = (int)PowerUpType.SpeedBoost;
            typesProp.GetArrayElementAtIndex(1).enumValueIndex = (int)PowerUpType.Shield;
            typesProp.GetArrayElementAtIndex(2).enumValueIndex = (int)PowerUpType.ScoreMultiplier;
            typesProp.GetArrayElementAtIndex(3).enumValueIndex = (int)PowerUpType.GhostMode;
            typesProp.GetArrayElementAtIndex(4).enumValueIndex = (int)PowerUpType.ShrinkPill;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Snake root ─────────────────────────────────────
        var snakeRootGO = new GameObject("[Snake]");

        var snakeGO  = CreateChild("[SnakeController]", snakeRootGO);
        var snakeCtrl = snakeGO.AddComponent<SnakeController>();
        SetRef(snakeCtrl, "_grid", gridSys);

        var snakeVisGO  = CreateChild("[SnakeVisuals]", snakeRootGO);
        var snakeVis    = snakeVisGO.AddComponent<SnakeVisuals>();
        SetRef(snakeVis, "_snake", snakeCtrl);
        SetRef(snakeVis, "_grid",  gridSys);
        SetPrefabRef(snakeVis, "_headPrefab", prefabs.SnakeHead.GetComponent<SnakeSegment>());
        SetPrefabRef(snakeVis, "_bodyPrefab", prefabs.SnakeBody.GetComponent<SnakeSegment>());

        // SwipeInputHandler
        var swipeGO = CreateChild("[SwipeInput]", snakeRootGO);
        var swipe   = swipeGO.AddComponent<SwipeInputHandler>();
        SetRef(swipe, "_snake", snakeCtrl);

        // Wire PowerUpManager's missing snake + visuals refs
        SetRef(puMgr, "_snake",        snakeCtrl);
        SetRef(puMgr, "_snakeVisuals", snakeVis);

        // Wire FoodSpawner's PowerUpManager ref (needed for poison food effect)
        SetRef(foodSp, "_powerUpManager", puMgr);

        // ── LevelManager (needed for Time Attack timer + Campaign levels) ─
        var lvlMgrGO  = new GameObject("[LevelManager]");
        var lvlMgr    = lvlMgrGO.AddComponent<LevelManager>();
        SetRef(lvlMgr, "_grid",  gridSys);
        SetRef(lvlMgr, "_snake", snakeCtrl);
        SetRef(lvlMgr, "_score", scoreMgr);

        // Time Attack LevelData asset — 120 seconds, no walls, 20×20 grid
        var taLevel = CreateTimeAttackLevelData();

        // ── GameSession (wires everything together) ────────
        var sessionGO  = new GameObject("[GameSession]");
        var session    = sessionGO.AddComponent<GameSession>();
        SetRef(session, "_grid",             gridSys);
        SetRef(session, "_snake",            snakeCtrl);
        SetRef(session, "_foodSpawner",      foodSp);
        SetRef(session, "_powerUpSpawner",   puSpawn);
        SetRef(session, "_powerUpManager",   puMgr);
        SetRef(session, "_scoreManager",     scoreMgr);
        SetRef(session, "_levelManager",     lvlMgr);
        SetRef(session, "_timeAttackLevel", taLevel);

        // ── VFX Manager ────────────────────────────────────
        var vfxManagerGO = new GameObject("[VFXManager]");
        vfxManagerGO.AddComponent<VFXManager>();

        // ── UI Canvas ──────────────────────────────────────
        var uiRootGO = new GameObject("[UI]");

        var hudCanvasGO = new GameObject("HUDCanvas");
        hudCanvasGO.transform.SetParent(uiRootGO.transform);
        var hudCanvas = hudCanvasGO.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ConfigureCanvasScaler(hudCanvasGO.AddComponent<CanvasScaler>());
        hudCanvasGO.AddComponent<GraphicRaycaster>();

        // Screen adaptation: SafeAreaPanel prevents HUD content being clipped by notches.
        var hudSafeAreaGO = new GameObject("HUDSafeArea");
        hudSafeAreaGO.transform.SetParent(hudCanvasGO.transform, false);
        var hudSafeAreaRT = hudSafeAreaGO.AddComponent<RectTransform>();
        hudSafeAreaRT.anchorMin = Vector2.zero;
        hudSafeAreaRT.anchorMax = Vector2.one;
        hudSafeAreaRT.offsetMin = hudSafeAreaRT.offsetMax = Vector2.zero;
        hudSafeAreaGO.AddComponent<SafeAreaPanel>();

        // ScreenFlash — full-screen flash VFX on death
        hudCanvasGO.AddComponent<ScreenFlash>();

        // Score text (top centre)
        var scoreTxtGO = CreateTMPText("ScoreText", hudCanvasGO,
            new Vector2(0f, 0.9f), new Vector2(1f, 1f), 36,
            Color.white, TextAlignmentOptions.Top);

        // NeonUIAnimator — pulse the score text between cyan and white
        {
            var anim   = scoreTxtGO.AddComponent<NeonUIAnimator>();
            var so     = new SerializedObject(anim);
            var textProp   = so.FindProperty("_text");
            var colorAProp = so.FindProperty("_colorA");
            var colorBProp = so.FindProperty("_colorB");
            var speedProp  = so.FindProperty("_speed");
            textProp.objectReferenceValue = scoreTxtGO.GetComponent<TextMeshProUGUI>();
            colorAProp.colorValue = new Color(0f, 1f, 0.8f);
            colorBProp.colorValue = Color.white;
            speedProp.floatValue  = 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Multiplier text (below score)
        var multTxtGO = CreateTMPText("MultiplierText", hudCanvasGO,
            new Vector2(0f, 0.82f), new Vector2(1f, 0.9f), 24,
            new Color(1f, 0.8f, 0f), TextAlignmentOptions.Top);

        // Timer panel (Time Attack / Campaign only — starts hidden)
        var timerPanelGO = new GameObject("TimerPanel");
        timerPanelGO.transform.SetParent(hudCanvasGO.transform, false);
        var timerPanelRT = timerPanelGO.AddComponent<RectTransform>();
        timerPanelRT.anchorMin = new Vector2(0f, 0.74f);
        timerPanelRT.anchorMax = new Vector2(1f, 0.82f);
        timerPanelRT.offsetMin = timerPanelRT.offsetMax = Vector2.zero;
        timerPanelGO.SetActive(false); // hidden by default; HUD.Start() shows it when needed

        var timerTxtGO = CreateTMPText("TimerText", timerPanelGO,
            Vector2.zero, Vector2.one, 28,
            new Color(0f, 0.9f, 1f), TextAlignmentOptions.Top, "0:00");

        // Coin text (top-left, below score band) — gold colour
        var coinTxtGO = CreateTMPText("CoinText", hudCanvasGO,
            new Vector2(0.0f, 0.88f), new Vector2(0.4f, 0.98f), 20,
            new Color(1f, 0.84f, 0f), TextAlignmentOptions.TopLeft, "$ 0");

        // HUD component
        var hudGO  = new GameObject("HUD");
        hudGO.transform.SetParent(uiRootGO.transform);
        var hud = hudGO.AddComponent<HUD>();
        SetRef(hud, "_scoreText",      scoreTxtGO.GetComponent<TextMeshProUGUI>());
        SetRef(hud, "_multiplierText", multTxtGO.GetComponent<TextMeshProUGUI>());
        SetRef(hud, "_coinText",       coinTxtGO.GetComponent<TextMeshProUGUI>());
        SetRef(hud, "_scoreManager",   scoreMgr);
        SetRef(hud, "_levelManager",   lvlMgr);
        SetRef(hud, "_timerPanel",     timerPanelGO);
        SetRef(hud, "_timerText",      timerTxtGO.GetComponent<TextMeshProUGUI>());

        // Pause button (top-right corner)
        var pauseBtnGO = CreateUIButton("PauseBtn", hudCanvasGO,
            new Vector2(0.82f, 0.88f), new Vector2(0.98f, 0.98f), "II",
            new Color(0.2f, 0.2f, 0.3f));

        // Pause panel
        var pausePanelGO = CreateUIPanel("PausePanel", hudCanvasGO, new Color(0f, 0f, 0f, 0.88f));
        pausePanelGO.SetActive(false);

        CreateTMPText("PauseTitleText", pausePanelGO,
            new Vector2(0.2f, 0.62f), new Vector2(0.8f, 0.78f), 48,
            new Color(0f, 1f, 0.8f), TextAlignmentOptions.Center).GetComponent<TextMeshProUGUI>().text = "PAUSED";

        var pauseResumeBtnGO = CreateUIButton("ResumeBtn", pausePanelGO,
            new Vector2(0.2f, 0.45f), new Vector2(0.8f, 0.60f), "RESUME",
            new Color(0f, 1f, 0.4f));

        var pauseMenuBtnGO = CreateUIButton("MenuBtn", pausePanelGO,
            new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.43f), "MAIN MENU",
            new Color(0.5f, 0.5f, 0.5f));

        var pauseUI = hudCanvasGO.AddComponent<PauseUI>();
        SetRef(pauseUI, "_panel",          pausePanelGO);
        SetRef(pauseUI, "_resumeButton",   pauseResumeBtnGO.GetComponent<Button>());
        SetRef(pauseUI, "_mainMenuButton", pauseMenuBtnGO.GetComponent<Button>());
        SetRef(pauseUI, "_swipeInput",     swipe);

        SetRef(hud, "_pauseButton", pauseBtnGO.GetComponent<Button>());
        SetRef(hud, "_pauseUI",     pauseUI);

        // Personal best label (top-right, same row as coin text)
        var personalBestTxtGO = CreateTMPText("PersonalBestText", hudCanvasGO,
            new Vector2(0.42f, 0.88f), new Vector2(0.82f, 0.98f), 20,
            new Color(1f, 0.84f, 0f), TextAlignmentOptions.TopRight, "BEST: ---");

        // Shield indicator (bottom-left — cyan, hidden by default)
        var shieldIndicatorGO = new GameObject("ShieldIndicator");
        shieldIndicatorGO.transform.SetParent(hudCanvasGO.transform, false);
        var shieldRT = shieldIndicatorGO.AddComponent<RectTransform>();
        shieldRT.anchorMin = new Vector2(0.01f, 0.02f);
        shieldRT.anchorMax = new Vector2(0.35f, 0.09f);
        shieldRT.offsetMin = shieldRT.offsetMax = Vector2.zero;
        shieldIndicatorGO.SetActive(false);
        CreateTMPText("ShieldText", shieldIndicatorGO,
            Vector2.zero, Vector2.one, 22,
            new Color(0f, 1f, 1f), TextAlignmentOptions.MidlineLeft, "◆ SHIELD");

        // Frenzy Mode indicator (bottom-centre — gold, hidden by default)
        var frenzyIndicatorGO = new GameObject("FrenzyIndicator");
        frenzyIndicatorGO.transform.SetParent(hudCanvasGO.transform, false);
        var frenzyRT = frenzyIndicatorGO.AddComponent<RectTransform>();
        frenzyRT.anchorMin = new Vector2(0.30f, 0.02f);
        frenzyRT.anchorMax = new Vector2(0.70f, 0.09f);
        frenzyRT.offsetMin = frenzyRT.offsetMax = Vector2.zero;
        frenzyIndicatorGO.SetActive(false);
        CreateTMPText("FrenzyText", frenzyIndicatorGO,
            Vector2.zero, Vector2.one, 22,
            new Color(1f, 0.84f, 0f), TextAlignmentOptions.Midline, "★ FRENZY!");

        // Wire new HUD fields
        SetRef(hud, "_personalBestText",  personalBestTxtGO.GetComponent<TextMeshProUGUI>());
        SetRef(hud, "_shieldIndicator",   shieldIndicatorGO);
        SetRef(hud, "_frenzyIndicator",   frenzyIndicatorGO);
        SetRef(hud, "_powerUpManager",    puMgr);
        SetRef(hud, "_snake",             snakeCtrl);

        // Game Over panel
        var goPanelGO = CreateUIPanel("GameOverPanel", hudCanvasGO, new Color(0,0,0,0.85f));
        goPanelGO.SetActive(false);

        var goScoreTxtGO = CreateTMPText("FinalScoreText", goPanelGO,
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f), 40,
            Color.white, TextAlignmentOptions.Center);

        var goBestTxtGO = CreateTMPText("BestScoreText", goPanelGO,
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f), 28,
            new Color(1f, 0.8f, 0f), TextAlignmentOptions.Center);

        // "NEW BEST!" label — shown in gold when the player beats their record
        var goNewBestTxtGO = CreateTMPText("NewBestText", goPanelGO,
            new Vector2(0.1f, 0.87f), new Vector2(0.9f, 0.96f), 28,
            new Color(1f, 0.84f, 0f), TextAlignmentOptions.Center, "NEW BEST!");
        goNewBestTxtGO.SetActive(false); // hidden until GameOverUI.Show() evaluates the score

        var restartBtnGO  = CreateUIButton("RestartButton",  goPanelGO,
            new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.50f), "RESTART",
            new Color(0f, 1f, 0.4f));

        var menuBtnGO = CreateUIButton("MainMenuButton", goPanelGO,
            new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.33f), "MENU",
            new Color(0f, 0.6f, 1f));

        // GameOverUI must live on an ALWAYS-ACTIVE object so OnEnable() fires at scene start.
        // The panel (goPanelGO) starts disabled, so attaching to it would prevent subscription.
        var gameOverUIGO  = hudCanvasGO.AddComponent<GameOverUI>();
        SetRef(gameOverUIGO, "_panel",          goPanelGO);
        SetRef(gameOverUIGO, "_finalScoreText", goScoreTxtGO.GetComponent<TextMeshProUGUI>());
        SetRef(gameOverUIGO, "_bestScoreText",  goBestTxtGO.GetComponent<TextMeshProUGUI>());
        SetRef(gameOverUIGO, "_newBestText",    goNewBestTxtGO.GetComponent<TextMeshProUGUI>());
        SetRef(gameOverUIGO, "_restartButton",  restartBtnGO.GetComponent<Button>());
        SetRef(gameOverUIGO, "_mainMenuButton", menuBtnGO.GetComponent<Button>());
        SetRef(gameOverUIGO, "_scoreManager",   scoreMgr);
        SetRef(gameOverUIGO, "_gameSession",    session);

        // Level Complete panel
        var lcPanelGO = CreateUIPanel("LevelCompletePanel", hudCanvasGO, new Color(0f, 0.05f, 0.1f, 0.92f));
        lcPanelGO.SetActive(false);

        var lcLevelNameTxt = CreateTMPText("LevelNameText", lcPanelGO,
            new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.90f), 36,
            new Color(0f, 1f, 0.4f), TextAlignmentOptions.Center);

        var lcStarsTxt = CreateTMPText("StarsText", lcPanelGO,
            new Vector2(0.1f, 0.64f), new Vector2(0.9f, 0.76f), 48,
            new Color(1f, 0.85f, 0f), TextAlignmentOptions.Center, "★★★");

        var lcScoreTxt = CreateTMPText("ScoreText", lcPanelGO,
            new Vector2(0.1f, 0.50f), new Vector2(0.9f, 0.63f), 34,
            Color.white, TextAlignmentOptions.Center);

        var lcNextBtnGO = CreateUIButton("NextLevelBtn", lcPanelGO,
            new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.47f), "NEXT LEVEL",
            new Color(0f, 1f, 0.4f));

        var lcMenuBtnGO = CreateUIButton("MenuBtn", lcPanelGO,
            new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.30f), "MENU",
            new Color(0.5f, 0.5f, 0.5f));

        var lcUI = hudCanvasGO.AddComponent<LevelCompleteUI>();
        SetRef(lcUI, "_panel",        lcPanelGO);
        SetRef(lcUI, "_scoreText",    lcScoreTxt.GetComponent<TextMeshProUGUI>());
        SetRef(lcUI, "_starsText",    lcStarsTxt.GetComponent<TextMeshProUGUI>());
        SetRef(lcUI, "_levelNameText", lcLevelNameTxt.GetComponent<TextMeshProUGUI>());
        SetRef(lcUI, "_nextLevelBtn", lcNextBtnGO.GetComponent<Button>());
        SetRef(lcUI, "_mainMenuBtn",  lcMenuBtnGO.GetComponent<Button>());
        SetRef(lcUI, "_scoreManager", scoreMgr);
        SetRef(lcUI, "_gameSession",  session);
        SetRef(lcUI, "_levelManager", lvlMgr);

        // Countdown panel — semi-transparent overlay, hidden initially by CountdownUI itself
        var cdPanelGO = CreateUIPanel("CountdownPanel", hudCanvasGO, new Color(0f, 0f, 0f, 0.55f));
        cdPanelGO.SetActive(false); // CountdownUI.StartCountdown() will re-enable it

        var cdTextGO = CreateTMPText("CountdownText", cdPanelGO,
            new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f), 120,
            new Color(0f, 1f, 0.8f), TextAlignmentOptions.Center, "3");

        // CountdownUI must live on an always-active parent (hudCanvasGO) so it can
        // receive Start() and respond to GameSession.BeginSession() before the panel
        // is activated.
        var countdownUI = hudCanvasGO.AddComponent<CountdownUI>();
        SetRef(countdownUI, "_panel",          cdPanelGO);
        SetRef(countdownUI, "_countdownText",  cdTextGO.GetComponent<TextMeshProUGUI>());

        // Wire CountdownUI into GameSession so the session waits for GO!
        SetRef(session, "_countdownUI", countdownUI);

        // ── Power-Up HUD (active effect icons — right side of screen) ─────────
        // Container: a vertical LayoutGroup anchored to the right
        var puHudContainerGO = new GameObject("PowerUpSlotContainer");
        puHudContainerGO.transform.SetParent(hudCanvasGO.transform, false);
        var puHudRT = puHudContainerGO.AddComponent<RectTransform>();
        puHudRT.anchorMin = new Vector2(0.72f, 0.55f);
        puHudRT.anchorMax = new Vector2(0.98f, 0.88f);
        puHudRT.offsetMin = puHudRT.offsetMax = Vector2.zero;
        var puVLG = puHudContainerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        puVLG.spacing            = 4f;
        puVLG.childAlignment     = UnityEngine.TextAnchor.UpperRight;
        puVLG.childForceExpandWidth  = true;
        puVLG.childForceExpandHeight = false;
        puVLG.childControlHeight     = false;
        puVLG.childControlWidth      = true;

        var puHud = hudCanvasGO.AddComponent<PowerUpHUD>();
        SetRef(puHud, "_powerUpManager", puMgr);
        SetRef(puHud, "_container",      puHudRT);

        // ── Power-Up Tooltip (first-time pickup explainer) ─
        // Anchored centre-top: slides into view below the score area.
        var tooltipPanelGO = new GameObject("PowerUpTooltipPanel");
        tooltipPanelGO.transform.SetParent(hudCanvasGO.transform, false);
        var tooltipRT = tooltipPanelGO.AddComponent<RectTransform>();
        tooltipRT.anchorMin = new Vector2(0.10f, 0.86f);
        tooltipRT.anchorMax = new Vector2(0.90f, 0.95f);
        tooltipRT.offsetMin = tooltipRT.offsetMax = Vector2.zero;

        // Dark semi-transparent background
        var tooltipBG = tooltipPanelGO.AddComponent<Image>();
        tooltipBG.color = new Color(0f, 0f, 0f, 0.82f);

        // CanvasGroup for alpha fading
        var tooltipCG = tooltipPanelGO.AddComponent<CanvasGroup>();
        tooltipCG.alpha          = 0f;
        tooltipCG.interactable   = false;
        tooltipCG.blocksRaycasts = false;

        // Name text (top half — neon colour set at runtime per type)
        var tooltipNameGO = CreateTMPText("TooltipNameText", tooltipPanelGO,
            new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.95f),
            22, Color.white, TextAlignmentOptions.Center);
        var tooltipNameTMP = tooltipNameGO.GetComponent<TextMeshProUGUI>();
        tooltipNameTMP.fontStyle = FontStyles.Bold;
        tooltipNameTMP.text      = "";

        // Description text (bottom half — white)
        var tooltipDescGO = CreateTMPText("TooltipDescText", tooltipPanelGO,
            new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.50f),
            16, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Center);
        tooltipDescGO.GetComponent<TextMeshProUGUI>().text = "";

        var tooltipUI = hudCanvasGO.AddComponent<PowerUpTooltipUI>();
        SetRef(tooltipUI, "_panel",          tooltipCG);
        SetRef(tooltipUI, "_nameText",       tooltipNameTMP);
        SetRef(tooltipUI, "_descText",       tooltipDescGO.GetComponent<TextMeshProUGUI>());
        SetRef(tooltipUI, "_powerUpManager", puMgr);

        // ── Floating Score Text Spawner ────────────────────
        var floatingPrefab    = CreateFloatingTextPrefab();
        var floatingSpawnerGO = new GameObject("[FloatingTextSpawner]");
        var floatingSpawner   = floatingSpawnerGO.AddComponent<FloatingTextSpawner>();
        SetRef(floatingSpawner, "_scoreManager",      scoreMgr);
        SetRef(floatingSpawner, "_snake",             snakeCtrl);
        SetPrefabRef(floatingSpawner, "_floatingTextPrefab", floatingPrefab.GetComponent<FloatingScoreText>());

        // ── Editor Mode Picker (only visible when playing Game scene directly) ─
        var pickerPanelGO = CreateUIPanel("EditorModePickerPanel", hudCanvasGO, new Color(0f, 0f, 0f, 0.92f));
        CreateTMPText("PickerTitle", pickerPanelGO,
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.85f), 32,
            new Color(0f, 1f, 0.8f), TextAlignmentOptions.Center)
            .GetComponent<TextMeshProUGUI>().text = "SELECT MODE (EDITOR)";

        var pickerClassicGO     = CreateUIButton("PickerClassicBtn",     pickerPanelGO, new Vector2(0.1f,0.54f), new Vector2(0.9f,0.67f), "CLASSIC",     new Color(0f,0.8f,0.3f));
        var pickerTimeAttackGO  = CreateUIButton("PickerTimeAttackBtn",  pickerPanelGO, new Vector2(0.1f,0.38f), new Vector2(0.9f,0.51f), "TIME ATTACK", new Color(0.2f,0.4f,1f));
        var pickerCampaignGO    = CreateUIButton("PickerCampaignBtn",    pickerPanelGO, new Vector2(0.1f,0.22f), new Vector2(0.9f,0.35f), "CAMPAIGN",    new Color(1f,0.6f,0f));

        var picker = hudCanvasGO.AddComponent<EditorModePicker>();
        SetRef(picker, "_panel",         pickerPanelGO);
        SetRef(picker, "_classicBtn",    pickerClassicGO.GetComponent<Button>());
        SetRef(picker, "_timeAttackBtn", pickerTimeAttackGO.GetComponent<Button>());
        SetRef(picker, "_campaignBtn",   pickerCampaignGO.GetComponent<Button>());

        // ── Swipe Hint UI (first-session only) ────────────────
        var swipeHintPanelGO = new GameObject("SwipeHintPanel");
        swipeHintPanelGO.transform.SetParent(hudCanvasGO.transform, false);
        var swipeHintRT = swipeHintPanelGO.AddComponent<RectTransform>();
        swipeHintRT.anchorMin = new Vector2(0.2f, 0.3f);
        swipeHintRT.anchorMax = new Vector2(0.8f, 0.55f);
        swipeHintRT.offsetMin = swipeHintRT.offsetMax = Vector2.zero;

        // Semi-transparent dark background
        var swipeHintBG = swipeHintPanelGO.AddComponent<Image>();
        swipeHintBG.color = new Color(0f, 0f, 0f, 0.7f);

        // CanvasGroup for fade in / fade out
        var swipeHintCG = swipeHintPanelGO.AddComponent<CanvasGroup>();
        swipeHintCG.alpha          = 0f;
        swipeHintCG.interactable   = false;
        swipeHintCG.blocksRaycasts = false;

        // "SWIPE TO STEER" label — cyan, top half of panel
        var swipeHintLabelGO = CreateTMPText("SwipeHintLabel", swipeHintPanelGO,
            new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.95f),
            22, new Color(0f, 1f, 1f), TextAlignmentOptions.Center, "SWIPE TO STEER");
        var swipeHintLabelTMP = swipeHintLabelGO.GetComponent<TextMeshProUGUI>();
        swipeHintLabelTMP.fontStyle = FontStyles.Bold;

        // Arrow text — white, large, bottom half of panel
        var swipeHintArrowGO = CreateTMPText("SwipeHintArrow", swipeHintPanelGO,
            new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.55f),
            48, Color.white, TextAlignmentOptions.Center, "↑");

        // SwipeHintUI component on the panel
        var swipeHintUI = swipeHintPanelGO.AddComponent<SwipeHintUI>();
        SetRef(swipeHintUI, "_canvasGroup", swipeHintCG);
        SetRef(swipeHintUI, "_arrowText",   swipeHintArrowGO.GetComponent<TextMeshProUGUI>());
        SetRef(swipeHintUI, "_snake",       snakeCtrl);

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[Setup] Game scene created.");
    }

    // ─────────────────────────────────────────────────────────
    // STEP 7 — MAIN MENU SCENE (placeholder)
    // ─────────────────────────────────────────────────────────

    static void CreateMainMenuScene()
    {
        string path = "Assets/_Project/Scenes/MainMenu.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic       = true;
        cam.clearFlags         = CameraClearFlags.SolidColor;
        cam.backgroundColor    = new Color(0.02f, 0.03f, 0.05f);
        cam.transform.position = new Vector3(0, 0, -10f);
        camGO.AddComponent<AudioListener>();

        // EventSystem — must use InputSystemUIInputModule for new Input System
        var menuEventSysGO = new GameObject("EventSystem");
        menuEventSysGO.AddComponent<EventSystem>();
        menuEventSysGO.AddComponent<InputSystemUIInputModule>();

        // Canvas — uses shared helper so all scenes stay consistent.
        var canvasGO    = new GameObject("Canvas");
        var canvas      = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ConfigureCanvasScaler(canvasGO.AddComponent<CanvasScaler>());
        canvasGO.AddComponent<GraphicRaycaster>();

        // Screen adaptation: SafeAreaPanel insets the menu content away from notches.
        var menuSafeAreaGO = new GameObject("SafeArea");
        menuSafeAreaGO.transform.SetParent(canvasGO.transform, false);
        var menuSafeAreaRT = menuSafeAreaGO.AddComponent<RectTransform>();
        menuSafeAreaRT.anchorMin = Vector2.zero;
        menuSafeAreaRT.anchorMax = Vector2.one;
        menuSafeAreaRT.offsetMin = menuSafeAreaRT.offsetMax = Vector2.zero;
        menuSafeAreaGO.AddComponent<SafeAreaPanel>();

        // ── Background snake demo (lowest sibling → behind everything) ──────
        var snakeDemoGO = new GameObject("MenuSnakeDemo", typeof(RectTransform));
        snakeDemoGO.transform.SetParent(canvasGO.transform, false);
        var snakeDemoRT      = snakeDemoGO.GetComponent<RectTransform>();
        snakeDemoRT.anchorMin = Vector2.zero;
        snakeDemoRT.anchorMax = Vector2.one;
        snakeDemoRT.offsetMin = Vector2.zero;
        snakeDemoRT.offsetMax = Vector2.zero;
        snakeDemoGO.AddComponent<MenuSnakeDemo>();
        snakeDemoGO.transform.SetAsFirstSibling(); // render behind all UI

        // ── Title panel (CanvasGroup for entrance animation) ─────────────────
        var titlePanelGO  = new GameObject("TitlePanel", typeof(RectTransform));
        titlePanelGO.transform.SetParent(canvasGO.transform, false);
        var titlePanelRT       = titlePanelGO.GetComponent<RectTransform>();
        titlePanelRT.anchorMin = new Vector2(0f, 0.70f);
        titlePanelRT.anchorMax = new Vector2(1f, 0.95f);
        titlePanelRT.offsetMin = Vector2.zero;
        titlePanelRT.offsetMax = Vector2.zero;
        var titleGroup = titlePanelGO.AddComponent<CanvasGroup>();

        // Title text inside the panel
        var menuTitleGO = CreateTMPText("TitleText", titlePanelGO,
            Vector2.zero, Vector2.one, 64,
            new Color(0f, 1f, 0.4f), TextAlignmentOptions.Center, "NEON SERPENT");

        // NeonTitleAnimator on title — flicker + color cycle
        {
            var anim = menuTitleGO.AddComponent<NeonTitleAnimator>();
            var so   = new SerializedObject(anim);
            so.FindProperty("_text")       .objectReferenceValue = menuTitleGO.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_colorA")     .colorValue           = new Color(0f, 1f, 0.4f);
            so.FindProperty("_colorB")     .colorValue           = new Color(0f, 0.9f, 1f);
            so.FindProperty("_cycleSpeed") .floatValue           = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Best score text — just below title
        var bestScoreGO = CreateTMPText("BestScoreText", canvasGO,
            new Vector2(0.2f, 0.64f), new Vector2(0.8f, 0.70f), 22,
            new Color(1f, 0.85f, 0f), TextAlignmentOptions.Center, string.Empty);

        // ── Buttons panel (CanvasGroup for entrance animation) ────────────────
        var btnPanelGO  = new GameObject("ButtonsPanel", typeof(RectTransform));
        btnPanelGO.transform.SetParent(canvasGO.transform, false);
        var btnPanelRT       = btnPanelGO.GetComponent<RectTransform>();
        btnPanelRT.anchorMin = new Vector2(0f, 0.08f);
        btnPanelRT.anchorMax = new Vector2(1f, 0.63f);
        btnPanelRT.offsetMin = Vector2.zero;
        btnPanelRT.offsetMax = Vector2.zero;
        var buttonsGroup = btnPanelGO.AddComponent<CanvasGroup>();

        // Helper: normalized anchors relative to ButtonsPanel (panel spans 0.08–0.63 = 0.55 of screen)
        // We stack 6 buttons evenly inside the panel using panel-local normalized coords
        var btnDefs = new (string name, string label, Color col)[]
        {
            ("ClassicBtn",     "CLASSIC",     new Color(0f,   1f,   0.4f)),
            ("TimeAttackBtn",  "TIME ATTACK", new Color(1f,   0.6f, 0f  )),
            ("CampaignBtn",    "CAMPAIGN",    new Color(0f,   0.6f, 1f  )),
            ("LeaderboardBtn", "LEADERBOARD", new Color(0.8f, 0.8f, 0.8f)),
            ("ShopBtn",        "SHOP",        new Color(1f,   0.85f,0f  )),
            ("SettingsBtn",    "SETTINGS",    new Color(0.6f, 0.6f, 0.9f)),
        };

        float slotH = 1f / btnDefs.Length;
        for (int i = 0; i < btnDefs.Length; i++)
        {
            float yMax = 1f - i * slotH;
            float yMin = yMax - slotH;
            var btnGO = CreateUIButton(btnDefs[i].name, btnPanelGO,
                new Vector2(0.2f, yMin + 0.01f), new Vector2(0.8f, yMax - 0.01f),
                btnDefs[i].label, btnDefs[i].col);

            // Attach MenuButtonAnimator to each button
            var animator = btnGO.AddComponent<MenuButtonAnimator>();
            var so = new SerializedObject(animator);
            so.FindProperty("_normalColor").colorValue = new Color(btnDefs[i].col.r, btnDefs[i].col.g, btnDefs[i].col.b, 0.75f);
            so.FindProperty("_hoverColor") .colorValue = Color.white;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── MainMenuUI — wire groups and best score text ──────────────────────
        var menuUI = canvasGO.AddComponent<MainMenuUI>();
        {
            var so = new SerializedObject(menuUI);
            so.FindProperty("_titleGroup")   .objectReferenceValue = titleGroup;
            so.FindProperty("_buttonsGroup") .objectReferenceValue = buttonsGroup;
            so.FindProperty("_bestScoreText").objectReferenceValue = bestScoreGO.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[Setup] MainMenu scene created.");
    }

    // ─────────────────────────────────────────────────────────
    // STEP 8 — BUILD SETTINGS
    // ─────────────────────────────────────────────────────────

    static void ConfigureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Bootstrap.unity",    true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Game.unity",         true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/MainMenu.unity",     true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/LevelSelect.unity",  true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Leaderboard.unity",  true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Shop.unity",         true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Settings.unity",     true),
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[Setup] Build settings configured. Bootstrap=0, Game=1, MainMenu=2, LevelSelect=3, Leaderboard=4, Shop=5, Settings=6");
    }

    // ─────────────────────────────────────────────────────────
    // STEP 9 — LEVEL SELECT SCENE
    // ─────────────────────────────────────────────────────────

    static void CreateLevelSelectScene()
    {
        string path = "Assets/_Project/Scenes/LevelSelect.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupSceneCamera(new Color(0.02f, 0.03f, 0.05f));
        SetupEventSystem();

        var canvasGO = CreateCanvas("LevelSelectCanvas");

        CreateTMPText("TitleText", canvasGO,
            new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.98f), 40,
            new Color(0f, 1f, 0.8f), TextAlignmentOptions.Center)
            .GetComponent<TextMeshProUGUI>().text = "SELECT LEVEL";

        // Scrollable grid for level buttons
        var scrollGO = new GameObject("LevelScroll");
        scrollGO.transform.SetParent(canvasGO.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        var scrollRT   = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.02f, 0.12f);
        scrollRT.anchorMax = new Vector2(0.98f, 0.86f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        var grid = contentGO.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(160f, 120f);
        grid.spacing         = new Vector2(12f, 12f);
        grid.padding         = new RectOffset(12, 12, 12, 12);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content    = contentRT;
        scrollRect.vertical   = true;
        scrollRect.horizontal = false;

        // Level button prefab (created and saved as prefab, then assigned to LevelSelectUI)
        var levelBtnPrefab = CreateLevelButtonPrefab();

        CreateUIButton("BackBtn", canvasGO,
            new Vector2(0.02f, 0.01f), new Vector2(0.35f, 0.10f), "BACK",
            new Color(0.5f, 0.5f, 0.5f));

        var lsUI = canvasGO.AddComponent<LevelSelectUI>();
        SetRef(lsUI, "_levelGridContainer", contentGO.transform);
        SetRef(lsUI, "_backButton",         canvasGO.transform.Find("BackBtn")?.GetComponent<Button>());
        SetRef(lsUI, "_levelButtonPrefab",  levelBtnPrefab);

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[Setup] LevelSelect scene created.");
    }

    static GameObject CreateLevelButtonPrefab()
    {
        const string path = "Assets/_Project/Prefabs/UI/LevelButton.prefab";
        EnsureFolder("Assets/_Project/Prefabs/UI");

        if (File.Exists(Path.GetFullPath(path)))
            AssetDatabase.DeleteAsset(path);

        // Root button
        var go  = new GameObject("LevelButton");
        var img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.12f, 0.18f);
        var btn = go.AddComponent<Button>();
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160f, 120f);

        // Level name text
        var nameGO  = new GameObject("LevelNameText");
        nameGO.transform.SetParent(go.transform, false);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.fontSize  = 16;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color     = Color.white;
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.4f);
        nameRT.anchorMax = new Vector2(1f, 0.9f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

        // Stars text
        var starsGO  = new GameObject("StarsText");
        starsGO.transform.SetParent(go.transform, false);
        var starsTMP = starsGO.AddComponent<TextMeshProUGUI>();
        starsTMP.fontSize  = 20;
        starsTMP.alignment = TextAlignmentOptions.Center;
        starsTMP.color     = new Color(1f, 0.85f, 0f);
        var starsRT = starsGO.GetComponent<RectTransform>();
        starsRT.anchorMin = new Vector2(0f, 0.05f);
        starsRT.anchorMax = new Vector2(1f, 0.42f);
        starsRT.offsetMin = starsRT.offsetMax = Vector2.zero;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[Setup] LevelButton prefab created.");
        return prefab;
    }

    // ─────────────────────────────────────────────────────────
    // STEP 10 — LEADERBOARD SCENE
    // ─────────────────────────────────────────────────────────

    static void CreateLeaderboardScene()
    {
        string path = "Assets/_Project/Scenes/Leaderboard.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupSceneCamera(new Color(0.02f, 0.03f, 0.05f));
        SetupEventSystem();

        var canvasGO = CreateCanvas("Canvas");

        // Title
        CreateTMPText("TitleText", canvasGO,
            new Vector2(0f, 0.88f), new Vector2(1f, 0.97f), 44,
            new Color(0f, 1f, 0.4f), TextAlignmentOptions.Center, "LEADERBOARD");

        // Tab buttons
        CreateUIButton("ClassicTabBtn",     canvasGO, new Vector2(0.02f,0.78f), new Vector2(0.34f,0.87f), "CLASSIC",     new Color(0f,1f,0.4f));
        CreateUIButton("TimeAttackTabBtn",  canvasGO, new Vector2(0.36f,0.78f), new Vector2(0.64f,0.87f), "TIME",        new Color(1f,0.6f,0f));
        CreateUIButton("CampaignTabBtn",    canvasGO, new Vector2(0.66f,0.78f), new Vector2(0.98f,0.87f), "CAMPAIGN",    new Color(0f,0.6f,1f));

        // Entries scroll area
        var entriesParentGO = new GameObject("EntriesContainer");
        entriesParentGO.transform.SetParent(canvasGO.transform, false);
        var rt = entriesParentGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.02f, 0.15f);
        rt.anchorMax = new Vector2(0.98f, 0.76f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var noScoresTxt = CreateTMPText("NoScoresText", canvasGO,
            new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f), 28,
            new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Center, "No scores yet!");

        // Bottom buttons
        CreateUIButton("OnlineBtn", canvasGO, new Vector2(0.25f,0.04f), new Vector2(0.75f,0.12f), "VIEW ONLINE", new Color(0.8f,0.8f,0f));
        CreateUIButton("BackBtn",   canvasGO, new Vector2(0.02f,0.04f), new Vector2(0.22f,0.12f), "BACK",        new Color(0.7f,0.7f,0.7f));

        // LeaderboardUI component
        var lbUI = canvasGO.AddComponent<LeaderboardUI>();
        SetRef(lbUI, "_titleText",          canvasGO.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>());
        SetRef(lbUI, "_entriesContainer",   entriesParentGO.transform);
        SetRef(lbUI, "_noScoresText",       noScoresTxt.GetComponent<TextMeshProUGUI>());
        SetRef(lbUI, "_classicTabBtn",      canvasGO.transform.Find("ClassicTabBtn")?.GetComponent<Button>());
        SetRef(lbUI, "_timeAttackTabBtn",   canvasGO.transform.Find("TimeAttackTabBtn")?.GetComponent<Button>());
        SetRef(lbUI, "_campaignTabBtn",     canvasGO.transform.Find("CampaignTabBtn")?.GetComponent<Button>());
        SetRef(lbUI, "_onlineBtn",          canvasGO.transform.Find("OnlineBtn")?.GetComponent<Button>());
        SetRef(lbUI, "_backBtn",            canvasGO.transform.Find("BackBtn")?.GetComponent<Button>());

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[Setup] Leaderboard scene created.");
    }

    // ─────────────────────────────────────────────────────────
    // STEP 10 — SHOP SCENE
    // ─────────────────────────────────────────────────────────

    static GameObject CreateSkinSlotPrefab()
    {
        const string path = "Assets/_Project/Prefabs/UI/SkinSlot.prefab";
        EnsureFolder("Assets/_Project/Prefabs/UI");

        if (File.Exists(Path.GetFullPath(path)))
            AssetDatabase.DeleteAsset(path);

        var go  = new GameObject("SkinSlot");
        var img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.12f, 0.18f);
        go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180f, 200f);

        var nameGO = new GameObject("SkinNameText");
        nameGO.transform.SetParent(go.transform, false);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.fontSize  = 14;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color     = Color.white;
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.08f);
        nameRT.anchorMax = new Vector2(1f, 0.32f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

        var statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(go.transform, false);
        var statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
        statusTMP.fontSize  = 12;
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.color     = new Color(0f, 1f, 0.8f);
        var statusRT = statusGO.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0f, 0f);
        statusRT.anchorMax = new Vector2(1f, 0.1f);
        statusRT.offsetMin = statusRT.offsetMax = Vector2.zero;

        var slot = go.AddComponent<SkinSlotUI>();
        SetRef(slot, "_nameText", nameTMP);
        SetRef(slot, "_priceText", statusTMP);
        SetRef(slot, "_button",   go.GetComponent<Button>());

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[Setup] SkinSlot prefab created.");
        return prefab;
    }

    static void CreateShopScene()
    {
        string path = "Assets/_Project/Scenes/Shop.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupSceneCamera(new Color(0.02f, 0.03f, 0.05f));
        SetupEventSystem();

        var canvasGO = CreateCanvas("Canvas");

        CreateTMPText("TitleText", canvasGO,
            new Vector2(0f, 0.88f), new Vector2(1f, 0.97f), 44,
            new Color(1f, 0.6f, 0f), TextAlignmentOptions.Center, "SHOP");

        CreateTMPText("CoinBalanceText", canvasGO,
            new Vector2(0.5f, 0.79f), new Vector2(1f, 0.87f), 28,
            new Color(1f, 0.85f, 0f), TextAlignmentOptions.Right, "0 coins");

        // Skin grid container
        var skinGridGO = new GameObject("SkinGridContainer");
        skinGridGO.transform.SetParent(canvasGO.transform, false);
        var sgrt = skinGridGO.AddComponent<RectTransform>();
        sgrt.anchorMin = new Vector2(0.02f, 0.20f);
        sgrt.anchorMax = new Vector2(0.98f, 0.78f);
        sgrt.offsetMin = sgrt.offsetMax = Vector2.zero;
        var glg = skinGridGO.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        glg.cellSize = new Vector2(140f, 180f);
        glg.spacing  = new Vector2(10f, 10f);

        CreateUIButton("RemoveAdsBtn", canvasGO, new Vector2(0.1f,0.10f), new Vector2(0.9f,0.18f), "REMOVE ADS", new Color(1f,0.3f,0.3f));
        CreateUIButton("BackBtn",      canvasGO, new Vector2(0.02f,0.01f), new Vector2(0.25f,0.09f), "BACK",     new Color(0.7f,0.7f,0.7f));

        // Create SkinSlotUI prefab
        var skinSlotPrefab = CreateSkinSlotPrefab();

        var shopUI = canvasGO.AddComponent<ShopUI>();
        SetRef(shopUI, "_coinBalanceText",   canvasGO.transform.Find("CoinBalanceText")?.GetComponent<TextMeshProUGUI>());
        SetRef(shopUI, "_skinGridContainer", skinGridGO.transform);
        SetRef(shopUI, "_removeAdsBtn",      canvasGO.transform.Find("RemoveAdsBtn")?.GetComponent<Button>());
        SetRef(shopUI, "_backBtn",           canvasGO.transform.Find("BackBtn")?.GetComponent<Button>());
        SetPrefabRef(shopUI, "_skinSlotPrefab", skinSlotPrefab.GetComponent<SkinSlotUI>());

        // Pre-populate _skins array with the 6 available skins
        {
            var so   = new SerializedObject(shopUI);
            var prop = so.FindProperty("_skins");
            var skinDefs = new (string id, string name, int cost)[]
            {
                ("default",     "Cyan",         0),
                ("neon_pink",   "Neon Pink",   100),
                ("cyber_blue",  "Cyber Blue",  100),
                ("toxic_green", "Toxic Green", 150),
                ("gold",        "Gold",        250),
                ("void_purple", "Void Purple", 300),
            };
            prop.arraySize = skinDefs.Length;
            for (int i = 0; i < skinDefs.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("Id").stringValue          = skinDefs[i].id;
                elem.FindPropertyRelative("DisplayName").stringValue = skinDefs[i].name;
                elem.FindPropertyRelative("CoinCost").intValue       = skinDefs[i].cost;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[Setup] Shop scene created.");
    }

    // ─────────────────────────────────────────────────────────
    // STEP 11 — SETTINGS SCENE
    // ─────────────────────────────────────────────────────────

    static void CreateSettingsScene()
    {
        string path = "Assets/_Project/Scenes/Settings.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupSceneCamera(new Color(0.02f, 0.03f, 0.05f));
        SetupEventSystem();

        var canvasGO = CreateCanvas("Canvas");

        CreateTMPText("TitleText", canvasGO,
            new Vector2(0f, 0.88f), new Vector2(1f, 0.97f), 44,
            new Color(0f, 0.8f, 1f), TextAlignmentOptions.Center, "SETTINGS");

        // Labels
        CreateTMPText("SFXLabel",   canvasGO, new Vector2(0.05f,0.72f), new Vector2(0.45f,0.80f), 26, Color.white, TextAlignmentOptions.Left,  "SFX VOLUME");
        CreateTMPText("MusicLabel", canvasGO, new Vector2(0.05f,0.59f), new Vector2(0.45f,0.67f), 26, Color.white, TextAlignmentOptions.Left,  "MUSIC");
        CreateTMPText("VibLabel",   canvasGO, new Vector2(0.05f,0.46f), new Vector2(0.45f,0.54f), 26, Color.white, TextAlignmentOptions.Left,  "VIBRATION");

        // SFX slider
        var sfxSliderGO = new GameObject("SFXSlider");
        sfxSliderGO.transform.SetParent(canvasGO.transform, false);
        var sfxSRT = sfxSliderGO.AddComponent<RectTransform>();
        sfxSRT.anchorMin = new Vector2(0.5f, 0.72f); sfxSRT.anchorMax = new Vector2(0.95f, 0.80f);
        sfxSRT.offsetMin = sfxSRT.offsetMax = Vector2.zero;
        var sfxSlider = sfxSliderGO.AddComponent<Slider>();
        sfxSlider.minValue = 0f; sfxSlider.maxValue = 1f; sfxSlider.value = 1f;

        // Music slider
        var musicSliderGO = new GameObject("MusicSlider");
        musicSliderGO.transform.SetParent(canvasGO.transform, false);
        var mSRT = musicSliderGO.AddComponent<RectTransform>();
        mSRT.anchorMin = new Vector2(0.5f, 0.59f); mSRT.anchorMax = new Vector2(0.95f, 0.67f);
        mSRT.offsetMin = mSRT.offsetMax = Vector2.zero;
        var musicSlider = musicSliderGO.AddComponent<Slider>();
        musicSlider.minValue = 0f; musicSlider.maxValue = 1f; musicSlider.value = 0.7f;

        // Vibration toggle
        var vibToggleGO = new GameObject("VibrationToggle");
        vibToggleGO.transform.SetParent(canvasGO.transform, false);
        var vSRT = vibToggleGO.AddComponent<RectTransform>();
        vSRT.anchorMin = new Vector2(0.5f, 0.46f); vSRT.anchorMax = new Vector2(0.65f, 0.54f);
        vSRT.offsetMin = vSRT.offsetMax = Vector2.zero;
        var vibToggle = vibToggleGO.AddComponent<Toggle>();
        vibToggle.isOn = true;

        // Delete save button
        CreateUIButton("DeleteSaveBtn", canvasGO, new Vector2(0.1f,0.25f), new Vector2(0.9f,0.33f), "DELETE SAVE DATA", new Color(1f,0.2f,0.2f));

        // Confirm panel (starts hidden)
        var confirmPanelGO = CreateUIPanel("ConfirmDeletePanel", canvasGO, new Color(0,0,0,0.92f));
        confirmPanelGO.SetActive(false);
        CreateTMPText("ConfirmText", confirmPanelGO, new Vector2(0.1f,0.55f), new Vector2(0.9f,0.75f), 30, Color.white, TextAlignmentOptions.Center, "Delete all save data?\nThis cannot be undone.");
        var confirmYesGO = CreateUIButton("ConfirmYesBtn", confirmPanelGO, new Vector2(0.1f,0.35f), new Vector2(0.45f,0.50f), "YES",    new Color(1f,0.2f,0.2f));
        var confirmNoGO  = CreateUIButton("ConfirmNoBtn",  confirmPanelGO, new Vector2(0.55f,0.35f), new Vector2(0.9f,0.50f), "CANCEL", new Color(0.5f,0.5f,0.5f));

        CreateUIButton("BackBtn", canvasGO, new Vector2(0.02f,0.01f), new Vector2(0.25f,0.09f), "BACK", new Color(0.7f,0.7f,0.7f));

        var settingsUI = canvasGO.AddComponent<SettingsUI>();
        SetRef(settingsUI, "_sfxSlider",          sfxSlider);
        SetRef(settingsUI, "_musicSlider",         musicSlider);
        SetRef(settingsUI, "_vibrationToggle",     vibToggle);
        SetRef(settingsUI, "_deleteSaveBtn",       canvasGO.transform.Find("DeleteSaveBtn")?.GetComponent<Button>());
        SetRef(settingsUI, "_confirmDeletePanel",  confirmPanelGO);
        SetRef(settingsUI, "_confirmYesBtn",       confirmYesGO.GetComponent<Button>());
        SetRef(settingsUI, "_confirmNoBtn",        confirmNoGO.GetComponent<Button>());
        SetRef(settingsUI, "_backBtn",             canvasGO.transform.Find("BackBtn")?.GetComponent<Button>());

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[Setup] Settings scene created.");
    }

    // ─────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string child  = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    static GameObject CreateChild(string name, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    // Set a serialized field by name on a MonoBehaviour
    static void SetRef<T>(MonoBehaviour target, string fieldName, T value) where T : Object
    {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        else Debug.LogWarning($"[Setup] Field not found: {target.GetType().Name}.{fieldName}");
    }

    // Set a serialized field to a prefab component reference
    static void SetPrefabRef<T>(MonoBehaviour target, string fieldName, T value) where T : Component
    {
        SetRef(target, fieldName, value);
    }

    // Set an enum/int serialized field
    static void SetField(MonoBehaviour target, string fieldName, int enumIndex)
    {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.enumValueIndex = enumIndex; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    // Create a TextMeshProUGUI element
    static GameObject CreateTMPText(string name, GameObject parent,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color,
        TextAlignmentOptions alignment, string content = "0")
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text           = content;
        txt.fontSize       = fontSize;
        txt.color          = color;
        txt.alignment      = alignment;
        txt.fontStyle      = FontStyles.Bold;
        txt.enableWordWrapping = false;

        return go;
    }

    // Shared scene camera setup
    static Camera SetupSceneCamera(Color bgColor)
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic    = true;
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = bgColor;
        cam.transform.position = new Vector3(0, 0, -10f);
        camGO.AddComponent<AudioListener>();
        return cam;
    }

    // Shared EventSystem with new Input System module
    static void SetupEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    // Shared Canvas setup — every canvas must scale with screen size using 1080×1920 at 0.5 match.
    static GameObject CreateCanvas(string name)
    {
        var go     = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ConfigureCanvasScaler(go.AddComponent<CanvasScaler>());
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    /// <summary>
    /// Apply the standard responsive scaler settings to a CanvasScaler.
    /// All canvases in the project use 1080x1920 portrait reference at 0.5 match.
    /// </summary>
    static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;
    }

    static GameObject CreateUIPanel(string name, GameObject parent, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img   = go.AddComponent<Image>();
        img.color = bgColor;

        return go;
    }

    static GameObject CreateUIButton(string name, GameObject parent,
        Vector2 anchorMin, Vector2 anchorMax, string label, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(color.r * 0.2f, color.g * 0.2f, color.b * 0.2f, 0.9f);

        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = img.color;
        cb.highlightedColor = color;
        cb.pressedColor     = color * 0.7f;
        btn.colors          = cb;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var txt = labelGO.AddComponent<Text>();
        txt.text      = label;
        txt.fontSize  = 28;
        txt.color     = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontStyle = FontStyle.Bold;

        return go;
    }
}
