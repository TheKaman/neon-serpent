// ============================================================
// PostShipAuditWiring.cs
// One-shot Editor utility that closes the four Inspector-only gaps left by the
// pre-ship playtest audit code pass. These four references cannot be set from
// runtime code alone — they are scene/prefab/asset wirings:
//
//   1. GameOverUI._headerText        (Game scene)        — find-or-create header TMP_Text
//   2. ShopUI._watchAdBtn / _watchAdBtnText (Shop scene) — create Watch-Ad button by
//                                                           cloning the existing RemoveAdsBtn
//   3. LevelSelectUI._worldHeaderPrefab (LevelSelect)    — create + assign a WorldHeader prefab
//   4. MusicManager._menuMusic / _gameMusic (Bootstrap)  — assign menu/game music clips
//
// HOW TO USE:
//   NeonSerpent > Apply Post-Audit Wiring
//   Safe to run multiple times — every step skips work that is already done.
//
// NOTE: This is an Editor-only utility. FindObjectsByType is acceptable here
// (the no-FindObjectOfType rule applies to runtime/production code only).
// ============================================================

using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using NeonSerpent.UI;
using NeonSerpent.Audio;

namespace NeonSerpent.Editor
{
    /// <summary>
    /// Editor menu utility that applies the four Inspector-only wirings identified by the
    /// pre-ship playtest audit. Each task is independent, idempotent, and logged separately,
    /// so a failure in one never blocks the others.
    /// </summary>
    public static class PostShipAuditWiring
    {
        // ─── Scene paths ───────────────────────────────────────────────────────
        private const string GAME_SCENE_PATH        = "Assets/_Project/Scenes/Game.unity";
        private const string SHOP_SCENE_PATH        = "Assets/_Project/Scenes/Shop.unity";
        private const string LEVEL_SELECT_SCENE_PATH = "Assets/_Project/Scenes/LevelSelect.unity";
        private const string BOOTSTRAP_SCENE_PATH    = "Assets/_Project/Scenes/Bootstrap.unity";

        // ─── Asset paths ───────────────────────────────────────────────────────
        private const string WORLD_HEADER_PREFAB_PATH = "Assets/_Project/Prefabs/UI/WorldHeader.prefab";
        private const string MUSIC_FOLDER_PATH         = "Assets/_Project/Audio/Music";

        // ─── Palette (matches the existing LevelSelect neon-cyan header colour) ──
        private static readonly Color NEON_CYAN = new Color(0f, 1f, 0.8f, 1f);

        /// <summary>
        /// Entry point. Runs all four wiring tasks in sequence, each guarded so a single
        /// failure cannot abort the rest, then reports a per-task summary dialog.
        /// </summary>
        [MenuItem("NeonSerpent/Apply Post-Audit Wiring")]
        public static void ApplyAll()
        {
            string headerResult   = WireGameOverHeader();
            string watchAdResult  = WireShopWatchAdButton();
            string prefabResult   = WireWorldHeaderPrefab();
            string musicResult    = WireMusicManagerClips();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary =
                $"1. GameOverUI._headerText\n   {headerResult}\n\n" +
                $"2. ShopUI._watchAdBtn\n   {watchAdResult}\n\n" +
                $"3. LevelSelectUI._worldHeaderPrefab\n   {prefabResult}\n\n" +
                $"4. MusicManager music clips\n   {musicResult}";

            Debug.Log("[PostShipAuditWiring] Completed.\n\n" + summary);
            EditorUtility.DisplayDialog("Apply Post-Audit Wiring", summary, "OK");
        }

        // ═════════════════════════════════════════════════════════════════════
        // TASK 1 — GameOverUI._headerText  (Game scene)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Wires GameOverUI._headerText in the Game scene. The header TMP_Text did not exist
        /// in the scene at audit time, so this finds an existing header child (named Header /
        /// Title / HeaderText) under the GameOverUI hierarchy, and creates one if absent — the
        /// field is required for the "GAME OVER" / "TIME'S UP!" copy added in the audit pass.
        /// </summary>
        /// <returns>A human-readable result line for the summary dialog.</returns>
        private static string WireGameOverHeader()
        {
            return RunInScene(GAME_SCENE_PATH, scene =>
            {
                GameOverUI gameOverUI = FindInScene<GameOverUI>(scene, includeInactive: true);
                if (gameOverUI == null)
                    return Warn("GameOverUI not found in Game scene — skipped.");

                var so       = new SerializedObject(gameOverUI);
                var headerProp = so.FindProperty("_headerText");
                if (headerProp == null)
                    return Warn("GameOverUI has no _headerText field — skipped.");

                if (headerProp.objectReferenceValue != null)
                    return Skip($"_headerText already wired to '{headerProp.objectReferenceValue.name}'.");

                // Prefer an existing header child by common names, then any TMP_Text on the
                // panel that is NOT one of the known score labels.
                TMP_Text header = FindExistingHeader(gameOverUI);

                bool created = false;
                if (header == null)
                {
                    header = CreateGameOverHeader(gameOverUI);
                    created = true;
                    if (header == null)
                        return Warn("Could not locate or create a header TMP_Text — skipped.");
                }

                Undo.RecordObject(gameOverUI, "Wire GameOverUI header");
                headerProp.objectReferenceValue = header;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(gameOverUI);

                return created
                    ? Ok($"Created header '{header.name}' and wired _headerText.")
                    : Ok($"Wired existing header '{header.name}' to _headerText.");
            });
        }

        /// <summary>
        /// Searches the GameOverUI hierarchy for a likely header label. Matches by name first
        /// (Header / Title / HeaderText), then falls back to a TMP_Text that is not one of the
        /// known score/button labels so we never re-purpose the score text as the header.
        /// </summary>
        private static TMP_Text FindExistingHeader(GameOverUI gameOverUI)
        {
            var candidates = gameOverUI.GetComponentsInChildren<TMP_Text>(true);
            // 1. Match by explicit name.
            foreach (var t in candidates)
            {
                string n = t.gameObject.name;
                if (n == "Header" || n == "Title" || n == "HeaderText" || n == "GameOverHeader")
                    return t;
            }
            // 2. Fall back: any TMP_Text that is not a known non-header label.
            foreach (var t in candidates)
            {
                string n = t.gameObject.name;
                if (n == "FinalScoreText" || n == "BestScoreText" || n == "NewBestText")
                    continue;
                if (n.Contains("Button") || n == "Label")
                    continue;
                return t;
            }
            return null;
        }

        /// <summary>
        /// Builds a header TMP_Text GameObject under the GameOverUI panel root, anchored to the
        /// top of the panel, defaulting its copy to "GAME OVER" (overwritten at runtime by Show).
        /// </summary>
        private static TMP_Text CreateGameOverHeader(GameOverUI gameOverUI)
        {
            // Parent under the panel if we can find it; otherwise under the GameOverUI object.
            Transform parent = FindChildByName(gameOverUI.transform, "GameOverPanel")
                               ?? gameOverUI.transform;

            var go = new GameObject("Header", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create GameOver Header");
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(0); // top of the panel

            var rt = (RectTransform)go.transform;
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -40f);
            rt.sizeDelta        = new Vector2(600f, 120f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text                 = "GAME OVER";
            tmp.alignment            = TextAlignmentOptions.Center;
            tmp.enableAutoSizing     = true;
            tmp.fontSizeMin          = 24f;
            tmp.fontSizeMax          = 96f;
            tmp.color                = NEON_CYAN;
            tmp.raycastTarget        = false;

            return tmp;
        }

        // ═════════════════════════════════════════════════════════════════════
        // TASK 2 — ShopUI._watchAdBtn / _watchAdBtnText  (Shop scene)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates the "Watch Ad for Coins" button in the Shop scene by cloning the existing
        /// RemoveAdsBtn (so styling and layout match exactly), then wires ShopUI._watchAdBtn and
        /// _watchAdBtnText. The button did not exist at audit time; ShopUI already guards a null
        /// _watchAdBtn, so this purely adds the feature.
        /// </summary>
        private static string WireShopWatchAdButton()
        {
            return RunInScene(SHOP_SCENE_PATH, scene =>
            {
                ShopUI shopUI = FindInScene<ShopUI>(scene, includeInactive: true);
                if (shopUI == null)
                    return Warn("ShopUI not found in Shop scene — skipped.");

                var so          = new SerializedObject(shopUI);
                var btnProp     = so.FindProperty("_watchAdBtn");
                var btnTextProp = so.FindProperty("_watchAdBtnText");
                if (btnProp == null)
                    return Warn("ShopUI has no _watchAdBtn field — skipped.");

                if (btnProp.objectReferenceValue != null)
                    return Skip($"_watchAdBtn already wired to '{btnProp.objectReferenceValue.name}'.");

                // Find the template button (RemoveAdsBtn) to clone.
                Button template = FindButtonByName(scene, "RemoveAdsBtn");
                if (template == null)
                    return Warn("RemoveAdsBtn template not found in Shop scene — skipped.");

                // Clone the template as a sibling so it sits in the same coin-purchase area.
                GameObject clone = Object.Instantiate(template.gameObject, template.transform.parent);
                Undo.RegisterCreatedObjectUndo(clone, "Create Watch-Ad Button");
                clone.name = "WatchAdBtn";
                // Place it immediately after the template in the layout.
                clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);

                Button watchBtn = clone.GetComponent<Button>();
                // The clone carries RemoveAdsBtn's onClick listeners — clear them so it does not
                // also fire the IAP purchase. ShopUI re-binds OnWatchAdBtn in Start() at runtime.
                if (watchBtn != null)
                    watchBtn.onClick = new Button.ButtonClickedEvent();

                // Update the label text on the clone.
                TMP_Text watchText = clone.GetComponentInChildren<TMP_Text>(true);
                if (watchText != null)
                    watchText.text = "WATCH AD\n+50 COINS";

                Undo.RecordObject(shopUI, "Wire ShopUI watch-ad refs");
                btnProp.objectReferenceValue = watchBtn;
                if (btnTextProp != null && watchText != null)
                    btnTextProp.objectReferenceValue = watchText;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(shopUI);

                return Ok(watchText != null
                    ? "Created WatchAdBtn (cloned from RemoveAdsBtn) and wired _watchAdBtn + _watchAdBtnText."
                    : "Created WatchAdBtn and wired _watchAdBtn (no label child found for _watchAdBtnText).");
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // TASK 3 — LevelSelectUI._worldHeaderPrefab  (prefab asset + LevelSelect scene)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates a WorldHeader prefab (a full-row TMP_Text styled to the neon-cyan LevelSelect
        /// palette, with a child named "WorldNameText" so LevelSelectUI.InsertWorldHeader finds it)
        /// and assigns it to LevelSelectUI._worldHeaderPrefab. The prefab is reused if it already
        /// exists, so the asset is created at most once.
        /// </summary>
        private static string WireWorldHeaderPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WORLD_HEADER_PREFAB_PATH);
            bool prefabCreated = false;

            if (prefab == null)
            {
                prefab = CreateWorldHeaderPrefab();
                prefabCreated = true;
                if (prefab == null)
                    return Warn("Failed to create WorldHeader prefab — skipped.");
            }

            string assignResult = RunInScene(LEVEL_SELECT_SCENE_PATH, scene =>
            {
                LevelSelectUI levelSelectUI = FindInScene<LevelSelectUI>(scene, includeInactive: true);
                if (levelSelectUI == null)
                    return Warn("LevelSelectUI not found in LevelSelect scene — prefab created but not assigned.");

                var so   = new SerializedObject(levelSelectUI);
                var prop = so.FindProperty("_worldHeaderPrefab");
                if (prop == null)
                    return Warn("LevelSelectUI has no _worldHeaderPrefab field — skipped.");

                if (prop.objectReferenceValue == prefab)
                    return Skip("_worldHeaderPrefab already assigned.");

                Undo.RecordObject(levelSelectUI, "Wire LevelSelectUI world header prefab");
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(levelSelectUI);
                return Ok("Assigned WorldHeader prefab to _worldHeaderPrefab.");
            });

            string prefabNote = prefabCreated
                ? $"Created prefab at {WORLD_HEADER_PREFAB_PATH}. "
                : "Reused existing prefab. ";
            return prefabNote + assignResult;
        }

        /// <summary>
        /// Builds and saves the WorldHeader prefab asset. Root holds a LayoutElement so it can
        /// span a full row in a GridLayoutGroup; the "WorldNameText" child is the TMP label that
        /// LevelSelectUI populates with the world name.
        /// </summary>
        private static GameObject CreateWorldHeaderPrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs/UI");

            var root = new GameObject("WorldHeader", typeof(RectTransform), typeof(LayoutElement));
            try
            {
                var rootRt = root.GetComponent<RectTransform>();
                rootRt.sizeDelta = new Vector2(600f, 60f);

                // Full-row hint for grid layouts — the developer can tune flexibleWidth/ignoreLayout
                // to match their specific GridLayoutGroup cell spanning.
                var layout = root.GetComponent<LayoutElement>();
                layout.minHeight       = 60f;
                layout.preferredHeight = 60f;
                layout.preferredWidth  = 600f;
                layout.flexibleWidth   = 1f;

                var labelGO = new GameObject("WorldNameText", typeof(RectTransform));
                labelGO.transform.SetParent(root.transform, false);

                var labelRt = (RectTransform)labelGO.transform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;

                var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text               = "WORLD 1";
                tmp.alignment          = TextAlignmentOptions.Center;
                tmp.enableAutoSizing   = true;
                tmp.fontSizeMin        = 18f;
                tmp.fontSizeMax        = 48f;
                tmp.color              = NEON_CYAN;
                tmp.raycastTarget      = false;

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, WORLD_HEADER_PREFAB_PATH);
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // TASK 4 — MusicManager._menuMusic / _gameMusic  (Bootstrap scene)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Assigns MusicManager._menuMusic and _gameMusic in the Bootstrap scene. Looks for the
        /// music clips under Assets/_Project/Audio/Music. If no music assets exist in the project
        /// yet, this logs a clear warning and skips rather than inventing assets — the clips must
        /// be added before this can complete.
        /// </summary>
        private static string WireMusicManagerClips()
        {
            AudioClip menuClip = FindMusicClip("menu", "title");
            AudioClip gameClip = FindMusicClip("game", "play", "gameplay");

            if (menuClip == null && gameClip == null)
            {
                return Warn(
                    $"No music clips found in {MUSIC_FOLDER_PATH}. The folder is empty — add menu " +
                    "and gameplay music tracks there, then re-run this command. Skipped.");
            }

            return RunInScene(BOOTSTRAP_SCENE_PATH, scene =>
            {
                MusicManager musicManager = FindInScene<MusicManager>(scene, includeInactive: true);
                if (musicManager == null)
                    return Warn("MusicManager not found in Bootstrap scene — skipped.");

                var so       = new SerializedObject(musicManager);
                var menuProp = so.FindProperty("_menuMusic");
                var gameProp = so.FindProperty("_gameMusic");
                if (menuProp == null || gameProp == null)
                    return Warn("MusicManager is missing _menuMusic / _gameMusic fields — skipped.");

                Undo.RecordObject(musicManager, "Wire MusicManager clips");
                int assigned = 0;

                if (menuClip != null && menuProp.objectReferenceValue == null)
                {
                    menuProp.objectReferenceValue = menuClip;
                    assigned++;
                }
                if (gameClip != null && gameProp.objectReferenceValue == null)
                {
                    gameProp.objectReferenceValue = gameClip;
                    assigned++;
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(musicManager);

                if (assigned == 0)
                    return Skip("Music clips already assigned.");

                string menuName = menuClip != null ? menuClip.name : "(none)";
                string gameName = gameClip != null ? gameClip.name : "(none)";
                return Ok($"Assigned {assigned} clip(s): menu='{menuName}', game='{gameName}'.");
            });
        }

        /// <summary>
        /// Returns the first AudioClip in the Music folder whose name contains any of the given
        /// keywords (case-insensitive). Returns null when no matching music asset exists — the
        /// caller treats null as "skip this slot" rather than guessing a wrong track.
        /// </summary>
        private static AudioClip FindMusicClip(params string[] keywords)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { MUSIC_FOLDER_PATH });
            if (guids == null || guids.Length == 0) return null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                foreach (string kw in keywords)
                {
                    if (lower.Contains(kw))
                        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            return null;
        }

        // ═════════════════════════════════════════════════════════════════════
        // SHARED HELPERS
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Opens the scene at <paramref name="scenePath"/> (additively if it is not already the
        /// active scene), runs <paramref name="work"/> against it, then marks it dirty + saves and
        /// closes it if this method opened it. Restores the editor to its prior scene context.
        /// </summary>
        private static string RunInScene(string scenePath, System.Func<Scene, string> work)
        {
            string activePath        = SceneManager.GetActiveScene().path;
            bool   sceneAlreadyOpen  = false;
            Scene  scene;

            // If the target scene is already loaded (active or additive), reuse it.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path == scenePath && s.isLoaded)
                {
                    scene = s;
                    sceneAlreadyOpen = true;
                    string r = work(scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    return r;
                }
            }

            // Otherwise open it additively so we never disturb the user's active scene.
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            if (!scene.IsValid())
                return Warn($"Could not open scene {scenePath} — skipped.");

            string result;
            try
            {
                result = work(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (!sceneAlreadyOpen && activePath != scenePath)
                    EditorSceneManager.CloseScene(scene, true);
            }
            return result;
        }

        /// <summary>Finds the first component of type T anywhere in the given scene's hierarchy.</summary>
        private static T FindInScene<T>(Scene scene, bool includeInactive) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(includeInactive);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>Finds a Button by GameObject name across all root objects of a scene.</summary>
        private static Button FindButtonByName(Scene scene, string goName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var btn in root.GetComponentsInChildren<Button>(true))
                {
                    if (btn.gameObject.name == goName) return btn;
                }
            }
            return null;
        }

        /// <summary>Recursively searches a transform's children for one with the given name.</summary>
        private static Transform FindChildByName(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var hit = FindChildByName(parent.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        /// <summary>Creates the asset folder chain for the given project-relative path if needed.</summary>
        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string leaf   = Path.GetFileName(folderPath);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ─── Result-string formatters (consistent log prefixes) ─────────────────
        private static string Ok(string msg)   { Debug.Log($"[PostShipAuditWiring] OK: {msg}");        return "OK — " + msg; }
        private static string Skip(string msg) { Debug.Log($"[PostShipAuditWiring] SKIP: {msg}");      return "SKIP — " + msg; }
        private static string Warn(string msg) { Debug.LogWarning($"[PostShipAuditWiring] {msg}");      return "WARN — " + msg; }
    }
}
