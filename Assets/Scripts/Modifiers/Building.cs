using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum BuildingType {
	Residential,
	Commercial,
}

[RequireComponent (typeof (MeshCollider), typeof (BuildingData), typeof (NavMeshModifier))]
public class Building : MonoBehaviour {

	float minX = -38.61511f - 50f;
	float maxX = -38.61511f + 50f;
	float minZ = -17.09686f - 50f;
	float maxZ = -17.09686f + 50f;

	Vector3 center;
	float volume;

	public float SignedVolumeOfTriangle (Vector3 p1, Vector3 p2, Vector3 p3) {
		float v321 = p3.x * p2.y * p1.z;
		float v231 = p2.x * p3.y * p1.z;
		float v312 = p3.x * p1.y * p2.z;
		float v132 = p1.x * p3.y * p2.z;
		float v213 = p2.x * p1.y * p3.z;
		float v123 = p1.x * p2.y * p3.z;
		return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
	}

	public float VolumeOfMesh (Mesh mesh) {
		float volume = 0;
		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;
		for (int i = 0; i < mesh.triangles.Length; i += 3) {
			Vector3 p1 = vertices[triangles[i + 0]];
			Vector3 p2 = vertices[triangles[i + 1]];
			Vector3 p3 = vertices[triangles[i + 2]];
			volume += SignedVolumeOfTriangle (p1, p2, p3);
		}
		return Mathf.Abs (volume);
	}

	// Use this for initialization
	void Start () {
		var mesh = GetComponent<MeshFilter> ().sharedMesh;
		foreach (var vertex in mesh.vertices) {
			center.x += vertex.x;
			center.z += vertex.z;
		}
		center.x /= mesh.vertexCount;
		center.z /= mesh.vertexCount;
		center = transform.TransformPoint (center);
		if (center.x < minX || center.x > maxX || center.z < minZ || center.z > maxZ) {
			gameObject.SetActive (false);
			return;
		}
		gameObject.isStatic = true;
		volume = VolumeOfMesh (mesh);

		var data = gameObject.GetComponent<BuildingData> ();
		data.Volume = volume;
		data.Position = center;

		var modifierBuilding = GameObject.Find ("Building").GetComponent<NavMeshModifier> ();
		var modifier = gameObject.GetComponent<NavMeshModifier> ();
		modifier.overrideArea = modifierBuilding.overrideArea;
		modifier.area = modifierBuilding.area;

		if (volume > 0.25) {
			data.Type = BuildingType.Commercial;
			GameObject.FindObjectOfType<PopulationSpawner> ().CommercialBuildings.Enqueue (data);
		} else {
			data.Type = BuildingType.Residential;
			GameObject.FindObjectOfType<PopulationSpawner> ().ResidentialBuildings.Enqueue (data);
		}
	}

	private void OnDrawGizmos () {
		Gizmos.color = Color.red;
		Gizmos.DrawCube (center, Vector3.one);
	}
}