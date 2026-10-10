using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFare.Editor
{
    /// <summary>
    /// Collects greybox pieces (boxes, discs, strips) and merges them into one mesh per material, so a road
    /// network made of thousands of pieces becomes a handful of renderers. Only positions and normals are
    /// kept: the greybox materials are untextured.
    /// </summary>
    public class MeshBatcher
    {
        static readonly Dictionary<PrimitiveType, Mesh> Primitives = new();

        class Part
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<Vector3> Normals = new();
            public readonly List<int> Triangles = new();
        }

        readonly Dictionary<Material, Part> parts = new();

        public bool IsEmpty => parts.Count == 0;

        public void Box(Vector3 center, Quaternion rotation, Vector3 size, Material material) =>
            Add(PrimitiveType.Cube, Matrix4x4.TRS(center, rotation, size), material);

        /// <summary>A flat disc (cylinder) whose top sits at <paramref name="top"/>.</summary>
        public void Disc(Vector3 center, float diameter, float thickness, float top, Material material)
        {
            center.y = top - thickness * 0.5f;
            Add(PrimitiveType.Cylinder, Matrix4x4.TRS(center, Quaternion.identity, new Vector3(diameter, thickness * 0.5f, diameter)), material);
        }

        /// <summary>
        /// A straight band from <paramref name="a"/> to <paramref name="b"/>, shifted sideways by <paramref name="offset"/>
        /// (positive = right of a-to-b), with its top at <paramref name="top"/>.
        /// </summary>
        public void Strip(Vector3 a, Vector3 b, float offset, float width, float thickness, float top, Material material)
        {
            var d = b - a;
            d.y = 0f;
            var length = d.magnitude;
            if (length < 0.01f) return;

            var dir = d / length;
            var right = Vector3.Cross(Vector3.up, dir);
            var center = (a + b) * 0.5f + right * offset;
            center.y = top - thickness * 0.5f;
            Box(center, Quaternion.LookRotation(dir), new Vector3(width, thickness, length), material);
        }

        /// <summary>Dashes along a-b: <paramref name="dash"/> metres of paint every <paramref name="period"/> metres.</summary>
        public void Dashes(Vector3 a, Vector3 b, float offset, float width, float dash, float period, float top, Material material,
            float margin = 0f)
        {
            var d = b - a;
            d.y = 0f;
            var length = d.magnitude;
            var dir = d / Mathf.Max(length, 0.001f);
            for (var s = margin; s + dash <= length - margin; s += period)
                Strip(a + dir * s, a + dir * (s + dash), offset, width, 0.02f, top, material);
        }

        void Add(PrimitiveType type, Matrix4x4 matrix, Material material)
        {
            var mesh = Primitive(type);
            if (!parts.TryGetValue(material, out var part)) parts[material] = part = new Part();

            var normalMatrix = matrix.inverse.transpose;
            var first = part.Vertices.Count;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            for (var i = 0; i < vertices.Length; i++)
            {
                part.Vertices.Add(matrix.MultiplyPoint3x4(vertices[i]));
                part.Normals.Add(normalMatrix.MultiplyVector(normals[i]).normalized);
            }

            foreach (var index in mesh.triangles) part.Triangles.Add(first + index);
        }

        /// <summary>Creates one child per material under <paramref name="parent"/> and clears the batch.</summary>
        public void Build(Transform parent, string name, bool withCollider = false)
        {
            var i = 0;
            foreach (var (material, part) in parts)
            {
                var mesh = new Mesh { name = $"{name}_{i++}", indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(part.Vertices);
                mesh.SetNormals(part.Normals);
                mesh.SetTriangles(part.Triangles, 0);
                mesh.RecalculateBounds();

                var go = new GameObject(parts.Count == 1 ? name : $"{name} ({material.name})");
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
                if (withCollider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }

            parts.Clear();
        }

        static Mesh Primitive(PrimitiveType type)
        {
            if (Primitives.TryGetValue(type, out var mesh) && mesh != null) return mesh;
            var go = GameObject.CreatePrimitive(type);
            mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            return Primitives[type] = mesh;
        }
    }
}
