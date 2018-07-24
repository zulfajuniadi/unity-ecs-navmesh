#region

using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;
using Demo;

#endregion

namespace Demo.Behaviours
{
    public enum BuildingType
    {
        Residential,
        Commercial
    }

    [RequireComponent (typeof (GameObjectEntity))]
    public class Building : MonoBehaviour
    {

        public Vector3 Position;
        public BuildingType Type;

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

            if (mesh.vertexCount == 0 || double.IsNaN (Position.z))
            {
                return;
            }

            gameObject.isStatic = true;

            if (gameObject.name.Contains ("Home"))
            {
                Type = BuildingType.Residential;
            }
            else
            {
                Type = BuildingType.Commercial;
            }

            var entity = GetComponent<GameObjectEntity> ().Entity;
            var manager = GetComponent<GameObjectEntity> ().EntityManager;
            manager.AddComponent (entity, typeof (BuildingData));
            manager.SetComponentData (entity, new BuildingData { Entity = entity, Type = Type, Position = Position });
        }
    }
}