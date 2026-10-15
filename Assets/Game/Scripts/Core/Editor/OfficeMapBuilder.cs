#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Unity.AI.Navigation;
using TMPro;
using RageHunters.Player;
using RageHunters.Camera;
using RageHunters.Input;
using RageHunters.Combat;
using RageHunters.AI;
using RageHunters.Missions;
using RageHunters.Inventory;
using RageHunters.UI;
using RageHunters.Core;

namespace RageHunters.Editor
{
    /// <summary>
    /// Automated generator for the Abandoned Office Complex survival map,
    /// complete with geometry, player rig, AI enemies, extraction zone,
    /// mobile touch controls canvas, and in-game HUD.
    /// </summary>
    public static class OfficeMapBuilder
    {
        private const string SCENE_DIR = "Assets/Game/Scenes";
        private const string SCENE_PATH = "Assets/Game/Scenes/OfficeComplexScene.unity";

        [MenuItem("Rage Hunters/Maps/Build Abandoned Office Complex Scene")]
        public static void BuildOfficeComplexScene()
        {
            if (!AssetDatabase.IsValidFolder(SCENE_DIR))
            {
                AssetDatabase.CreateFolder("Assets/Game", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Lighting & Environment
            SetupEnvironment();

            // 2. Office Complex Architecture (Floors, Walls, Rooms, Furniture)
            GameObject mapRoot = BuildOfficeArchitecture();

            // 3. Bake NavMesh
            BakeNavMesh(mapRoot);

            // 4. Player Rig & Third-Person Camera
            GameObject player = CreatePlayerRig(new Vector3(0f, 1f, -25f));

            // 5. AI Enemies
            CreateVillainEnemy(new Vector3(10f, 1f, 15f), "OfficeTyrant_AI");
            CreateVillainEnemy(new Vector3(-12f, 1f, 5f), "Backstabber_AI");

            // 6. Extraction Zone
            CreateExtractionZone(new Vector3(0f, 0.1f, 32f));

            // 7. Loot Drops
            CreateLootPickup(new Vector3(5f, 0.5f, -10f), "Keycard_ServerRoom");
            CreateLootPickup(new Vector3(-8f, 0.5f, 12f), "Medkit");
            CreateLootPickup(new Vector3(12f, 0.5f, -5f), "Baton_Melee");

            // 8. Mobile Touch UI & HUD Canvas
            CreateGameplayUI(player);

            // 9. Mission & Match Managers
            CreateManagers();

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[OfficeMapBuilder] Abandoned Office Complex scene created successfully at {SCENE_PATH}!");
        }

        #region Environment & Lighting

        private static void SetupEnvironment()
        {
            var lightGo = new GameObject("Directional Light (Emergency Lighting)");
            var light = lightGo.AddComponent<UnityEngine.Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.85f, 0.88f, 0.95f);
            light.intensity = 0.65f;
            light.shadows = LightShadows.Hard;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.08f, 0.09f, 0.12f);
            RenderSettings.fogDensity = 0.015f;
        }

        #endregion

        #region Architecture

        private static GameObject BuildOfficeArchitecture()
        {
            var root = new GameObject("Office_Complex_Map");

            // Main Floor (70m x 80m)
            CreateBox("Floor_Main", root.transform, new Vector3(0f, -0.25f, 5f), new Vector3(60f, 0.5f, 80f), new Color(0.2f, 0.22f, 0.24f));
            CreateBox("Ceiling", root.transform, new Vector3(0f, 6.25f, 5f), new Vector3(60f, 0.5f, 80f), new Color(0.15f, 0.16f, 0.18f));

            // Perimeter Walls
            CreateBox("Wall_North", root.transform, new Vector3(0f, 3f, 45f), new Vector3(60f, 6f, 1f), new Color(0.3f, 0.32f, 0.35f));
            CreateBox("Wall_South", root.transform, new Vector3(0f, 3f, -35f), new Vector3(60f, 6f, 1f), new Color(0.3f, 0.32f, 0.35f));
            CreateBox("Wall_East", root.transform, new Vector3(30f, 3f, 5f), new Vector3(1f, 6f, 80f), new Color(0.3f, 0.32f, 0.35f));
            CreateBox("Wall_West", root.transform, new Vector3(-30f, 3f, 5f), new Vector3(1f, 6f, 80f), new Color(0.3f, 0.32f, 0.35f));

            // Interior Dividing Walls (Corridors & Rooms)
            // Reception Area (South)
            CreateBox("Wall_Reception_Left", root.transform, new Vector3(-12f, 3f, -20f), new Vector3(18f, 6f, 0.6f), new Color(0.35f, 0.35f, 0.38f));
            CreateBox("Wall_Reception_Right", root.transform, new Vector3(12f, 3f, -20f), new Vector3(18f, 6f, 0.6f), new Color(0.35f, 0.35f, 0.38f));
            CreateBox("Reception_Desk", root.transform, new Vector3(0f, 0.7f, -24f), new Vector3(8f, 1.4f, 2f), new Color(0.45f, 0.3f, 0.2f));

            // Central Corridor & Open Office
            CreateBox("Wall_Corridor_West", root.transform, new Vector3(-8f, 3f, 5f), new Vector3(0.6f, 6f, 40f), new Color(0.32f, 0.33f, 0.36f));
            CreateBox("Wall_Corridor_East", root.transform, new Vector3(8f, 3f, 5f), new Vector3(0.6f, 6f, 40f), new Color(0.32f, 0.33f, 0.36f));

            // Executive Office (North West)
            CreateBox("Wall_Exec_Office", root.transform, new Vector3(-18f, 3f, 25f), new Vector3(20f, 6f, 0.6f), new Color(0.28f, 0.3f, 0.33f));
            CreateBox("Exec_Desk", root.transform, new Vector3(-18f, 0.6f, 32f), new Vector3(4f, 1.2f, 2f), new Color(0.35f, 0.25f, 0.15f));

            // Server Room (North East)
            CreateBox("Wall_Server_Room", root.transform, new Vector3(18f, 3f, 25f), new Vector3(20f, 6f, 0.6f), new Color(0.22f, 0.25f, 0.28f));
            for (int i = 0; i < 4; i++)
            {
                CreateBox($"ServerRack_{i}", root.transform, new Vector3(14f + (i * 3.5f), 1.5f, 33f), new Vector3(1.2f, 3f, 2.5f), new Color(0.1f, 0.12f, 0.15f));
            }

            // Meeting Room (West)
            CreateBox("Meeting_Table", root.transform, new Vector3(-18f, 0.5f, 5f), new Vector3(8f, 1f, 4f), new Color(0.4f, 0.28f, 0.2f));

            // Cubicle Partitions (East Open Office)
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 2; c++)
                {
                    Vector3 pos = new Vector3(14f + (c * 7f), 0.75f, -10f + (r * 8f));
                    CreateBox($"Cubicle_{r}_{c}", root.transform, pos, new Vector3(4f, 1.5f, 4f), new Color(0.38f, 0.4f, 0.42f));
                }
            }

            // Office Pillars
            CreateBox("Pillar_1", root.transform, new Vector3(-6f, 3f, -10f), new Vector3(1.5f, 6f, 1.5f), new Color(0.4f, 0.42f, 0.45f));
            CreateBox("Pillar_2", root.transform, new Vector3(6f, 3f, -10f), new Vector3(1.5f, 6f, 1.5f), new Color(0.4f, 0.42f, 0.45f));
            CreateBox("Pillar_3", root.transform, new Vector3(-6f, 3f, 15f), new Vector3(1.5f, 6f, 1.5f), new Color(0.4f, 0.42f, 0.45f));
            CreateBox("Pillar_4", root.transform, new Vector3(6f, 3f, 15f), new Vector3(1.5f, 6f, 1.5f), new Color(0.4f, 0.42f, 0.45f));

            return root;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.2f);
            renderer.material = mat;

            return go;
        }

        #endregion

        #region NavMesh

        private static void BakeNavMesh(GameObject root)
        {
            var navSurface = root.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navSurface.BuildNavMesh();
            Debug.Log("[OfficeMapBuilder] NavMesh successfully baked for Abandoned Office Complex.");
        }

        #endregion

        #region Player Rig

        private static GameObject CreatePlayerRig(Vector3 spawnPosition)
        {
            var playerRoot = new GameObject("Player");
            playerRoot.tag = "Player";
            playerRoot.transform.position = spawnPosition;

            // Character Controller
            var cc = playerRoot.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Visual Capsule Representation
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Player_Visual";
            visual.transform.SetParent(playerRoot.transform);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);

            // Remove visual collider to prevent collision with character controller
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            var playerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            playerMat.color = new Color(0.15f, 0.5f, 0.85f); // Deep blue survivor
            visual.GetComponent<MeshRenderer>().material = playerMat;

            // Combat & HitBox
            playerRoot.AddComponent<MeleeCombat>();
            var hitBox = playerRoot.AddComponent<HitBox>();

            // Player Gameplay Systems
            playerRoot.AddComponent<PlayerStats>();
            playerRoot.AddComponent<PlayerInputHandler>();
            var controller = playerRoot.AddComponent<PlayerController>();

            // Third-Person Orbit Camera
            var camGo = new GameObject("ThirdPersonCamera");
            camGo.tag = "MainCamera";
            var cameraComp = camGo.AddComponent<UnityEngine.Camera>();
            cameraComp.nearClipPlane = 0.2f;
            cameraComp.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();

            var tpCamera = camGo.AddComponent<ThirdPersonCamera>();
            tpCamera.Target = playerRoot.transform;

            return playerRoot;
        }

        #endregion

        #region AI Enemies

        private static GameObject CreateVillainEnemy(Vector3 spawnPos, string enemyName)
        {
            var enemyRoot = new GameObject(enemyName);
            enemyRoot.tag = "Enemy";
            enemyRoot.transform.position = spawnPos;

            var agent = enemyRoot.AddComponent<NavMeshAgent>();
            agent.height = 1.8f;
            agent.radius = 0.4f;
            agent.speed = 3.5f;
            agent.stoppingDistance = 1.5f;

            // Visual Cylinder Representation (Threat styling)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Enemy_Visual";
            visual.transform.SetParent(enemyRoot.transform);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            var enemyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            enemyMat.color = new Color(0.85f, 0.15f, 0.15f); // Restrained villain red accent
            visual.GetComponent<MeshRenderer>().material = enemyMat;

            // HitBox & Melee
            var hitBox = enemyRoot.AddComponent<HitBox>();
            enemyRoot.AddComponent<MeleeCombat>();

            // AI Controller
            enemyRoot.AddComponent<AIController>();

            return enemyRoot;
        }

        #endregion

        #region Extraction & Loot

        private static void CreateExtractionZone(Vector3 position)
        {
            var extractGo = new GameObject("ExtractionZone");
            extractGo.transform.position = position;

            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "Beacon_Pad";
            beacon.transform.SetParent(extractGo.transform);
            beacon.transform.localPosition = Vector3.zero;
            beacon.transform.localScale = new Vector3(6f, 0.1f, 6f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.1f, 0.8f, 0.3f, 0.7f);
            beacon.GetComponent<MeshRenderer>().material = mat;

            var collider = extractGo.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 3.5f;

            extractGo.AddComponent<ExtractionPoint>();
        }

        private static void CreateLootPickup(Vector3 position, string itemName)
        {
            var lootGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lootGo.name = $"Loot_{itemName}";
            lootGo.tag = "Loot";
            lootGo.transform.position = position;
            lootGo.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.95f, 0.8f, 0.2f); // Golden loot glow
            lootGo.GetComponent<MeshRenderer>().material = mat;

            var collider = lootGo.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            lootGo.AddComponent<LootDrop>();
        }

        #endregion

        #region Mobile UI & HUD

        private static void CreateGameplayUI(GameObject player)
        {
            var canvasGo = new GameObject("MobileControlsCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Event System
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Virtual Joystick
            var joyBgGo = new GameObject("VirtualJoystick");
            joyBgGo.transform.SetParent(canvasGo.transform, false);
            var joyBgRect = joyBgGo.AddComponent<RectTransform>();
            joyBgRect.anchorMin = new Vector2(0f, 0f);
            joyBgRect.anchorMax = new Vector2(0f, 0f);
            joyBgRect.pivot = new Vector2(0.5f, 0.5f);
            joyBgRect.anchoredPosition = new Vector2(250f, 250f);
            joyBgRect.sizeDelta = new Vector2(240f, 240f);

            var joyBgImg = joyBgGo.AddComponent<Image>();
            joyBgImg.color = new Color(0.2f, 0.2f, 0.25f, 0.45f);

            var handleGo = new GameObject("JoystickHandle");
            handleGo.transform.SetParent(joyBgGo.transform, false);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(100f, 100f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(0.85f, 0.2f, 0.2f, 0.75f);

            joyBgGo.AddComponent<VirtualJoystick>();

            // Camera Look Touch Region (Right Screen Half)
            var lookRegionGo = new GameObject("TouchCameraRegion");
            lookRegionGo.transform.SetParent(canvasGo.transform, false);
            var lookRect = lookRegionGo.AddComponent<RectTransform>();
            lookRect.anchorMin = new Vector2(0.4f, 0f);
            lookRect.anchorMax = new Vector2(1f, 1f);
            lookRect.offsetMin = Vector2.zero;
            lookRect.offsetMax = Vector2.zero;
            var lookImg = lookRegionGo.AddComponent<Image>();
            lookImg.color = Color.clear;
            lookRegionGo.AddComponent<TouchCameraRegion>();

            // Action Buttons Panel (Bottom Right)
            CreateTouchButton(canvasGo.transform, "Btn_Attack", new Vector2(-160f, 160f), "ATTACK", new Color(0.85f, 0.15f, 0.15f));
            CreateTouchButton(canvasGo.transform, "Btn_Sprint", new Vector2(-280f, 110f), "SPRINT", new Color(0.25f, 0.55f, 0.85f));
            CreateTouchButton(canvasGo.transform, "Btn_Jump", new Vector2(-110f, 280f), "JUMP", new Color(0.35f, 0.75f, 0.35f));
            CreateTouchButton(canvasGo.transform, "Btn_Crouch", new Vector2(-230f, 230f), "CROUCH", new Color(0.55f, 0.55f, 0.6f));
            CreateTouchButton(canvasGo.transform, "Btn_Interact", new Vector2(-360f, 220f), "INTERACT", new Color(0.95f, 0.75f, 0.15f));

            // Central Touch Input Manager
            canvasGo.AddComponent<TouchInputManager>();

            // HUD Overlay Screen
            var hudGo = new GameObject("HUDScreen");
            hudGo.transform.SetParent(canvasGo.transform, false);
            var hudRect = hudGo.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;
            hudGo.AddComponent<CanvasGroup>();
            hudGo.AddComponent<HUDScreen>();
        }

        private static GameObject CreateTouchButton(Transform parent, string name, Vector2 position, string label, Color color)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            var rect = btnGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(90f, 90f);

            var img = btnGo.AddComponent<Image>();
            img.color = color;

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            btnGo.AddComponent<TouchButton>();
            return btnGo;
        }

        #endregion

        #region Managers

        private static void CreateManagers()
        {
            var mgrRoot = new GameObject("GameManagers");
            mgrRoot.AddComponent<GameManager>();
            mgrRoot.AddComponent<MissionManager>();
            mgrRoot.AddComponent<UIManager>();
        }

        #endregion
    }
}
#endif
