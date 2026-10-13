using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeSouls
{
    /// <summary>
    /// Entry point. The whole game is assembled from code, so it runs from any (even empty) scene:
    /// press Play and the island, camps, player, camera and HUD are generated.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game I;
        public PlayerAvatar player;
        public ThirdPersonCamera cam;
        public WorldGenerator world;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<Game>() != null) return;
            new GameObject("AnimeSoulsGame").AddComponent<Game>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 120;

            DisableSceneDefaults();
            SetupLighting();

            world = new GameObject("World").AddComponent<WorldGenerator>();
            world.Build();

            SpawnPlayer();
            SetupCamera();

            var hud = new GameObject("HUD").AddComponent<HUD>();
            hud.Bind(player);
            HUD.Banner("ОСТРІВ ДУШ", "Збирай душі аніме-героїв у тематичних кемпах. Почни з кемпів тіру 1 (M — карта).", Hex.C("#8FE8FF"), 6f);
        }

        static void DisableSceneDefaults()
        {
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.gameObject.SetActive(false);
        }

        void SetupLighting()
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(transform, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Hex.C("#FFF2DC");
            sun.intensity = 0.95f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.shadowBias = 0.06f;
            RenderSettings.sun = sun;

            RenderSettings.skybox = Mats.Sky();
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.33f, 0.35f, 0.42f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex.C("#C8E4FA");
            RenderSettings.fogStartDistance = 160f;
            RenderSettings.fogEndDistance = 750f;
            QualitySettings.shadowDistance = 140f;
            QualitySettings.shadowCascades = 2;
        }

        void SpawnPlayer()
        {
            var go = new GameObject("Player");
            go.layer = 2; // Ignore Raycast: mouse aiming and the camera look through the player
            var spawn = World.HubPosition + new Vector3(0, 0.2f, -10f);
            go.transform.position = spawn;
            go.AddComponent<CharacterController>();
            var health = go.AddComponent<Health>();
            health.team = Team.Player;
            go.AddComponent<SoulInventory>();
            go.AddComponent<SkillCaster>();
            player = go.AddComponent<PlayerAvatar>();
        }

        void SetupCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.Skybox;
            c.fieldOfView = 55f;
            c.nearClipPlane = 0.2f;
            c.farClipPlane = 2500f;
            go.AddComponent<AudioListener>();
            cam = go.AddComponent<ThirdPersonCamera>();
            cam.target = player.transform;
            cam.yaw = 0f;
            player.cam = cam;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && player != null) player.inventory.Save();
        }
    }
}
