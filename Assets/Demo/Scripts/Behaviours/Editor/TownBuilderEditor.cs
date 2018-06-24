using Demo.Behaviours;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (TownBuilder))]
public class TownBuilderEditor : Editor
{
    public override void OnInspectorGUI ()
    {
        base.OnInspectorGUI ();

        TownBuilder town = target as TownBuilder;

        if (GUILayout.Button ("Generate"))
        {
            town.Generate ();
        }

        if (GUILayout.Button ("Generate Random"))
        {
            town.Seed = Random.Range (1, 99999);
            town.Generate ();
        }

    }
}