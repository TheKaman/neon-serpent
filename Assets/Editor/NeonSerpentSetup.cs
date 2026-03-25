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
            "• Build Bootstrap & Game scenes\n" +
            "• Add scenes to Build Settings\n\n" +
            "Any existing scenes with the same names will be overwritten.",
            "Yes, set everything up!", "Cancel");

        if (!confirm) return;

        EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Starting...", 0f);

        try
        {
            ConfigurePlayerSettings();
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
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Building MainMenu scene...", 0.88f);

            CreateMainMenuScene();
            EditorUtility.DisplayProgressBar("Neon Serpent Setup", "Configuring build settings...", 0.95f);

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
            "Open the Game scene and press Play to test the snake!\n\n" +
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
        EnsureFolder("Assets/_Project/Textures/Sprites");

        var set = new SpriteSet
        {
            SnakeHead        = MakeSprite("SnakeHead",      new Color(0.0f, 1.0f,  0.4f)),
            SnakeBody        = MakeSprite("SnakeBody",      new Color(0.0f, 0.75f, 0.3f)),
            FoodNormal       = MakeSprite("FoodNormal",     new Color(1.0f, 0.2f,  0.2f)),
            FoodBonus        = MakeSprite("FoodBonus",      new Color(1.0f, 0.8f,  0.0f)),
            FoodPoison       = MakeSprite("FoodPoison",     new Color(0.6f, 0.0f,  1.0f)),
            PowerUpSpeed     = MakeSprite("PU_SpeedBoost",  new Color(1.0f, 0.6f,  0.0f)),
            PowerUpShield    = MakeSprite("PU_Shield",      new Color(0.0f, 0.6f,  1.0f)),
            PowerUpMultiplier= MakeSprite("PU_Multiplier",  new Color(1.0f, 0.1f,  0.8f)),
            PowerUpGhost     = MakeSprite("PU_Ghost",       new Color(0.5f, 0.5f,  0.9f)),
            PowerUpShrink    = MakeSprite("PU_Shrink",      new Color(0.3f, 1.0f,  0.0f)),
            PowerUpPoison    = MakeSprite("PU_Poison",      new Color(0.5f, 0.0f,  0.7f)),
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Setup] Sprites created.");
        return set;
    }

    static Sprite MakeSprite(string name, Color color)
    {
        string path = $"Assets/_Project/Textures/Sprites/{name}.png";

        // Draw a 28×28 filled square inside a 32×32 texture (2px border stays transparent)
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                pixels[y * 32 + x] = (x >= 2 && x < 30 && y >= 2 && y < 30) ? color : Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType           = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit   = 32;
        importer.filterMode            = FilterMode.Point;
        importer.textureCompression    = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency   = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
        canvasGO.AddComponent<CanvasScaler>();
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

        // URP camera data is auto-added by Unity when URP is active

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

        // ── GameSession (wires everything together) ────────
        var sessionGO  = new GameObject("[GameSession]");
        var session    = sessionGO.AddComponent<GameSession>();
        SetRef(session, "_grid",           gridSys);
        SetRef(session, "_snake",          snakeCtrl);
        SetRef(session, "_foodSpawner",    foodSp);
        SetRef(session, "_powerUpSpawner", puSpawn);
        SetRef(session, "_powerUpManager", puMgr);
        // _levelManager is optional for Phase 1 — leave null

        // ── UI Canvas ──────────────────────────────────────
        var uiRootGO = new GameObject("[UI]");

        var hudCanvasGO = new GameObject("HUDCanvas");
        hudCanvasGO.transform.SetParent(uiRootGO.transform);
        var hudCanvas = hudCanvasGO.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvasGO.AddComponent<CanvasScaler>();
        hudCanvasGO.AddComponent<GraphicRaycaster>();

        // Score text (top centre)
        var scoreTxtGO = CreateUIText("ScoreText", hudCanvasGO,
            new Vector2(0f, 0.9f), new Vector2(1f, 1f), 36,
            Color.white, TextAnchor.UpperCenter);

        // Multiplier text (below score)
        var multTxtGO = CreateUIText("MultiplierText", hudCanvasGO,
            new Vector2(0f, 0.82f), new Vector2(1f, 0.9f), 24,
            new Color(1f, 0.8f, 0f), TextAnchor.UpperCenter);

        // HUD component
        var hudGO  = new GameObject("HUD");
        hudGO.transform.SetParent(uiRootGO.transform);
        var hud = hudGO.AddComponent<HUD>();
        SetRef(hud, "_scoreText",      scoreTxtGO.GetComponent<Text>());
        SetRef(hud, "_multiplierText", multTxtGO.GetComponent<Text>());
        SetRef(hud, "_scoreManager",   scoreMgr);

        // Game Over panel
        var goPanelGO = CreateUIPanel("GameOverPanel", hudCanvasGO, new Color(0,0,0,0.85f));
        goPanelGO.SetActive(false);

        var goScoreTxtGO = CreateUIText("FinalScoreText", goPanelGO,
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f), 40,
            Color.white, TextAnchor.MiddleCenter);

        var restartBtnGO  = CreateUIButton("RestartButton",  goPanelGO,
            new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.50f), "RESTART",
            new Color(0f, 1f, 0.4f));

        var menuBtnGO = CreateUIButton("MainMenuButton", goPanelGO,
            new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.33f), "MENU",
            new Color(0f, 0.6f, 1f));

        var gameOverUIGO  = goPanelGO.AddComponent<GameOverUI>();
        SetRef(gameOverUIGO, "_panel",          goPanelGO);
        SetRef(gameOverUIGO, "_finalScoreText", goScoreTxtGO.GetComponent<Text>());
        SetRef(gameOverUIGO, "_restartButton",  restartBtnGO.GetComponent<Button>());
        SetRef(gameOverUIGO, "_mainMenuButton", menuBtnGO.GetComponent<Button>());
        SetRef(gameOverUIGO, "_scoreManager",   scoreMgr);

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
        cam.orthographic    = true;
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
        cam.transform.position = new Vector3(0, 0, -10f);
        camGO.AddComponent<AudioListener>();

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Title
        CreateUIText("TitleText", canvasGO,
            new Vector2(0f, 0.75f), new Vector2(1f, 0.92f), 52,
            new Color(0f, 1f, 0.4f), TextAnchor.MiddleCenter, "NEON SERPENT");

        // Buttons
        CreateUIButton("ClassicBtn",    canvasGO, new Vector2(0.25f,0.58f), new Vector2(0.75f,0.68f), "CLASSIC",     new Color(0f,1f,0.4f));
        CreateUIButton("TimeAttackBtn", canvasGO, new Vector2(0.25f,0.45f), new Vector2(0.75f,0.55f), "TIME ATTACK", new Color(1f,0.6f,0f));
        CreateUIButton("CampaignBtn",   canvasGO, new Vector2(0.25f,0.32f), new Vector2(0.75f,0.42f), "CAMPAIGN",    new Color(0f,0.6f,1f));
        CreateUIButton("LeaderboardBtn",canvasGO, new Vector2(0.25f,0.19f), new Vector2(0.75f,0.29f), "LEADERBOARD", new Color(0.8f,0.8f,0.8f));

        // MainMenuUI script
        var menuUI = canvasGO.AddComponent<MainMenuUI>();

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
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Bootstrap.unity", true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Game.unity",      true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/MainMenu.unity",  true),
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[Setup] Build settings configured. Bootstrap=0, Game=1, MainMenu=2");
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

    // Create a legacy Text UI element (avoids TMP dependency for setup)
    static GameObject CreateUIText(string name, GameObject parent,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color,
        TextAnchor alignment, string content = "0")
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<Text>();
        txt.text      = content;
        txt.fontSize  = fontSize;
        txt.color     = color;
        txt.alignment = alignment;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontStyle = FontStyle.Bold;

        return go;
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
