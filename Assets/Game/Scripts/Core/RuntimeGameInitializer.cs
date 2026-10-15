using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RageHunters.Player;
using RageHunters.Camera;
using RageHunters.Input;
using RageHunters.Combat;
using RageHunters.AI;
using RageHunters.Missions;
using RageHunters.Inventory;
using RageHunters.Database;
using RageHunters.Audio;

namespace RageHunters.Core
{
    /// <summary>
    /// Production-grade runtime game bootstrapper for RAGE HUNTERS.
    /// Runs automatically upon scene load on Android, iOS, or PC.
    /// Spawns the complete 3D environment, articulated character models, AI villains with autonomous combat,
    /// dynamic multi-limb animations, procedural audio synthesizers, tactical voice comms,
    /// local persistent database, and 10 production UI screens.
    /// </summary>
    public class RuntimeGameInitializer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            if (FindAnyObjectByType<RuntimeGameInitializer>() != null) return;

            var go = new GameObject("RuntimeGameInitializer");
            go.AddComponent<RuntimeGameInitializer>();
        }

        #region Screen & Game Object References

        private GameObject _playerRoot;
        private PlayerStats _playerStats;
        private ProceduralCharacterAnimator _playerAnimator;

        // UI Panels
        private GameObject _splashPanel;
        private GameObject _hudPanel;
        private GameObject _touchControlsPanel;
        private GameObject _mainMenuPanel;
        private GameObject _backpackPanel;
        private GameObject _dossierPanel;
        private GameObject _villainCreatorPanel;
        private GameObject _databasePanel;
        private GameObject _loadoutPanel;
        private GameObject _settingsPanel;
        private GameObject _matchResultsPanel;

        // HUD Elements
        private RectTransform _healthFillRect;
        private TextMeshProUGUI _healthText;
        private RectTransform _staminaFillRect;
        private TextMeshProUGUI _staminaText;
        private TextMeshProUGUI _radarText;
        private TextMeshProUGUI _matchTimerText;
        private Image _damageVignette;

        private float _matchTimeRemaining = 600f; // 10 minutes match clock
        private bool _matchActive = false;
        private int _totalDamageDealt = 0;
        private int _enemiesSlain = 0;

        #endregion

        private void Awake()
        {
            // 1. Initialize Database, Audio, Voice, and Damage Services
            if (GameDatabase.Instance == null)
            {
                var dbGo = new GameObject("GameDatabase");
                dbGo.AddComponent<GameDatabase>();
            }

            if (SoundFXService.Instance == null)
            {
                var sfxGo = new GameObject("SoundFXService");
                sfxGo.AddComponent<SoundFXService>();
            }

            if (TacticalVoiceComms.Instance == null)
            {
                var voxGo = new GameObject("TacticalVoiceComms");
                voxGo.AddComponent<TacticalVoiceComms>();
            }

            if (DamageNumberSpawner.Instance == null)
            {
                var dnsGo = new GameObject("DamageNumberSpawner");
                dnsGo.AddComponent<DamageNumberSpawner>();
            }

            // 2. Build 3D Game World if missing
            if (GameObject.Find("Office_Complex_Map") == null)
            {
                BuildEnvironment();
            }

            // 3. Build Player Survivor Model & Rig if missing
            if (GameObject.FindWithTag("Player") == null)
            {
                _playerRoot = BuildPlayerSurvivor(new Vector3(0f, 0.9f, -24f));
            }
            else
            {
                _playerRoot = GameObject.FindWithTag("Player");
            }

            _playerStats = _playerRoot.GetComponent<PlayerStats>();
            _playerAnimator = _playerRoot.GetComponent<ProceduralCharacterAnimator>();

            // 4. Build AI Villains if missing
            if (GameObject.Find("OfficeTyrant_AI") == null)
            {
                BuildAIVillain("OfficeTyrant_AI", "The Corporate Tyrant", new Vector3(10f, 0.9f, 15f), new Color(0.85f, 0.15f, 0.15f), 120f, 4.0f);
            }
            if (GameObject.Find("Backstabber_AI") == null)
            {
                BuildAIVillain("Backstabber_AI", "The Smiling Betrayer", new Vector3(-12f, 0.9f, 6f), new Color(0.6f, 0.15f, 0.85f), 90f, 4.8f);
            }

            // 5. Build Extraction Zone if missing
            if (GameObject.Find("ExtractionZone") == null)
            {
                BuildExtractionZone(new Vector3(0f, 0.1f, 32f));
            }

            // 6. Build World Loot Items if missing
            if (GameObject.Find("Loot_ExecutiveKeycard") == null)
            {
                BuildLoot(new Vector3(-16f, 0.6f, 5f), "ExecutiveKeycard", "Executive Keycard", new Color(0.95f, 0.75f, 0.1f));
                BuildLoot(new Vector3(14f, 0.6f, 30f), "ServerRoomKey", "Server Room Key", new Color(0.2f, 0.85f, 0.95f));
                BuildLoot(new Vector3(18f, 0.6f, -10f), "TraumaMedkit", "Emergency Trauma Medkit", new Color(0.2f, 0.95f, 0.3f));
                BuildLoot(new Vector3(-10f, 0.6f, 22f), "AdrenalineStim", "Tactical Adrenaline Injector", new Color(0.95f, 0.4f, 0.1f));
            }

            // 7. Build Complete Mobile UI & HUD Screen Canvas
            if (GameObject.Find("GameCanvas") == null)
            {
                BuildMobileUI();
            }

            // 8. Initialize Mission Manager
            if (MissionManager.Instance == null)
            {
                var mgrGo = new GameObject("MissionManager");
                mgrGo.AddComponent<MissionManager>();
            }

            Debug.Log("[RuntimeGameInitializer] RAGE HUNTERS production systems, models, screens, audio, and database loaded!");
        }

        private void Update()
        {
            if (!_matchActive) return;

            // 1. Update Match Clock
            _matchTimeRemaining = Mathf.Max(0f, _matchTimeRemaining - Time.deltaTime);
            int minutes = Mathf.FloorToInt(_matchTimeRemaining / 60f);
            int seconds = Mathf.FloorToInt(_matchTimeRemaining % 60f);
            if (_matchTimerText != null)
            {
                _matchTimerText.text = $"TIME: {minutes:00}:{seconds:00}";
            }

            // 2. Update Health & Stamina Bars dynamically
            if (_playerStats != null)
            {
                float hpRatio = _playerStats.HealthPercent;
                if (_healthFillRect != null)
                {
                    _healthFillRect.sizeDelta = new Vector2(370f * hpRatio, 24f);
                }
                if (_healthText != null)
                {
                    _healthText.text = $"HP: {Mathf.CeilToInt(_playerStats.CurrentHealth)} / {Mathf.CeilToInt(_playerStats.MaxHealth)}";
                }

                float stamRatio = _playerStats.StaminaPercent;
                if (_staminaFillRect != null)
                {
                    _staminaFillRect.sizeDelta = new Vector2(370f * stamRatio, 16f);
                }
                if (_staminaText != null)
                {
                    _staminaText.text = $"STAMINA: {Mathf.CeilToInt(_playerStats.CurrentStamina)}%";
                }

                // Heartbeat & Damage Vignette when low health
                if (hpRatio < 0.3f && _playerStats.IsAlive)
                {
                    SoundFXService.Instance?.SetHeartbeatActive(true, 1.4f);
                    if (_damageVignette != null)
                    {
                        float alpha = (Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f) * 0.35f;
                        _damageVignette.color = new Color(0.8f, 0f, 0f, alpha);
                    }
                }
                else
                {
                    SoundFXService.Instance?.SetHeartbeatActive(false);
                    if (_damageVignette != null)
                    {
                        _damageVignette.color = Color.clear;
                    }
                }

                // Player Defeat Check
                if (!_playerStats.IsAlive)
                {
                    TriggerMatchDefeat();
                }
            }

            // 3. Update Tactical Radar Compass to Extraction Beacon
            if (_playerRoot != null && _radarText != null)
            {
                var extract = GameObject.Find("ExtractionZone");
                if (extract != null)
                {
                    float dist = Vector3.Distance(_playerRoot.transform.position, extract.transform.position);
                    Vector3 forward = _playerRoot.transform.forward;
                    Vector3 toTarget = (extract.transform.position - _playerRoot.transform.position).normalized;
                    float angle = Vector3.SignedAngle(forward, toTarget, Vector3.up);

                    string dirTag = "FWD";
                    if (angle > 45f && angle < 135f) dirTag = "RIGHT";
                    else if (angle < -45f && angle > -135f) dirTag = "LEFT";
                    else if (Mathf.Abs(angle) >= 135f) dirTag = "BEHIND";

                    _radarText.text = $"BEACON: {Mathf.RoundToInt(dist)}m [{dirTag}]";
                }
            }
        }

        #region 3D World Construction

        private void BuildEnvironment()
        {
            var map = new GameObject("Office_Complex_Map");

            // Directional Light
            var lightGo = GameObject.Find("Directional Light");
            if (lightGo == null)
            {
                lightGo = new GameObject("Directional Light");
                var l = lightGo.AddComponent<UnityEngine.Light>();
                l.type = LightType.Directional;
                l.color = new Color(0.9f, 0.92f, 0.98f);
                l.intensity = 0.8f;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Ground Floor (70m x 80m)
            CreateCube("Floor", map.transform, new Vector3(0f, -0.25f, 5f), new Vector3(60f, 0.5f, 80f), new Color(0.18f, 0.20f, 0.22f));

            // Exterior Enclosure Walls (4m high)
            CreateCube("Wall_North", map.transform, new Vector3(0f, 2f, 45f), new Vector3(60f, 4f, 1f), new Color(0.25f, 0.28f, 0.32f));
            CreateCube("Wall_South", map.transform, new Vector3(0f, 2f, -35f), new Vector3(60f, 4f, 1f), new Color(0.25f, 0.28f, 0.32f));
            CreateCube("Wall_East", map.transform, new Vector3(30f, 2f, 5f), new Vector3(1f, 4f, 80f), new Color(0.25f, 0.28f, 0.32f));
            CreateCube("Wall_West", map.transform, new Vector3(-30f, 2f, 5f), new Vector3(1f, 4f, 80f), new Color(0.25f, 0.28f, 0.32f));

            // Interior Partitions & Corridor Walls
            CreateCube("Partition_Lobby", map.transform, new Vector3(-10f, 2f, -15f), new Vector3(20f, 4f, 0.5f), new Color(0.35f, 0.38f, 0.42f));
            CreateCube("Partition_Server", map.transform, new Vector3(12f, 2f, 15f), new Vector3(0.5f, 4f, 30f), new Color(0.35f, 0.38f, 0.42f));
            CreateCube("Partition_Offices", map.transform, new Vector3(-14f, 2f, 18f), new Vector3(18f, 4f, 0.5f), new Color(0.35f, 0.38f, 0.42f));

            // Office Cubicles & Desks
            for (int x = -20; x <= -6; x += 7)
            {
                for (int z = -8; z <= 8; z += 6)
                {
                    CreateCube($"Desk_{x}_{z}", map.transform, new Vector3(x, 0.4f, z), new Vector3(2.4f, 0.8f, 1.4f), new Color(0.45f, 0.35f, 0.25f));
                    CreateCube($"Screen_{x}_{z}", map.transform, new Vector3(x, 1.1f, z + 0.3f), new Vector3(0.9f, 0.6f, 0.1f), new Color(0.08f, 0.08f, 0.1f));
                }
            }

            // Server Room Racks (Glowing Blue LED Accents)
            for (int z = 8; z <= 28; z += 5)
            {
                var rack = CreateCube($"ServerRack_{z}", map.transform, new Vector3(20f, 1.5f, z), new Vector3(1.2f, 3.0f, 2.5f), new Color(0.12f, 0.14f, 0.16f));
                var led = CreateCube("LED_Blink", rack.transform, new Vector3(0.61f, 0.5f, 0f), new Vector3(0.05f, 1.8f, 2.0f), new Color(0.1f, 0.7f, 0.95f));
                var ledMat = led.GetComponent<MeshRenderer>()?.material;
                if (ledMat != null)
                {
                    ledMat.EnableKeyword("_EMISSION");
                    ledMat.SetColor("_EmissionColor", new Color(0.1f, 0.7f, 0.95f) * 2.0f);
                }
            }

            // Fluorescent Office Ceiling Tube Lights with Atmospheric Flickering
            var lightPositions = new Vector3[]
            {
                new Vector3(-12f, 3.6f, -10f),
                new Vector3(12f, 3.6f, -10f),
                new Vector3(-12f, 3.6f, 12f),
                new Vector3(12f, 3.6f, 12f),
                new Vector3(0f, 3.6f, 30f)
            };

            foreach (var pos in lightPositions)
            {
                var lampGo = new GameObject("CeilingLamp");
                lampGo.transform.SetParent(map.transform, false);
                lampGo.transform.position = pos;

                var fixture = CreateCube("Fixture", lampGo.transform, Vector3.zero, new Vector3(0.4f, 0.15f, 2.2f), new Color(0.9f, 0.9f, 0.95f));
                var ptLight = lampGo.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.range = 14f;
                ptLight.intensity = 1.1f;
                ptLight.color = new Color(0.95f, 0.96f, 1.0f);

                var flick = lampGo.AddComponent<ProceduralEnvironmentAnimator>();
                flick.Init(ProceduralEnvironmentAnimator.TargetType.FlickeringLight);
            }
        }

        #endregion

        #region Character & AI Construction

        private GameObject BuildPlayerSurvivor(Vector3 position)
        {
            var root = new GameObject("PlayerSurvivor");
            root.tag = "Player";
            root.transform.position = position;

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);

            // Torso (Survivor Tactical Jacket)
            CreateModelPart("Torso", model.transform, new Vector3(0f, 0.95f, 0f), new Vector3(0.6f, 0.7f, 0.35f), new Color(0.2f, 0.35f, 0.28f));

            // Head with Tactical HUD Visor
            var head = CreateModelPart("Head", model.transform, new Vector3(0f, 1.55f, 0f), new Vector3(0.35f, 0.35f, 0.35f), new Color(0.82f, 0.65f, 0.52f));
            var visor = CreateModelPart("Visor", head.transform, new Vector3(0f, 0.05f, 0.18f), new Vector3(0.28f, 0.1f, 0.05f), new Color(0.1f, 0.85f, 0.95f));
            var visorMat = visor.GetComponent<MeshRenderer>()?.material;
            if (visorMat != null)
            {
                visorMat.EnableKeyword("_EMISSION");
                visorMat.SetColor("_EmissionColor", new Color(0.1f, 0.85f, 0.95f) * 1.8f);
            }

            // Tactical Backpack
            CreateModelPart("Backpack", model.transform, new Vector3(0f, 1.0f, -0.25f), new Vector3(0.45f, 0.5f, 0.22f), new Color(0.15f, 0.18f, 0.15f));

            // Limbs
            CreateModelPart("Arm_L", model.transform, new Vector3(-0.42f, 0.95f, 0f), new Vector3(0.2f, 0.65f, 0.2f), new Color(0.2f, 0.35f, 0.28f));
            var armR = CreateModelPart("Arm_R", model.transform, new Vector3(0.42f, 0.95f, 0f), new Vector3(0.2f, 0.65f, 0.2f), new Color(0.2f, 0.35f, 0.28f));
            CreateModelPart("Leg_L", model.transform, new Vector3(-0.18f, 0.32f, 0f), new Vector3(0.22f, 0.7f, 0.25f), new Color(0.16f, 0.18f, 0.22f));
            CreateModelPart("Leg_R", model.transform, new Vector3(0.18f, 0.32f, 0f), new Vector3(0.22f, 0.7f, 0.25f), new Color(0.16f, 0.18f, 0.22f));

            // Tactical Melee Shock Baton
            CreateModelPart("Baton", armR.transform, new Vector3(0f, -0.35f, 0.25f), new Vector3(0.08f, 0.65f, 0.08f), new Color(0.85f, 0.2f, 0.2f));

            // Production Components
            root.AddComponent<PlayerStats>();
            root.AddComponent<PlayerController>();
            root.AddComponent<PlayerInventory>();
            root.AddComponent<HitBox>();
            root.AddComponent<MeleeCombat>();

            var anim = root.AddComponent<ProceduralCharacterAnimator>();
            anim.AutoDetectBones();

            // Setup Camera Tracking
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var tpCam = cam.GetComponent<ThirdPersonCamera>() ?? cam.gameObject.AddComponent<ThirdPersonCamera>();
                tpCam.SetTarget(root.transform);
            }

            return root;
        }

        public GameObject BuildAIVillain(string id, string villainName, Vector3 position, Color themeColor, float health, float speed)
        {
            var root = new GameObject(id);
            root.tag = "Enemy";
            root.transform.position = position;

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0f, 0.95f, 0f);

            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);

            // Villain Torso
            CreateModelPart("Torso", model.transform, new Vector3(0f, 1.0f, 0f), new Vector3(0.65f, 0.75f, 0.4f), themeColor);

            // Villain Head with Red Glowing Eyes
            var head = CreateModelPart("Head", model.transform, new Vector3(0f, 1.62f, 0f), new Vector3(0.38f, 0.38f, 0.38f), new Color(0.2f, 0.2f, 0.22f));
            var eyes = CreateModelPart("Eyes", head.transform, new Vector3(0f, 0.05f, 0.19f), new Vector3(0.3f, 0.08f, 0.08f), Color.red);
            var eyeMat = eyes.GetComponent<MeshRenderer>()?.material;
            if (eyeMat != null)
            {
                eyeMat.EnableKeyword("_EMISSION");
                eyeMat.SetColor("_EmissionColor", Color.red * 2.5f);
            }

            // Limbs
            CreateModelPart("Arm_L", model.transform, new Vector3(-0.45f, 1.0f, 0f), new Vector3(0.22f, 0.7f, 0.22f), themeColor * 0.8f);
            var armR = CreateModelPart("Arm_R", model.transform, new Vector3(0.45f, 1.0f, 0f), new Vector3(0.22f, 0.7f, 0.22f), themeColor * 0.8f);
            CreateModelPart("Leg_L", model.transform, new Vector3(-0.2f, 0.35f, 0f), new Vector3(0.25f, 0.75f, 0.28f), new Color(0.1f, 0.1f, 0.12f));
            CreateModelPart("Leg_R", model.transform, new Vector3(0.2f, 0.35f, 0f), new Vector3(0.25f, 0.75f, 0.28f), new Color(0.1f, 0.1f, 0.12f));

            // Threat Weapon
            CreateModelPart("Baton", armR.transform, new Vector3(0f, -0.4f, 0.25f), new Vector3(0.1f, 0.75f, 0.1f), new Color(0.15f, 0.15f, 0.18f));

            // Floating Nameplate
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            var textMesh = labelGo.AddComponent<TextMeshPro>();
            textMesh.text = $"[VILLAIN]\n{villainName}";
            textMesh.fontSize = 2.4f;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = themeColor;

            // Combat & AI Systems
            root.AddComponent<HitBox>();
            root.AddComponent<MeleeCombat>();

            var villainAnim = root.AddComponent<ProceduralCharacterAnimator>();
            villainAnim.AutoDetectBones();

            var ai = root.AddComponent<SimpleVillainAI>();
            ai.Init(villainName, themeColor, health, speed);

            return root;
        }

        private static GameObject CreateModelPart(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            Destroy(go.GetComponent<Collider>()); // Visual model part only

            var rend = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.3f);
            rend.material = mat;

            return go;
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;

            var rend = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.2f);
            rend.material = mat;

            return go;
        }

        #endregion

        #region Extraction & Loot

        private void BuildExtractionZone(Vector3 position)
        {
            var zone = new GameObject("ExtractionZone");
            zone.transform.position = position;

            // Concentric Base Pad
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "Beacon_Pad";
            pad.transform.SetParent(zone.transform, false);
            pad.transform.localPosition = Vector3.zero;
            pad.transform.localScale = new Vector3(6f, 0.1f, 6f);
            Destroy(pad.GetComponent<Collider>());

            var rend = pad.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.1f, 0.9f, 0.35f, 0.8f);
            rend.material = mat;

            // Holographic Vertical Light Column
            var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "Beacon_Beam";
            column.transform.SetParent(zone.transform, false);
            column.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            column.transform.localScale = new Vector3(1.8f, 2.5f, 1.8f);
            Destroy(column.GetComponent<Collider>());

            var colRend = column.GetComponent<MeshRenderer>();
            var colMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            colMat.color = new Color(0.2f, 0.95f, 0.4f, 0.45f);
            colRend.material = colMat;

            var beaconAnim = column.AddComponent<ProceduralEnvironmentAnimator>();
            beaconAnim.Init(ProceduralEnvironmentAnimator.TargetType.ExtractionBeacon);

            var trigger = zone.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 3.8f;

            zone.AddComponent<ExtractionPoint>();
        }

        private void BuildLoot(Vector3 position, string id, string name, Color color)
        {
            var loot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            loot.name = $"Loot_{id}";
            loot.tag = "Loot";
            loot.transform.position = position;
            loot.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var rend = loot.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.5f);
            rend.material = mat;

            var col = loot.GetComponent<BoxCollider>();
            col.isTrigger = true;

            var lootDrop = loot.AddComponent<LootDrop>();
            var lootAnim = loot.AddComponent<ProceduralEnvironmentAnimator>();
            lootAnim.Init(ProceduralEnvironmentAnimator.TargetType.FloatingLoot);
        }

        #endregion

        #region Production UI Screens & Mobile Touch Controls

        private void BuildMobileUI()
        {
            var canvasGo = new GameObject("GameCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Event System
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Damage Vignette Flash Image
            var vigGo = new GameObject("DamageVignette");
            vigGo.transform.SetParent(canvasGo.transform, false);
            var vigRt = vigGo.AddComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero;
            vigRt.anchorMax = Vector2.one;
            vigRt.offsetMin = Vector2.zero;
            vigRt.offsetMax = Vector2.zero;
            _damageVignette = vigGo.AddComponent<Image>();
            _damageVignette.color = Color.clear;
            _damageVignette.raycastTarget = false;

            // 1. In-Game HUD Panel
            _hudPanel = CreateUIContainer("HUDPanel", canvasGo.transform, false);
            BuildHUDContents(_hudPanel.transform);

            // 2. Touch Controls Overlay
            _touchControlsPanel = CreateUIContainer("TouchControls", canvasGo.transform, false);
            BuildTouchControls(_touchControlsPanel.transform);

            // 3. Tactical Backpack Screen Overlay
            _backpackPanel = CreateUIContainer("BackpackScreen", canvasGo.transform, false);
            BuildBackpackContents(_backpackPanel.transform);

            // 4. Mission Dossier Screen Overlay
            _dossierPanel = CreateUIContainer("DossierScreen", canvasGo.transform, false);
            BuildDossierContents(_dossierPanel.transform);

            // 5. Main Menu Screen Overlay
            _mainMenuPanel = CreateUIContainer("MainMenuScreen", canvasGo.transform, false);
            BuildMainMenuContents(_mainMenuPanel.transform);

            // 6. Custom Villain Studio Overlay
            _villainCreatorPanel = CreateUIContainer("VillainCreatorScreen", canvasGo.transform, false);
            BuildVillainCreatorContents(_villainCreatorPanel.transform);

            // 7. Database & Records Screen Overlay
            _databasePanel = CreateUIContainer("DatabaseScreen", canvasGo.transform, false);
            BuildDatabaseContents(_databasePanel.transform);

            // 8. Loadout & Equipment Screen Overlay
            _loadoutPanel = CreateUIContainer("LoadoutScreen", canvasGo.transform, false);
            BuildLoadoutContents(_loadoutPanel.transform);

            // 9. Settings Screen Overlay
            _settingsPanel = CreateUIContainer("SettingsScreen", canvasGo.transform, false);
            BuildSettingsContents(_settingsPanel.transform);

            // 10. Match Results Screen Overlay
            _matchResultsPanel = CreateUIContainer("MatchResultsScreen", canvasGo.transform, false);
            BuildMatchResultsContents(_matchResultsPanel.transform);

            // Top Menu & Action Quick Buttons
            CreateSimpleButton(canvasGo.transform, "Btn_MenuToggle", new Vector2(-100f, -60f), new Vector2(140f, 55f), "MENU", new Color(0.2f, 0.22f, 0.28f, 0.9f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                bool show = !_mainMenuPanel.activeSelf;
                CloseAllOverlayPanels();
                _mainMenuPanel.SetActive(show);
            }, anchorTopRight: true);

            CreateSimpleButton(canvasGo.transform, "Btn_DossierToggle", new Vector2(-250f, -60f), new Vector2(140f, 55f), "DOSSIER", new Color(0.18f, 0.35f, 0.45f, 0.9f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                bool show = !_dossierPanel.activeSelf;
                CloseAllOverlayPanels();
                _dossierPanel.SetActive(show);
            }, anchorTopRight: true);

            CreateSimpleButton(canvasGo.transform, "Btn_BackpackToggle", new Vector2(-400f, -60f), new Vector2(140f, 55f), "BACKPACK", new Color(0.45f, 0.35f, 0.15f, 0.9f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                bool show = !_backpackPanel.activeSelf;
                CloseAllOverlayPanels();
                _backpackPanel.SetActive(show);
            }, anchorTopRight: true);

            // 0. Screen 0: Production Animated Splash & Briefing Screen (Active on startup)
            _splashPanel = CreateUIContainer("SplashScreen", canvasGo.transform, true);
            BuildSplashContents(_splashPanel.transform);
        }

        public void CloseAllOverlayPanels()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_backpackPanel != null) _backpackPanel.SetActive(false);
            if (_dossierPanel != null) _dossierPanel.SetActive(false);
            if (_villainCreatorPanel != null) _villainCreatorPanel.SetActive(false);
            if (_databasePanel != null) _databasePanel.SetActive(false);
            if (_loadoutPanel != null) _loadoutPanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_matchResultsPanel != null) _matchResultsPanel.SetActive(false);
        }

        #endregion

        #region Screen 0: Splash & Mission Briefing

        private void BuildSplashContents(Transform parent)
        {
            var bg = CreatePanel("SplashBg", parent, Vector2.zero, Vector2.zero, new Color(0.04f, 0.05f, 0.07f, 0.98f), fullStretch: true);

            // Animated Title
            CreateText("GameTitle", bg.transform, new Vector2(0f, 320f), new Vector2(900f, 100f), "RAGE HUNTERS", 68, new Color(0.92f, 0.15f, 0.15f));
            CreateText("GameSub", bg.transform, new Vector2(0f, 250f), new Vector2(900f, 40f), "MOBILE MULTIPLAYER 3D TACTICAL EXTRACTION", 20, new Color(0.2f, 0.85f, 0.95f));

            // Mission Intel Dossier Box
            var box = CreatePanel("BriefingBox", bg.transform, new Vector2(0f, 60f), new Vector2(800f, 260f), new Color(0.1f, 0.12f, 0.16f, 0.9f));
            string briefing = "OPERATION SECTOR 4: ABANDONED OFFICE COMPLEX\n\n" +
                              "• PRIMARY OBJECTIVE: Recover executive keycards and reach Extraction Beacon Alpha.\n" +
                              "• ADVISORY: Corporate Tyrant and Smiling Betrayer patrol the floors with lethal aggression.\n" +
                              "• COMBAT DIRECTIVE: Engage using 3-hit melee combos. Manage your stamina carefully.\n" +
                              "• PERSISTENCE: All combat kills, loot, and custom villains persist to local GameDatabase.";
            CreateText("BriefingText", box.transform, Vector2.zero, new Vector2(760f, 230f), briefing, 17, new Color(0.9f, 0.92f, 0.95f));

            // Deployment Progress Bar
            var pBarBg = CreatePanel("PBarBg", bg.transform, new Vector2(0f, -130f), new Vector2(600f, 24f), new Color(0.15f, 0.18f, 0.22f));
            CreatePanel("PBarFill", pBarBg.transform, Vector2.zero, new Vector2(590f, 18f), new Color(0.2f, 0.85f, 0.45f));
            CreateText("PBarLabel", bg.transform, new Vector2(0f, -165f), new Vector2(400f, 30f), "SYSTEMS VERIFIED • READY FOR EXTRACTION", 14, new Color(0.7f, 0.75f, 0.8f));

            // Deploy Action Button
            CreateSimpleButton(bg.transform, "Btn_Deploy", new Vector2(0f, -250f), new Vector2(480f, 80f), "DEPLOY TO OFFICE COMPLEX", new Color(0.85f, 0.15f, 0.15f), () =>
            {
                SoundFXService.Play(SoundType.ExtractionSuccess);
                _splashPanel.SetActive(false);
                _hudPanel.SetActive(true);
                _touchControlsPanel.SetActive(true);
                _matchActive = true;
                TacticalVoiceComms.Instance?.CueMatchStart();
            });
        }

        #endregion

        #region Screen 1: In-Game HUD

        private void BuildHUDContents(Transform parent)
        {
            // Health Bar (Top Left)
            var healthBg = CreatePanel("HealthBg", parent, new Vector2(240f, -50f), new Vector2(380f, 32f), new Color(0.12f, 0.14f, 0.18f, 0.88f), anchorTopLeft: true);
            var hFill = CreatePanel("HealthFill", healthBg.transform, new Vector2(0f, 0f), new Vector2(370f, 24f), new Color(0.85f, 0.15f, 0.15f, 1f));
            _healthFillRect = hFill.GetComponent<RectTransform>();
            var hTextGo = CreateText("HP_Text", healthBg.transform, new Vector2(0f, 0f), new Vector2(300f, 30f), "HP: 100 / 100", 18, Color.white);
            _healthText = hTextGo.GetComponent<TextMeshProUGUI>();

            // Stamina Bar (Below Health)
            var stamBg = CreatePanel("StamBg", parent, new Vector2(240f, -90f), new Vector2(380f, 22f), new Color(0.12f, 0.14f, 0.18f, 0.88f), anchorTopLeft: true);
            var sFill = CreatePanel("StamFill", stamBg.transform, new Vector2(0f, 0f), new Vector2(370f, 16f), new Color(0.2f, 0.65f, 0.95f, 1f));
            _staminaFillRect = sFill.GetComponent<RectTransform>();
            var sTextGo = CreateText("STAM_Text", stamBg.transform, new Vector2(0f, 0f), new Vector2(300f, 20f), "STAMINA: 100%", 13, Color.white);
            _staminaText = sTextGo.GetComponent<TextMeshProUGUI>();

            // Tactical Objective Box (Top Center)
            var objBox = CreatePanel("ObjBox", parent, new Vector2(0f, -55f), new Vector2(520f, 65f), new Color(0.08f, 0.10f, 0.14f, 0.88f), anchorTopCenter: true);
            CreateText("ObjTitle", objBox.transform, new Vector2(0f, 14f), new Vector2(500f, 25f), "OBJECTIVE: LOCATE KEYCARDS & EXTRACT", 16, new Color(0.95f, 0.8f, 0.2f));
            CreateText("ObjDesc", objBox.transform, new Vector2(0f, -12f), new Vector2(500f, 25f), "Eliminate hostiles • Retrieve keycard • Hold extraction beacon", 13, Color.white);

            // Match Clock (Top Center)
            var timerGo = CreateText("MatchTimer", parent, new Vector2(0f, -15f), new Vector2(220f, 30f), "TIME: 10:00", 20, Color.white, anchorTopCenter: true);
            _matchTimerText = timerGo.GetComponent<TextMeshProUGUI>();

            // Tactical Radar & Distance Compass Widget (Top Right Area)
            var radarBg = CreatePanel("RadarBg", parent, new Vector2(-220f, -125f), new Vector2(280f, 40f), new Color(0.08f, 0.12f, 0.16f, 0.85f), anchorTopRight: true);
            var rTextGo = CreateText("RadarLabel", radarBg.transform, Vector2.zero, new Vector2(270f, 30f), "BEACON: SCANNING...", 15, new Color(0.2f, 0.95f, 0.45f));
            _radarText = rTextGo.GetComponent<TextMeshProUGUI>();
        }

        #endregion

        #region Screen 2: Mobile Touch Controls

        private void BuildTouchControls(Transform parent)
        {
            // Virtual Joystick (Bottom Left)
            var joyBg = CreatePanel("VirtualJoystick", parent, new Vector2(240f, 240f), new Vector2(220f, 220f), new Color(0.18f, 0.22f, 0.28f, 0.45f), anchorBottomLeft: true);
            var handle = CreatePanel("JoystickHandle", joyBg.transform, Vector2.zero, new Vector2(90f, 90f), new Color(0.85f, 0.2f, 0.2f, 0.85f));
            var vj = joyBg.AddComponent<VirtualJoystick>();
            vj.SetHandle(handle.GetComponent<RectTransform>());

            // Touch Camera Look Region (Right Screen Half)
            var camTouch = CreatePanel("TouchCameraRegion", parent, Vector2.zero, Vector2.zero, Color.clear);
            var rt = camTouch.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.35f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            camTouch.AddComponent<TouchCameraRegion>();

            // 1. ATTACK BUTTON (Drives 3-Hit Combo with damage and audio)
            CreateActionButton(parent, "Btn_Attack", new Vector2(-160f, 160f), "ATTACK", new Color(0.85f, 0.15f, 0.15f), () =>
            {
                if (_playerRoot == null) return;

                int comboStep = 1;
                if (_playerAnimator != null)
                {
                    comboStep = _playerAnimator.TriggerAttackAnimation();
                }

                // Determine base damage based on combo step
                float damage = (comboStep == 3) ? 75f : (comboStep == 2 ? 45f : 30f);
                bool isCrit = (comboStep == 3) || (UnityEngine.Random.value < 0.25f);
                if (isCrit && comboStep != 3) damage *= 1.5f;

                // Hit Detection
                Collider[] hits = Physics.OverlapSphere(_playerRoot.transform.position + _playerRoot.transform.forward * 1.5f, 1.9f);
                bool hitTarget = false;

                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Enemy") && hit.gameObject != _playerRoot)
                    {
                        hitTarget = true;
                        _totalDamageDealt += Mathf.RoundToInt(damage);

                        // Spawn damage number & sound
                        DamageNumberSpawner.SpawnDamageNumber(hit.transform.position + Vector3.up * 1.5f, damage, isCrit);
                        SoundFXService.Play(SoundType.HitImpact, 0.9f);

                        // Flinch animation & AI health deduction
                        hit.GetComponent<ProceduralCharacterAnimator>()?.TriggerFlinch();
                        hit.GetComponent<SimpleVillainAI>()?.TakeDamage(damage);
                        break;
                    }
                }
            });

            // 2. SPRINT BUTTON
            CreateActionButton(parent, "Btn_Sprint", new Vector2(-280f, 110f), "SPRINT", new Color(0.25f, 0.55f, 0.85f), () =>
            {
                SoundFXService.Play(SoundType.UIClick, 0.4f);
                if (_playerStats != null)
                {
                    _playerStats.SetSprinting(!_playerStats.CanSprint ? false : true);
                }
            });

            // 3. JUMP BUTTON
            CreateActionButton(parent, "Btn_Jump", new Vector2(-110f, 280f), "JUMP", new Color(0.35f, 0.75f, 0.35f), () =>
            {
                if (_playerAnimator != null) _playerAnimator.TriggerJump();
            });

            // 4. CROUCH SNEAK BUTTON
            CreateActionButton(parent, "Btn_Crouch", new Vector2(-230f, 230f), "CROUCH", new Color(0.55f, 0.55f, 0.6f), () =>
            {
                SoundFXService.Play(SoundType.UIClick, 0.4f);
                if (_playerAnimator != null)
                {
                    _playerAnimator.SetCrouching(!_playerAnimator.IsCrouching);
                }
            });

            // 5. INTERACT BUTTON (Loots, operates extraction beacon)
            CreateActionButton(parent, "Btn_Interact", new Vector2(-360f, 220f), "INTERACT", new Color(0.95f, 0.75f, 0.15f), () =>
            {
                if (_playerRoot == null) return;

                // Check distance to extraction beacon
                var extract = GameObject.Find("ExtractionZone");
                if (extract != null && Vector3.Distance(_playerRoot.transform.position, extract.transform.position) < 4.2f)
                {
                    TriggerMatchVictory();
                    return;
                }

                // Check distance to loot objects
                var loots = GameObject.FindGameObjectsWithTag("Loot");
                foreach (var l in loots)
                {
                    if (Vector3.Distance(_playerRoot.transform.position, l.transform.position) < 2.8f)
                    {
                        SoundFXService.Play(SoundType.LootPickup);
                        SoundFXService.Play(SoundType.KeycardSwipe);
                        DamageNumberSpawner.SpawnDamageNumber(l.transform.position + Vector3.up * 0.8f, 100f, false);
                        TacticalVoiceComms.Transmit("HQ DISPATCH", $"Recovered: [{l.name.Replace("Loot_", "")}]! Secure for extraction.", SoundType.Voice_MatchStart);
                        Destroy(l);
                        GameDatabase.Instance?.UpdatePlayerProfile(addXP: 100, addCredits: 75);
                        return;
                    }
                }

                SoundFXService.Play(SoundType.UIClick, 0.3f);
            });

            parent.gameObject.AddComponent<TouchInputManager>();
        }

        #endregion

        #region Screen 3: Tactical Backpack & Inventory

        private void BuildBackpackContents(Transform parent)
        {
            var bg = CreatePanel("BackpackBg", parent, Vector2.zero, Vector2.zero, new Color(0.06f, 0.08f, 0.11f, 0.98f), fullStretch: true);

            CreateText("BPTitle", bg.transform, new Vector2(0f, 380f), new Vector2(800f, 60f), "TACTICAL SURVIVOR BACKPACK", 40, new Color(0.95f, 0.75f, 0.2f));
            CreateText("BPCapacity", bg.transform, new Vector2(0f, 330f), new Vector2(800f, 40f), "CAPACITY: 4 / 8 SLOTS UTILIZED", 18, Color.white);

            var items = new (string Name, string Type, Color Color, Action OnUse)[]
            {
                ("Trauma Medkit", "Consumable (+50 HP)", new Color(0.2f, 0.8f, 0.3f), () =>
                {
                    SoundFXService.Play(SoundType.MedkitUse);
                    if (_playerStats != null)
                    {
                        _playerStats.TakeDamage(-50f, Vector3.zero, null); // Heal
                        DamageNumberSpawner.SpawnDamageNumber(_playerRoot.transform.position + Vector3.up * 1.5f, 50f, false);
                    }
                }),
                ("Tactical Adrenaline", "Stimulant (+100 Stamina)", new Color(0.95f, 0.45f, 0.15f), () =>
                {
                    SoundFXService.Play(SoundType.MedkitUse);
                    if (_playerStats != null)
                    {
                        _playerStats.SetSprinting(false);
                    }
                }),
                ("Executive Keycard", "Mission Intel (Vault Access)", new Color(0.95f, 0.8f, 0.1f), () =>
                {
                    SoundFXService.Play(SoundType.KeycardSwipe);
                }),
                ("Shock Baton Alpha", "Equipped Melee Weapon", new Color(0.2f, 0.75f, 0.95f), () =>
                {
                    SoundFXService.Play(SoundType.AttackSwing);
                })
            };

            for (int i = 0; i < items.Length; i++)
            {
                int idx = i;
                float y = 210f - (i * 90f);
                var row = CreatePanel($"Slot_{i}", bg.transform, new Vector2(0f, y), new Vector2(700f, 75f), new Color(0.12f, 0.15f, 0.2f, 0.9f));
                CreateText($"Name_{i}", row.transform, new Vector2(-150f, 0f), new Vector2(340f, 40f), $"{items[idx].Name}\n<size=12>{items[idx].Type}</size>", 17, items[idx].Color);

                CreateSimpleButton(row.transform, $"Btn_Use_{i}", new Vector2(230f, 0f), new Vector2(160f, 50f), "USE ITEM", items[idx].Color * 0.8f, () =>
                {
                    items[idx].OnUse();
                });
            }

            CreateSimpleButton(bg.transform, "Btn_BPClose", new Vector2(0f, -220f), new Vector2(300f, 60f), "CLOSE BACKPACK", new Color(0.3f, 0.3f, 0.35f), () =>
            {
                SoundFXService.Play(SoundType.UIBack);
                CloseAllOverlayPanels();
            });
        }

        #endregion

        #region Screen 4: Mission Dossier & Objectives

        private void BuildDossierContents(Transform parent)
        {
            var bg = CreatePanel("DossierBg", parent, Vector2.zero, Vector2.zero, new Color(0.06f, 0.08f, 0.11f, 0.98f), fullStretch: true);

            CreateText("DTitle", bg.transform, new Vector2(0f, 380f), new Vector2(800f, 60f), "MISSION DOSSIER // SECTOR 4", 40, new Color(0.2f, 0.85f, 0.95f));

            var infoBox = CreatePanel("DInfo", bg.transform, new Vector2(0f, 120f), new Vector2(760f, 380f), new Color(0.1f, 0.12f, 0.16f, 0.9f));
            string dossier = "ACTIVE DIRECTIVES:\n\n" +
                             "[✓] Infiltrate Abandoned Office Complex\n" +
                             "[ ] Neutralize The Corporate Tyrant (Bounty: +200 XP)\n" +
                             "[ ] Neutralize The Smiling Betrayer (Bounty: +150 XP)\n" +
                             "[ ] Recover Executive Keycards\n" +
                             "[ ] Transmit Beacon Signal & Extract\n\n" +
                             $"LIVE COMBAT STATISTICS:\n" +
                             $"• Damage Dealt: {_totalDamageDealt} HP\n" +
                             $"• Hostiles Neutralized: {_enemiesSlain}\n" +
                             $"• Time Remaining: {Mathf.CeilToInt(_matchTimeRemaining)}s";
            CreateText("DText", infoBox.transform, Vector2.zero, new Vector2(720f, 340f), dossier, 18, Color.white);

            CreateSimpleButton(bg.transform, "Btn_DResume", new Vector2(-160f, -200f), new Vector2(280f, 60f), "RESUME MISSION", new Color(0.2f, 0.65f, 0.35f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                CloseAllOverlayPanels();
            });

            CreateSimpleButton(bg.transform, "Btn_DMenu", new Vector2(160f, -200f), new Vector2(280f, 60f), "MAIN MENU", new Color(0.3f, 0.3f, 0.35f), () =>
            {
                SoundFXService.Play(SoundType.UIBack);
                CloseAllOverlayPanels();
                _mainMenuPanel.SetActive(true);
            });
        }

        #endregion

        #region Screen 5: Main Menu Screen

        private void BuildMainMenuContents(Transform parent)
        {
            var bg = CreatePanel("MenuBg", parent, Vector2.zero, Vector2.zero, new Color(0.06f, 0.08f, 0.10f, 0.96f), fullStretch: true);

            // Title
            CreateText("GameTitle", bg.transform, new Vector2(0f, 320f), new Vector2(800f, 100f), "RAGE HUNTERS", 64, new Color(0.9f, 0.15f, 0.15f));
            CreateText("GameSubtitle", bg.transform, new Vector2(0f, 260f), new Vector2(800f, 40f), "3D MULTIPLAYER SURVIVAL - MOBILE", 20, new Color(0.7f, 0.75f, 0.8f));

            // Menu Buttons
            CreateSimpleButton(bg.transform, "Btn_PlaySolo", new Vector2(0f, 150f), new Vector2(420f, 70f), "RESUME PLAYING", new Color(0.85f, 0.15f, 0.15f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                parent.gameObject.SetActive(false);
            });

            CreateSimpleButton(bg.transform, "Btn_VillainCreator", new Vector2(0f, 65f), new Vector2(420f, 70f), "CUSTOM VILLAIN STUDIO", new Color(0.2f, 0.35f, 0.55f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                CloseAllOverlayPanels();
                _villainCreatorPanel.SetActive(true);
            });

            CreateSimpleButton(bg.transform, "Btn_Loadout", new Vector2(0f, -20f), new Vector2(420f, 70f), "LOADOUT & EQUIPMENT", new Color(0.45f, 0.35f, 0.2f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                CloseAllOverlayPanels();
                _loadoutPanel.SetActive(true);
            });

            CreateSimpleButton(bg.transform, "Btn_Database", new Vector2(0f, -105f), new Vector2(420f, 70f), "LOCAL DATABASE & STATS", new Color(0.2f, 0.45f, 0.3f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                CloseAllOverlayPanels();
                _databasePanel.SetActive(true);
            });

            CreateSimpleButton(bg.transform, "Btn_Settings", new Vector2(0f, -190f), new Vector2(420f, 70f), "SETTINGS", new Color(0.25f, 0.25f, 0.30f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                CloseAllOverlayPanels();
                _settingsPanel.SetActive(true);
            });
        }

        #endregion

        #region Screen 6: Custom Villain Studio

        private void BuildVillainCreatorContents(Transform parent)
        {
            var bg = CreatePanel("VCBg", parent, Vector2.zero, Vector2.zero, new Color(0.08f, 0.09f, 0.12f, 0.98f), fullStretch: true);

            CreateText("VCTitle", bg.transform, new Vector2(0f, 380f), new Vector2(800f, 60f), "CUSTOM VILLAIN STUDIO", 40, new Color(0.9f, 0.15f, 0.15f));
            CreateText("VCDesc", bg.transform, new Vector2(0f, 330f), new Vector2(800f, 40f), "Design fictional rival AI villains, persist to database, or deploy directly into match", 18, Color.white);

            var archetypes = new (string Name, string Archetype, float HP, float Speed, Color Color)[]
            {
                ("The Office Tyrant", "Corporate Tank", 130f, 3.8f, new Color(0.85f, 0.15f, 0.15f)),
                ("The Smiling Betrayer", "Shadow Stalker", 90f, 5.2f, new Color(0.6f, 0.15f, 0.85f)),
                ("The Gaslighting Mastermind", "Tactician", 105f, 4.4f, new Color(0.15f, 0.75f, 0.85f)),
                ("The Apex Berserker", "Rival Hunter", 160f, 4.6f, new Color(0.95f, 0.5f, 0.1f))
            };

            for (int i = 0; i < archetypes.Length; i++)
            {
                int index = i;
                float y = 200f - (i * 90f);
                var arch = archetypes[index];

                CreateSimpleButton(bg.transform, $"Btn_Arch_{i}", new Vector2(-150f, y), new Vector2(420f, 70f), $"SAVE: {arch.Name}", arch.Color, () =>
                {
                    SoundFXService.Play(SoundType.ObjectiveComplete);
                    var v = new VillainRecord
                    {
                        Id = "vil_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                        Name = arch.Name,
                        Archetype = arch.Archetype,
                        Difficulty = "Hard",
                        Aggressiveness = 0.85f,
                        Stealthiness = 0.6f,
                        Intelligence = 0.75f,
                        BaseHealth = arch.HP,
                        MoveSpeed = arch.Speed,
                        Backstory = "Custom designed villain created in player studio.",
                        ModelColorHex = "#" + ColorUtility.ToHtmlStringRGB(arch.Color)
                    };
                    GameDatabase.Instance?.InsertVillain(v);
                    TacticalVoiceComms.Transmit("STUDIO ARCHIVE", $"Saved [{v.Name}] to local GameDatabase!", SoundType.Voice_MatchStart);
                });

                CreateSimpleButton(bg.transform, $"Btn_Spawn_{i}", new Vector2(230f, y), new Vector2(240f, 70f), "SPAWN IN MATCH", new Color(0.2f, 0.55f, 0.35f), () =>
                {
                    SoundFXService.Play(SoundType.VillainAlertStinger);
                    Vector3 spawnPos = (_playerRoot != null) ? _playerRoot.transform.position + _playerRoot.transform.forward * 12f : new Vector3(0f, 0.9f, 0f);
                    BuildAIVillain($"Custom_{arch.Name.Replace(" ", "")}", arch.Name, spawnPos, arch.Color, arch.HP, arch.Speed);
                    TacticalVoiceComms.Transmit("TACTICAL SENSOR", $"ALERT: [{arch.Name}] has deployed into the office complex!", SoundType.Voice_ThreatDetected);
                    CloseAllOverlayPanels();
                });
            }

            CreateSimpleButton(bg.transform, "Btn_VCBack", new Vector2(0f, -220f), new Vector2(300f, 60f), "BACK TO MENU", new Color(0.3f, 0.3f, 0.35f), () =>
            {
                SoundFXService.Play(SoundType.UIBack);
                CloseAllOverlayPanels();
                _mainMenuPanel.SetActive(true);
            });
        }

        #endregion

        #region Screen 7: Local Database & Records Inspector

        private void BuildDatabaseContents(Transform parent)
        {
            var bg = CreatePanel("DBBg", parent, Vector2.zero, Vector2.zero, new Color(0.06f, 0.08f, 0.10f, 0.98f), fullStretch: true);

            CreateText("DBTitle", bg.transform, new Vector2(0f, 380f), new Vector2(800f, 60f), "LOCAL DATABASE VIEWER (JSON PERSISTENT)", 38, new Color(0.2f, 0.85f, 0.45f));

            // Profile Record Display
            var p = GameDatabase.Instance?.Profile;
            string profileInfo = p != null
                ? $"PLAYER: {p.PlayerName} | LEVEL: {p.Level} | XP: {p.TotalXP} | CREDITS: {p.Credits}\nMATCHES PLAYED: {p.MatchesPlayed} | WINS: {p.MatchesWon} | KILLS: {p.Kills}"
                : "Database loading...";
            CreateText("DBProfile", bg.transform, new Vector2(0f, 270f), new Vector2(800f, 80f), profileInfo, 20, Color.white);

            // Database Villains Table
            var villains = GameDatabase.Instance?.Villains;
            string villainList = "SAVED VILLAINS IN DATABASE:\n";
            if (villains != null)
            {
                foreach (var v in villains)
                {
                    villainList += $"• {v.Name} ({v.Archetype} - {v.Difficulty}) | HP: {v.BaseHealth}\n";
                }
            }
            CreateText("DBVillains", bg.transform, new Vector2(0f, 80f), new Vector2(800f, 220f), villainList, 18, new Color(0.85f, 0.85f, 0.9f));

            CreateSimpleButton(bg.transform, "Btn_ResetDB", new Vector2(-180f, -180f), new Vector2(300f, 60f), "RESET DATABASE", new Color(0.7f, 0.2f, 0.2f), () =>
            {
                SoundFXService.Play(SoundType.DamageTaken);
                GameDatabase.Instance?.ResetDatabase();
                CloseAllOverlayPanels();
                _databasePanel.SetActive(true);
            });

            CreateSimpleButton(bg.transform, "Btn_DBBack", new Vector2(180f, -180f), new Vector2(300f, 60f), "BACK TO MENU", new Color(0.3f, 0.3f, 0.35f), () =>
            {
                SoundFXService.Play(SoundType.UIBack);
                CloseAllOverlayPanels();
                _mainMenuPanel.SetActive(true);
            });
        }

        #endregion

        #region Screen 8: Loadout & Equipment

        private void BuildLoadoutContents(Transform parent)
        {
            var bg = CreatePanel("LoadoutBg", parent, Vector2.zero, Vector2.zero, new Color(0.07f, 0.08f, 0.11f, 0.98f), fullStretch: true);

            CreateText("LTitle", bg.transform, new Vector2(0f, 380f), new Vector2(800f, 60f), "LOADOUT & EQUIPMENT", 40, new Color(0.95f, 0.75f, 0.2f));
            CreateText("LDesc", bg.transform, new Vector2(0f, 330f), new Vector2(800f, 40f), "Select weapons, body armor, and tactical supplies", 18, Color.white);

            var weapons = new string[]
            {
                "Tactical Shock Baton [Damage: 35 | Speed: Fast | EQUIPPED]",
                "Heavy Steel Pipe [Damage: 55 | Speed: Medium]",
                "Stun Rod Alpha [Damage: 40 | Crit Chance: +20%]"
            };

            for (int i = 0; i < weapons.Length; i++)
            {
                CreateSimpleButton(bg.transform, $"Btn_Wpn_{i}", new Vector2(0f, 210f - (i * 90f)), new Vector2(560f, 72f), weapons[i], new Color(0.35f, 0.28f, 0.15f), () =>
                {
                    SoundFXService.Play(SoundType.LootPickup);
                });
            }

            CreateSimpleButton(bg.transform, "Btn_LBack", new Vector2(0f, -200f), new Vector2(300f, 60f), "BACK TO MENU", new Color(0.3f, 0.3f, 0.35f), () =>
            {
                SoundFXService.Play(SoundType.UIBack);
                CloseAllOverlayPanels();
                _mainMenuPanel.SetActive(true);
            });
        }

        #endregion

        #region Screen 9: Settings

        private void BuildSettingsContents(Transform parent)
        {
            var bg = CreatePanel("SettingsBg", parent, Vector2.zero, Vector2.zero, new Color(0.07f, 0.08f, 0.11f, 0.98f), fullStretch: true);

            CreateText("STitle", bg.transform, new Vector2(0f, 380f), new Vector2(800f, 60f), "GAME SETTINGS", 40, Color.white);

            // Audio Controls
            CreateText("AudioHeading", bg.transform, new Vector2(0f, 280f), new Vector2(600f, 40f), "AUDIO VOLUME", 24, new Color(0.2f, 0.85f, 0.95f));
            CreateSimpleButton(bg.transform, "Btn_VolMaster", new Vector2(-150f, 210f), new Vector2(240f, 60f), "MASTER: 100%", new Color(0.2f, 0.35f, 0.45f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
            });
            CreateSimpleButton(bg.transform, "Btn_VolSFX", new Vector2(150f, 210f), new Vector2(240f, 60f), "SFX & VOX: 100%", new Color(0.2f, 0.35f, 0.45f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
            });

            // Graphics Controls
            CreateText("GfxHeading", bg.transform, new Vector2(0f, 110f), new Vector2(600f, 40f), "TARGET FRAMERATE", 24, new Color(0.2f, 0.85f, 0.95f));
            CreateSimpleButton(bg.transform, "Btn_GfxHigh", new Vector2(0f, 45f), new Vector2(380f, 60f), "PERFORMANCE: 60 FPS", new Color(0.2f, 0.5f, 0.35f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                Application.targetFrameRate = 60;
            });

            // Sensitivity
            CreateText("SensHeading", bg.transform, new Vector2(0f, -50f), new Vector2(600f, 40f), "TOUCH SENSITIVITY", 24, new Color(0.2f, 0.85f, 0.95f));
            CreateSimpleButton(bg.transform, "Btn_Sens", new Vector2(0f, -110f), new Vector2(380f, 60f), "SENSITIVITY: 1.5x", new Color(0.35f, 0.3f, 0.45f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
            });

            CreateSimpleButton(bg.transform, "Btn_SetBack", new Vector2(0f, -220f), new Vector2(300f, 60f), "BACK TO MENU", new Color(0.3f, 0.3f, 0.35f), () =>
            {
                SoundFXService.Play(SoundType.UIBack);
                CloseAllOverlayPanels();
                _mainMenuPanel.SetActive(true);
            });
        }

        #endregion

        #region Screen 10: Match Results (Victory / Defeat)

        private void BuildMatchResultsContents(Transform parent)
        {
            var bg = CreatePanel("MRBg", parent, Vector2.zero, Vector2.zero, new Color(0.06f, 0.08f, 0.10f, 0.98f), fullStretch: true);
            CreateText("MRTitle", bg.transform, new Vector2(0f, 220f), new Vector2(800f, 80f), "EXTRACTION COMPLETE", 52, new Color(0.2f, 0.85f, 0.35f));
            CreateText("MRSubtitle", bg.transform, new Vector2(0f, 150f), new Vector2(800f, 40f), "SURVIVOR STATUS: EXTRACTED", 24, Color.white);

            var rewardBox = CreatePanel("RewardBox", bg.transform, new Vector2(0f, 30f), new Vector2(650f, 160f), new Color(0.12f, 0.15f, 0.2f, 0.9f));
            string rewardInfo = "+350 XP AWARDED TO DATABASE PROFILE\n" +
                                "+150 CREDITS SECURED\n" +
                                "PERFORMANCE RATING: S-RANK SURVIVOR";
            CreateText("MRReward", rewardBox.transform, Vector2.zero, new Vector2(600f, 140f), rewardInfo, 20, new Color(0.95f, 0.85f, 0.3f));

            CreateSimpleButton(bg.transform, "Btn_MRContinue", new Vector2(-150f, -130f), new Vector2(280f, 65f), "MAIN MENU", new Color(0.85f, 0.15f, 0.15f), () =>
            {
                SoundFXService.Play(SoundType.UIClick);
                CloseAllOverlayPanels();
                _mainMenuPanel.SetActive(true);
            });

            CreateSimpleButton(bg.transform, "Btn_MRRedeploy", new Vector2(150f, -130f), new Vector2(280f, 65f), "REDEPLOY", new Color(0.2f, 0.65f, 0.35f), () =>
            {
                SoundFXService.Play(SoundType.ExtractionSuccess);
                CloseAllOverlayPanels();
                _splashPanel.SetActive(true);
            });
        }

        public void TriggerMatchVictory()
        {
            _matchActive = false;
            SoundFXService.Play(SoundType.ExtractionSuccess);
            TacticalVoiceComms.Instance?.CueExtractionSuccess();

            GameDatabase.Instance?.AddMatchRecord(new MatchRecord
            {
                GameMode = "Solo Survival",
                MapName = "Abandoned Office Complex",
                Outcome = "Extracted",
                DurationSeconds = 600f - _matchTimeRemaining,
                Kills = _enemiesSlain,
                XPEarned = 350,
                Extracted = true
            });
            GameDatabase.Instance?.UpdatePlayerProfile(addXP: 350, matchWon: true, addCredits: 150);

            CloseAllOverlayPanels();
            _matchResultsPanel.SetActive(true);
        }

        public void TriggerMatchDefeat()
        {
            if (!_matchActive) return;
            _matchActive = false;

            _playerAnimator?.TriggerDeathCollapse();
            SoundFXService.Play(SoundType.Voice_VillainLaugh);
            TacticalVoiceComms.Transmit("HQ DISPATCH", "SIGNAL LOST: Survivor has been neutralized in sector.", SoundType.Voice_ThreatDetected);

            GameDatabase.Instance?.AddMatchRecord(new MatchRecord
            {
                GameMode = "Solo Survival",
                MapName = "Abandoned Office Complex",
                Outcome = "Defeated",
                DurationSeconds = 600f - _matchTimeRemaining,
                Kills = _enemiesSlain,
                XPEarned = 50,
                Extracted = false
            });
            GameDatabase.Instance?.UpdatePlayerProfile(addXP: 50, matchWon: false);

            StartCoroutine(ShowDefeatScreenDelayed());
        }

        private IEnumerator ShowDefeatScreenDelayed()
        {
            yield return new WaitForSeconds(2.0f);
            CloseAllOverlayPanels();
            _matchResultsPanel.SetActive(true);

            // Change Results title to Defeat styling
            var title = _matchResultsPanel.transform.Find("MRBg/MRTitle")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                title.text = "SIGNAL LOST // KILLED IN ACTION";
                title.color = new Color(0.9f, 0.15f, 0.15f);
            }
            var sub = _matchResultsPanel.transform.Find("MRBg/MRSubtitle")?.GetComponent<TextMeshProUGUI>();
            if (sub != null)
            {
                sub.text = "SURVIVOR STATUS: ELIMINATED BY CORPORATE TYRANT";
            }
        }

        #endregion

        #region UI Component Helpers

        private GameObject CreateUIContainer(string name, Transform parent, bool active)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.SetActive(active);
            return go;
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPos, Vector2 size, Color color, bool fullStretch = false, bool anchorTopLeft = false, bool anchorTopCenter = false, bool anchorTopRight = false, bool anchorBottomLeft = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            if (fullStretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else if (anchorTopLeft)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;
            }
            else if (anchorTopCenter)
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;
            }
            else if (anchorTopRight)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;
            }
            else if (anchorBottomLeft)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;
            }
            else
            {
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;
            }

            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private GameObject CreateText(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color color, bool anchorTopCenter = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            if (anchorTopCenter)
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
            }

            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return go;
        }

        private GameObject CreateSimpleButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, Color color, Action onClick, bool anchorTopRight = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            if (anchorTopRight)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
            }

            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;

            var btn = go.AddComponent<Button>();
            if (onClick != null)
            {
                btn.onClick.AddListener(() => onClick());
            }

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 17;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        private GameObject CreateActionButton(Transform parent, string name, Vector2 pos, string label, Color color, Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(95f, 95f);

            var img = go.AddComponent<Image>();
            img.color = color;

            var tb = go.AddComponent<TouchButton>();
            if (onClick != null)
            {
                tb.OnPressed.AddListener(() => onClick());
            }

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        #endregion
    }

    /// <summary>
    /// Autonomous runtime villain behavior.
    /// Drives patrol routes and chases player when spotted with real combat,
    /// animated melee strikes, threat roar barks, health deduction, and death collapse kinematics.
    /// </summary>
    public class SimpleVillainAI : MonoBehaviour
    {
        private string _villainName;
        private Color _themeColor;
        private Transform _player;
        private float _health = 120f;
        private float _maxHealth = 120f;
        private float _patrolSpeed = 3.2f;
        private float _chaseSpeed = 4.8f;
        private float _detectionRange = 15f;
        private Vector3 _startPosition;
        private Vector3 _patrolTarget;
        private float _patrolTimer;
        private float _attackCooldown;
        private bool _hasAlerted;

        public void Init(string villainName, Color themeColor, float health, float speed)
        {
            _villainName = villainName;
            _themeColor = themeColor;
            _health = health;
            _maxHealth = health;
            _chaseSpeed = speed;
            _patrolSpeed = speed * 0.7f;
            _startPosition = transform.position;
            _patrolTarget = _startPosition + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
        }

        private void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
        }

        private void Update()
        {
            if (_health <= 0f) return;

            if (_player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) _player = p.transform;
                return;
            }

            _attackCooldown -= Time.deltaTime;
            float distToPlayer = Vector3.Distance(transform.position, _player.position);

            if (distToPlayer <= _detectionRange)
            {
                if (!_hasAlerted)
                {
                    _hasAlerted = true;
                    GetComponent<ProceduralCharacterAnimator>()?.TriggerVillainRoar();
                    TacticalVoiceComms.Instance?.CueThreatSpotted(_villainName);
                }

                // Chase Player
                Vector3 dir = (_player.position - transform.position).normalized;
                dir.y = 0f;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);

                if (distToPlayer > 1.8f)
                {
                    transform.position += dir * (_chaseSpeed * Time.deltaTime);
                }
                else
                {
                    // Attack strike
                    if (_attackCooldown <= 0f)
                    {
                        _attackCooldown = 1.35f;
                        GetComponent<ProceduralCharacterAnimator>()?.TriggerAttackAnimation();
                        SoundFXService.Play(SoundType.HitImpact, 0.85f);
                        _player.GetComponent<ProceduralCharacterAnimator>()?.TriggerFlinch();
                        _player.GetComponent<PlayerStats>()?.TakeDamage(18f, transform.position, gameObject);
                        DamageNumberSpawner.SpawnDamageNumber(_player.position + Vector3.up * 1.5f, 18f, false);
                    }
                }
            }
            else
            {
                _hasAlerted = false;

                // Patrol Area
                _patrolTimer += Time.deltaTime;
                if (_patrolTimer > 5f || Vector3.Distance(transform.position, _patrolTarget) < 1.0f)
                {
                    _patrolTimer = 0f;
                    _patrolTarget = _startPosition + new Vector3(UnityEngine.Random.Range(-10f, 10f), 0f, UnityEngine.Random.Range(-10f, 10f));
                }

                Vector3 pDir = (_patrolTarget - transform.position).normalized;
                pDir.y = 0f;
                if (pDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pDir), 4f * Time.deltaTime);
                    transform.position += pDir * (_patrolSpeed * Time.deltaTime);
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (_health <= 0f) return;

            _health -= amount;

            if (_health <= 0f)
            {
                _health = 0f;
                var anim = GetComponent<ProceduralCharacterAnimator>();
                anim?.TriggerDeathCollapse();
                DamageNumberSpawner.SpawnDamageNumber(transform.position + Vector3.up * 2f, 150f, true);
                GameDatabase.Instance?.UpdatePlayerProfile(addXP: 150, kills: 1, addCredits: 100);
                TacticalVoiceComms.Transmit("HQ DISPATCH", $"Confirmed: Hostile [{_villainName}] neutralized! +150 XP.", SoundType.Voice_MatchStart);
                Destroy(gameObject, 1.8f); // Delay destroy for collapse kinematics
            }
        }
    }
}
