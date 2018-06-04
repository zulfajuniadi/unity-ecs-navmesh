using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent (typeof (MeshCollider), typeof (NavMeshModifier))]
public class Sidewalk : MonoBehaviour {

	// Use this for initialization
	void Start () {
		gameObject.isStatic = true;
		var modifierSidewalk = GameObject.Find ("Sidewalk").GetComponent<NavMeshModifier> ();
		var modifier = gameObject.GetComponent<NavMeshModifier> ();
		modifier.overrideArea = modifierSidewalk.overrideArea;
		modifier.area = modifierSidewalk.area;
	}
}