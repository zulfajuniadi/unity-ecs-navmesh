#region

using UnityEngine;
using UnityEngine.AI;

#endregion

namespace Demo.Modifiers
{
    [RequireComponent (typeof (MeshCollider), typeof (NavMeshModifier))]
    public class Sidewalk : MonoBehaviour
    {
        private void Start ()
        {
            gameObject.isStatic = true;
            var modifierSidewalk = GameObject.Find ("Sidewalk").GetComponent<NavMeshModifier> ();
            var modifier = gameObject.GetComponent<NavMeshModifier> ();
            modifier.overrideArea = modifierSidewalk.overrideArea;
            modifier.area = modifierSidewalk.area;
            gameObject.layer = 10;
        }
    }
}