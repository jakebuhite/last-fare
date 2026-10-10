using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastFare.Editor
{
    /// <summary>
    /// Saves what the world builder generates (materials, meshes, prefabs) under <see cref="Dir"/>.
    /// Identical materials are merged so each colour exists once, however many objects use it.
    /// </summary>
    public class GeneratedAssets
    {
        public const string Dir = "Assets/_Project/Art/Generated";
        public const string PrefabDir = Dir + "/Prefabs";
        public const string MeshDir = Dir + "/Meshes";

        readonly Dictionary<string, Material> materials = new();

        /// <summary>Deletes and recreates the generated folders. Only call with no open scene referencing them.</summary>
        public static void ResetFolders()
        {
            if (AssetDatabase.IsValidFolder(Dir)) AssetDatabase.DeleteAsset(Dir);
            AssetDatabase.CreateFolder(Path.GetDirectoryName(Dir)!.Replace('\\', '/'), Path.GetFileName(Dir));
            AssetDatabase.CreateFolder(Dir, "Prefabs");
            AssetDatabase.CreateFolder(Dir, "Meshes");
        }

        /// <summary>Saves an asset that the builder created directly (render textures, special materials).</summary>
        public void SaveAsset(Object asset, string fileName) => AssetDatabase.CreateAsset(asset, $"{Dir}/{fileName}");

        /// <summary>Turns <paramref name="template"/> into a prefab asset and destroys the template.</summary>
        public GameObject SavePrefab(GameObject template, string name)
        {
            Save(template);
            var prefab = PrefabUtility.SaveAsPrefabAsset(template, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(template);
            return prefab;
        }

        public void Save(IEnumerable<GameObject> roots)
        {
            foreach (var root in roots) Save(root);
        }

        /// <summary>Saves every material and mesh under <paramref name="root"/> that isn't an asset yet.</summary>
        public void Save(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var shared = renderer.sharedMaterials;
                var changed = false;
                for (var i = 0; i < shared.Length; i++)
                {
                    var m = shared[i];
                    if (m == null || EditorUtility.IsPersistent(m)) continue;
                    shared[i] = SaveMaterial(m);
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = shared;
            }

            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) SaveMesh(filter.sharedMesh);
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true)) SaveMesh(collider.sharedMesh);
        }

        Material SaveMaterial(Material m)
        {
            var key = MaterialKey(m);
            if (materials.TryGetValue(key, out var saved)) return saved;

            m.name = key;
            m.enableInstancing = true;
            AssetDatabase.CreateAsset(m, $"{Dir}/{key}.mat");
            materials[key] = m;
            return m;
        }

        static void SaveMesh(Mesh mesh)
        {
            if (mesh == null || EditorUtility.IsPersistent(mesh)) return;
            AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath($"{MeshDir}/{mesh.name}.asset"));
        }

        static string MaterialKey(Material m)
        {
            var lit = m.shader.name.EndsWith("/Lit");
            var key = $"{(lit ? "Lit" : "Unlit")}_{ColorUtility.ToHtmlStringRGB(m.GetColor("_BaseColor"))}";
            if (lit) key += $"_s{Mathf.RoundToInt(m.GetFloat("_Smoothness") * 100f):00}";
            return key;
        }
    }
}
