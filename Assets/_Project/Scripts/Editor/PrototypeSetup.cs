using System.IO;
using LastFare.Data;
using LastFare.Rides;
using LastFare.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastFare.Editor
{
    /// <summary>
    /// One-click setup for the first-fare prototype: saves the content assets and materials, then
    /// creates <c>Assets/_Project/Scenes/Main.unity</c> containing only a PrototypeBuilder.
    /// </summary>
    public static class PrototypeSetup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Main.unity";

        [MenuItem("Last Fare/Create First-Fare Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            var lit = CreateMaterial("Lit_Base", "Universal Render Pipeline/Lit");
            var unlit = CreateMaterial("Unlit_Base", "Universal Render Pipeline/Unlit");

            var passenger = SaveAsset(QuietOneFare.CreatePassenger(), Root + "/ScriptableObjects/Passengers/Passenger_QuietOne.asset");
            var ride = RideDefinition.CreateRuntime(passenger, "Mercy Hill Cemetery, north gate", "114 Aldous Street", 28, 25f);
            ride = SaveAsset(ride, Root + "/ScriptableObjects/Rides/Ride_QuietOne.asset");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Game");
            var builder = go.AddComponent<PrototypeBuilder>();

            var so = new SerializedObject(builder);
            so.FindProperty("ride").objectReferenceValue = ride;
            so.FindProperty("litMaterial").objectReferenceValue = lit;
            so.FindProperty("unlitMaterial").objectReferenceValue = unlit;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Last Fare: created {ScenePath}. Open it and press Play.");
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
