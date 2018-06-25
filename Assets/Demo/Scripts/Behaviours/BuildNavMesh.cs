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

        public void Build (System.Action<AsyncOperation> callback = null)
        {
            IsBuilt = false;
            surface = GetComponent<NavMeshSurface> ();
            var operation = surface.UpdateNavMesh (surface.navMeshData);
            operation.completed += BuildComplete;
            if (callback != null)
                operation.completed += callback;
        }

        private void BuildComplete (AsyncOperation operation)
        {
            IsBuilt = true;
        }
    }
}