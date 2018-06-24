using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Demo.Behaviours
{
    public class BuildNavMesh : MonoBehaviour
    {

        NavMeshSurface surface;
        public bool IsBuilt = false;

        public void Build ()
        {
            IsBuilt = false;
            surface = GetComponent<NavMeshSurface> ();
            var operation = surface.UpdateNavMesh (surface.navMeshData);
            operation.completed += BuildComplete;
        }

        private void BuildComplete (AsyncOperation operation)
        {
            IsBuilt = true;
        }
    }
}