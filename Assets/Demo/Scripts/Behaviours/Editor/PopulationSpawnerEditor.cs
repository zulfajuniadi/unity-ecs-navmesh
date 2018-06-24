using Demo.Behaviours;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (PopulationSpawner))]

public class PopulationSpawnerEditor : Editor
{

    public override void OnInspectorGUI ()
    {
        base.OnInspectorGUI ();
        var spawner = target as PopulationSpawner;

        var val = EditorGUILayout.MaskField ("Walkable Areas", spawner.AgentAreaMask, GameObjectUtility.GetNavMeshAreaNames ());
        if (!val.Equals (spawner.AgentAreaMask))
        {
            spawner.AgentAreaMask = val;
        }
    }
}