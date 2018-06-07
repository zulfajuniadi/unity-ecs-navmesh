using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent (typeof (MeshCollider), typeof (NavMeshModifier))]
public class Sidewalk : MonoBehaviour {
	void Start () {
		gameObject.isStatic = true;
		var modifierSidewalk = GameObject.Find ("Sidewalk").GetComponent<NavMeshModifier> ();
		var modifier = gameObject.GetComponent<NavMeshModifier> ();
		modifier.overrideArea = modifierSidewalk.overrideArea;
		modifier.area = modifierSidewalk.area;
		gameObject.layer = 10;
	}
}