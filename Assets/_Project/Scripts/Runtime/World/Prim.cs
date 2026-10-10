using UnityEngine;

namespace LastFare.World
{
    /// <summary>Greybox helpers: coloured primitives built at runtime with URP materials.</summary>
    public static class Prim
    {
        static Material litBase;
        static Material unlitBase;

        /// <summary>Optionally provide base materials (assigned in the scene so builds include the shaders).</summary>
        public static void Init(Material lit, Material unlit)
        {
            litBase = lit;
            unlitBase = unlit;
        }

        public static Material Lit(Color color, float smoothness = 0.15f)
        {
            if (litBase == null) litBase = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var m = new Material(litBase);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        public static Material Unlit(Color color)
        {
            if (unlitBase == null) unlitBase = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var m = new Material(unlitBase);
            m.SetColor("_BaseColor", color);
            return m;
        }

        public static GameObject Make(PrimitiveType type, Transform parent, string name, Vector3 localPos,
            Vector3 localScale, Material material, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col);
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localScale = localScale;
            if (euler.HasValue) t.localEulerAngles = euler.Value;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat,
            Vector3? euler = null) => Make(PrimitiveType.Cube, parent, name, pos, scale, mat, euler);

        public static GameObject Sphere(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat) =>
            Make(PrimitiveType.Sphere, parent, name, pos, scale, mat);

        public static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float range, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }
    }
}
