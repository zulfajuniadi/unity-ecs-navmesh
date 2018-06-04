using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using UnityEngine;
using UnityEngine.AI;

public class BuildNavMesh : MonoBehaviour {

	NavMeshSurface surface;
	AbstractMap map;
	public bool IsBuilt = false;

	// Use this for initialization
	void Start () {
		map = GetComponent<AbstractMap> ();
		surface = GetComponent<NavMeshSurface> ();
		map.OnInitialized += () => {
			StartCoroutine ("DoBuild");
		};
	}

	IEnumerator DoBuild () {
		yield return new WaitUntil (() => map.MapVisualizer.State == ModuleState.Finished);
		Build ();
		IsBuilt = true;
	}

	public void Build () {
		surface.BuildNavMesh ();
	}

}