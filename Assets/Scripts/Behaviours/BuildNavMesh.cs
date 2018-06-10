using System.Collections;
using Mapbox.Unity.Map;
using UnityEngine;
using UnityEngine.AI;

namespace Behaviours
{
    public class BuildNavMesh : MonoBehaviour
    {

        NavMeshSurface surface;
        AbstractMap map;
        public bool IsBuilt = false;

        void Start ()
        {
            map = GetComponent<AbstractMap> ();
            surface = GetComponent<NavMeshSurface> ();
            StartCoroutine ("DoBuild");
        }

        IEnumerator DoBuild ()
        {
            yield return new WaitUntil (() => map.MapVisualizer.State == ModuleState.Finished);
            Build ();
            IsBuilt = true;
        }

        public void Build ()
        {
            surface.BuildNavMesh ();
        }

    }
}