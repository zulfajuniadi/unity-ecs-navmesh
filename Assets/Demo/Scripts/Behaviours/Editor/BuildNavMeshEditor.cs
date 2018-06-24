using Demo.Behaviours;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (BuildNavMesh))]
public class BuildNavMeshEditor : Editor
{
    public override void OnInspectorGUI ()
    {
        base.OnInspectorGUI ();
        if (GUILayout.Button ("Rebuild"))
        {
            ((BuildNavMesh) target).Build ();
        }
    }
}