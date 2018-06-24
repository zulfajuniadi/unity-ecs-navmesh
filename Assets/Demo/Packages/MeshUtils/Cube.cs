using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils
{

    public class Cube
    {

        public GameObject GameObject;
        public Transform Transform;
        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;
        public MeshCollider MeshCollider;

        public float Width;
        public float Length;
        public float Height;

        public Cube (
            string name,
            Vector3[] points,
            float height,
            Material material,
            Transform parent,
            bool addCollider = true
        )
        {

            var offset = new Vector3 (0, height, 0);

            var bottomBackLeftVert = points[0];
            var bottomFrontLeftVert = points[1];
            var bottomFrontRightVert = points[2];
            var bottomBackRightVert = points[3];

            var topBackLeftVert = bottomBackLeftVert + offset;
            var topFrontLeftVert = bottomFrontLeftVert + offset;
            var topFrontRightVert = bottomFrontRightVert + offset;
            var topBackRightVert = bottomBackRightVert + offset;

            var bounds = new Bounds (points[0], Vector3.zero);
            foreach (var point in points)
            {
                bounds.Encapsulate (point);
            }

            // var bottomBackLeftVert = new Vector3 (0, 0, 0);
            // var bottomFrontLeftVert = new Vector3 (0, 0, length);
            // var bottomFrontRightVert = new Vector3 (width, 0, length);
            // var bottomBackRightVert = new Vector3 (width, 0, 0);

            // var topBackLeftVert = new Vector3 (0, height, 0);
            // var topFrontLeftVert = new Vector3 (0, height, length);
            // var topFrontRightVert = new Vector3 (width, height, length);
            // var topBackRightVert = new Vector3 (width, height, 0);

            var vertices = new Vector3[]
            {
                // back
                bottomBackLeftVert,
                topBackLeftVert,
                topBackRightVert,
                bottomBackRightVert,
                // front
                bottomFrontLeftVert,
                topFrontLeftVert,
                topFrontRightVert,
                bottomFrontRightVert,
                // right
                bottomBackRightVert,
                topBackRightVert,
                topFrontRightVert,
                bottomFrontRightVert,
                // left
                bottomFrontLeftVert,
                topFrontLeftVert,
                topBackLeftVert,
                bottomBackLeftVert,
                // top
                topBackLeftVert,
                topFrontLeftVert,
                topFrontRightVert,
                topBackRightVert,
                // bottom
                bottomBackLeftVert,
                bottomFrontLeftVert,
                bottomFrontRightVert,
                bottomBackRightVert,
            };

            var triangles = new int[]
            {
                0,
                1,
                2,
                0,
                2,
                3,
                0 + 4 * 1,
                2 + 4 * 1,
                1 + 4 * 1,
                0 + 4 * 1,
                3 + 4 * 1,
                2 + 4 * 1,
                0 + 4 * 2,
                1 + 4 * 2,
                2 + 4 * 2,
                0 + 4 * 2,
                2 + 4 * 2,
                3 + 4 * 2,
                0 + 4 * 3,
                1 + 4 * 3,
                2 + 4 * 3,
                0 + 4 * 3,
                2 + 4 * 3,
                3 + 4 * 3,
                0 + 4 * 4,
                1 + 4 * 4,
                2 + 4 * 4,
                0 + 4 * 4,
                2 + 4 * 4,
                3 + 4 * 4,
                0 + 4 * 5,
                2 + 4 * 5,
                1 + 4 * 5,
                0 + 4 * 5,
                3 + 4 * 5,
                2 + 4 * 5,
            };

            var top = Vector3.up;
            var left = Vector3.left;
            var right = Vector3.right;
            var bottom = Vector3.down;
            var front = Vector3.forward;
            var back = Vector3.back;

            var normals = new Vector3[]
            {
                // back
                back,
                back,
                back,
                back,
                //front
                front,
                front,
                front,
                front,
                // right
                right,
                right,
                right,
                right,
                // left
                left,
                left,
                left,
                left,
                // top
                top,
                top,
                top,
                top,
                // bottom
                bottom,
                bottom,
                bottom,
                bottom
            };

            var z00 = new Vector2 (0, 0);
            var z01 = new Vector2 (0, bounds.size.y);
            var z11 = new Vector2 (bounds.size.x, bounds.size.y);
            var z10 = new Vector2 (bounds.size.x, 0);

            var x00 = new Vector2 (0, 0);
            var x01 = new Vector2 (0, bounds.size.y);
            var x11 = new Vector2 (bounds.size.z, bounds.size.y);
            var x10 = new Vector2 (bounds.size.z, 0);

            var y00 = new Vector2 (0, 0);
            var y01 = new Vector2 (0, bounds.size.z);
            var y11 = new Vector2 (bounds.size.x, bounds.size.z);
            var y10 = new Vector2 (bounds.size.x, 0);

            var yb01 = new Vector2 (bounds.size.z, bounds.size.z);
            var yb11 = new Vector2 (bounds.size.x - bounds.size.z, bounds.size.z);

            var uvs = new Vector2[]
            {
                // back
                z00,
                z01,
                z11,
                z10,
                // front
                z10,
                z11,
                z01,
                z00,
                // right
                x00,
                x01,
                x11,
                x10,
                // left
                x00,
                x01,
                x11,
                x10,
                // top
                y00,
                y01,
                y11,
                y10,
                // bottom
                y10,
                y11,
                y01,
                y00,
            };

            var mesh = new Mesh ();
            mesh.subMeshCount = 6;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.normals = normals;

            mesh.RecalculateBounds ();

            var go = new GameObject (name, typeof (MeshFilter), typeof (MeshRenderer));
            go.transform.parent = parent;
            MeshRenderer = go.GetComponent<MeshRenderer> ();
            MeshRenderer.sharedMaterial = material;
            MeshFilter = go.GetComponent<MeshFilter> ();
            MeshFilter.sharedMesh = mesh;
            if (addCollider)
                MeshCollider = go.AddComponent<MeshCollider> ();

            // go.isStatic = true;

            this.GameObject = go;
            this.Transform = go.transform;
            this.Width = bounds.size.x;
            this.Length = bounds.size.z;
            this.Height = bounds.size.y;
        }
    }
}