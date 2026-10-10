using System.IO;
using LastFare.Data;
using LastFare.Rides;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastFare.Editor
{
    /// <summary>
    /// One-click setup for the first-fare prototype: saves the content assets and base materials, then
    /// (re)builds <c>Assets/_Project/Scenes/Main.unity</c> with every GameObject laid out in the scene.
    /// Rebuilding replaces the scene and everything in <see cref="PrototypeSceneBuilder.GeneratedDir"/>.
    /// </summary>
    public static class PrototypeSetup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Main.unity";

        [MenuItem("Last Fare/Build First-Fare Prototype Scene")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild prototype scene?",
                    $"This replaces {ScenePath} and the generated materials. Hand edits to that scene will be lost.",
                    "Rebuild", "Cancel"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildPrototypeScene();
        }

        public static void BuildPrototypeScene()
        {
            var lit = CreateMaterial("Lit_Base", "Universal Render Pipeline/Lit");
            var unlit = CreateMaterial("Unlit_Base", "Universal Render Pipeline/Unlit");

            var passenger = SaveAsset(QuietOneFare.CreatePassenger(), Root + "/ScriptableObjects/Passengers/Passenger_QuietOne.asset");
            var ride = RideDefinition.CreateRuntime(passenger, "Mercy Hill Cemetery, north gate", "114 Aldous Street", 28, 25f);
            ride = SaveAsset(ride, Root + "/ScriptableObjects/Rides/Ride_QuietOne.asset");

            // Start from a fresh scene so nothing references the generated assets we're about to replace.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var generated = PrototypeSceneBuilder.GeneratedDir;
            if (AssetDatabase.IsValidFolder(generated)) AssetDatabase.DeleteAsset(generated);
            AssetDatabase.CreateFolder(Path.GetDirectoryName(generated)!.Replace('\\', '/'), Path.GetFileName(generated));

            new PrototypeSceneBuilder(ride, lit, unlit).Build();
            PrototypeSceneBuilder.SaveGeneratedAssets(scene.GetRootGameObjects());

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Last Fare: built {ScenePath}. Press Play.");
        }

        static Material CreateMaterial(string name, string shaderName)
        {
            var path = $"{Root}/Art/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static T SaveAsset<T>(T asset, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Object.DestroyImmediate(asset);
                return existing;
            }

            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
