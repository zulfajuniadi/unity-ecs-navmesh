#region

using UnityEngine;
using UnityEngine.AI;

#endregion

namespace Demo.Behaviours
{
    [RequireComponent (typeof (MeshCollider), typeof (NavMeshModifier))]
    public class Road : MonoBehaviour
    {
        private void Start ()
        {
            gameObject.isStatic = true;
            gameObject.layer = 10;
        }
    }
}