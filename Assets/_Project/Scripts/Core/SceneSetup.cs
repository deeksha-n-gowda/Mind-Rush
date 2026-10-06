using UnityEngine;
using UnityEditor;
using MindRush.Network;
using MindRush.Core;
using MindRush.Player;
using MindRush.Environment;

namespace MindRush.Editor
{
    /// <summary>
    /// Editor script to auto-setup the MainGame scene with all required components.
    /// Run via: Tools > MindRush > Setup Scene
    /// </summary>
    public class SceneSetup
    {
        [MenuItem("Tools/MindRush/Setup Scene")]
        public static void SetupScene()
        {
            // Ensure NetworkInitializer exists
            var networkInit = GameObject.Find("NetworkInitializer");
            if (networkInit == null)
            {
                networkInit = new GameObject("NetworkInitializer");
                networkInit.AddComponent<NetworkInitializer>();
                Debug.Log("[SceneSetup] Added NetworkInitializer");
            }

            // Ensure HUDManager exists
            var hudManager = GameObject.Find("HUDManager");
            if (hudManager == null)
            {
                hudManager = new GameObject("HUDManager");
                hudManager.AddComponent<HUDManager>();
                Debug.Log("[SceneSetup] Added HUDManager");
            }

            // Find player and link to HUD
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var pc = player.GetComponent<PlayerController>();
                var hud = hudManager.GetComponent<HUDManager>();
                if (pc != null && hud != null)
                {
                    hud.SetPlayerController(pc);
                    Debug.Log("[SceneSetup] Linked PlayerController to HUDManager");
                }
            }

            // Ensure EventSystem exists (for UI)
            if (GameObject.Find("EventSystem") == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("[SceneSetup] Added EventSystem");
            }

            // Setup collectible layers
            SetupLayers();

            Debug.Log("[SceneSetup] Scene setup complete!");
        }

        private static void SetupLayers()
        {
            // Ensure custom layers exist
            string[] requiredLayers = { "Collectible", "Obstacle" };
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");

            foreach (var layerName in requiredLayers)
            {
                bool exists = false;
                for (int i = 8; i < 32; i++)
                {
                    var layerProp = layersProp.GetArrayElementAtIndex(i);
                    if (layerProp.stringValue == layerName)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    for (int i = 8; i < 32; i++)
                    {
                        var layerProp = layersProp.GetArrayElementAtIndex(i);
                        if (string.IsNullOrEmpty(layerProp.stringValue))
                        {
                            layerProp.stringValue = layerName;
                            tagManager.ApplyModifiedProperties();
                            Debug.Log($"[SceneSetup] Added layer: {layerName}");
                            break;
                        }
                    }
                }
            }
        }

        [MenuItem("Tools/MindRush/Create Collectible Prefabs")]
        public static void CreateCollectiblePrefabs()
        {
            // Create Idea Lightbulb prefab
            CreateCollectiblePrefab("Idea_Lightbulb", Collectible.CollectibleType.Idea);
            
            // Create Focus Gem prefab
            CreateCollectiblePrefab("Focus_Gem", Collectible.CollectibleType.Gem);

            Debug.Log("[SceneSetup] Created collectible prefabs");
        }

        private static void CreateCollectiblePrefab(string name, Collectible.CollectibleType type)
        {
            var go = new GameObject(name);
            var collectible = go.AddComponent<Collectible>();
            collectible.type = type;

            // Add visual representation
            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();

            if (type == Collectible.CollectibleType.Idea)
            {
                meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.yellow;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.yellow * 2f);
                meshRenderer.material = mat;
            }
            else
            {
                meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.cyan;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.cyan * 2f);
                meshRenderer.material = mat;
            }

            // Add collider
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;

            // Save as prefab
            string path = $"Assets/_Project/Prefabs/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
        }
    }
}