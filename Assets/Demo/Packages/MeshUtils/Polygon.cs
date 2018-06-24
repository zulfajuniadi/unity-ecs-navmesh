using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils
{
    public class Polygon
    {
        public GameObject GameObject;
        public Transform Transform;
        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;
        public MeshCollider MeshCollider;

        public Polygon (
            string name,
            List<Vector3> vertices,
            float extrusion,
            Material material,
            Transform parent,
            bool createCollider = true
        )
        {
            var triangulator = new Triangulator (vertices);
            var uvs = new Vector2[vertices.Count];
            var normals = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                uvs[i] = new Vector2 (vertices[i].x, vertices[i].z);
                normals[i] = Vector3.up;
            }
            var mesh = new Mesh ();
            mesh.SetVertices (vertices);
            mesh.SetTriangles (triangulator.Triangulate (), 0);
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.RecalculateBounds ();

            var output = mesh;
            if (extrusion != 0)
            {
                output = new Mesh ();
                var matrix = new Matrix4x4[]
                {
                    Matrix4x4.TRS (new Vector3 (0, 0, 0), Quaternion.identity, Vector3.one),
                    Matrix4x4.TRS (new Vector3 (0, extrusion, 0), Quaternion.identity, Vector3.one)
                };
                MeshExtrusion.Extrude (mesh, output, matrix, true);
            }

            GameObject = new GameObject (name);
            GameObject.transform.parent = parent;
            MeshRenderer = GameObject.AddComponent<MeshRenderer> ();
            MeshRenderer.sharedMaterial = material;
            MeshFilter = GameObject.AddComponent<MeshFilter> ();
            MeshFilter.sharedMesh = output;
            if (createCollider)
            {
                MeshCollider = GameObject.AddComponent<MeshCollider> ();
                MeshCollider.sharedMesh = output;
            }
            Transform = GameObject.transform;
        }
    }
}