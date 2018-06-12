#region

using Demo;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

#endregion

namespace Demo.Modifiers
{
    public enum BuildingType
    {
        Residential,
        Commercial
    }

    [RequireComponent (typeof (MeshCollider), typeof (NavMeshModifier), typeof (GameObjectEntity))]
    public class Building : MonoBehaviour
    {
        private readonly float maxX = -43.61511f + 50f;
        private readonly float maxZ = -22.09686f + 50f;
        private readonly float minX = -33.61511f - 50f;
        private readonly float minZ = -12.09686f - 50f;

        public Vector3 Position;
        public BuildingType Type;
        public float Volume;

        public float SignedVolumeOfTriangle (Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var v321 = p3.x * p2.y * p1.z;
            var v231 = p2.x * p3.y * p1.z;
            var v312 = p3.x * p1.y * p2.z;
            var v132 = p1.x * p3.y * p2.z;
            var v213 = p2.x * p1.y * p3.z;
            var v123 = p1.x * p2.y * p3.z;
            return 1.0f / 6.0f * (-v321 + v231 + v312 - v132 - v213 + v123);
        }

        public float VolumeOfMesh (Mesh mesh)
        {
            float volume = 0;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (var i = 0; i < mesh.triangles.Length; i += 3)
            {
                var p1 = vertices[triangles[i + 0]];
                var p2 = vertices[triangles[i + 1]];
                var p3 = vertices[triangles[i + 2]];
                volume += SignedVolumeOfTriangle (p1, p2, p3);
            }

            return Mathf.Abs (volume);
        }

        private void Start ()
        {
            var mesh = GetComponent<MeshFilter> ().sharedMesh;
            foreach (var vertex in mesh.vertices)
            {
                Position.x += vertex.x;
                Position.z += vertex.z;
            }

            Position.x /= mesh.vertexCount;
            Position.z /= mesh.vertexCount;
            Position = transform.TransformPoint (Position);
            if (Position.x < minX || Position.x > maxX || Position.z < minZ || Position.z > maxZ)
            {
                gameObject.SetActive (false);
                return;
            }

            if (mesh.vertexCount == 0 || double.IsNaN (Position.z))
            {
                Debug.Log ("culprit");
                return;
            }

            gameObject.isStatic = true;
            Volume = VolumeOfMesh (mesh);

            var modifierBuilding = GameObject.Find ("Building").GetComponent<NavMeshModifier> ();
            var modifier = gameObject.GetComponent<NavMeshModifier> ();
            modifier.overrideArea = modifierBuilding.overrideArea;
            modifier.area = modifierBuilding.area;

            if (Volume > 0.25)
            {
                Type = BuildingType.Commercial;
            }
            else
            {
                Type = BuildingType.Residential;
            }

            var entity = GetComponent<GameObjectEntity> ().Entity;
            var manager = GetComponent<GameObjectEntity> ().EntityManager;
            manager.AddComponent (entity, typeof (BuildingData));
            manager.SetComponentData (entity, new BuildingData { Entity = entity, Type = Type, Position = Position });
            gameObject.layer = 9;
        }

        private void OnDrawGizmos ()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube (Position, Vector3.one);
        }
    }
}