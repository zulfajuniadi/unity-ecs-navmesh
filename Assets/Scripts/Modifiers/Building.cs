using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

public enum BuildingType {
	Residential,
	Commercial,
}

[RequireComponent (typeof (MeshCollider), typeof (NavMeshModifier), typeof (GameObjectEntity))]
public class Building : MonoBehaviour {

	float minX = -33.61511f - 50f;
	float maxX = -43.61511f + 50f;
	float minZ = -12.09686f - 50f;
	float maxZ = -22.09686f + 50f;

	public Vector3 Position;
	public float Volume;
	public BuildingType Type;

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

	void Start () {
		var mesh = GetComponent<MeshFilter> ().sharedMesh;
		foreach (var vertex in mesh.vertices) {
			Position.x += vertex.x;
			Position.z += vertex.z;
		}
		Position.x /= mesh.vertexCount;
		Position.z /= mesh.vertexCount;
		Position = transform.TransformPoint (Position);
		if (Position.x < minX || Position.x > maxX || Position.z < minZ || Position.z > maxZ) {
			gameObject.SetActive (false);
			return;
		}
		gameObject.isStatic = true;
		Volume = VolumeOfMesh (mesh);

		var modifierBuilding = GameObject.Find ("Building").GetComponent<NavMeshModifier> ();
		var modifier = gameObject.GetComponent<NavMeshModifier> ();
		modifier.overrideArea = modifierBuilding.overrideArea;
		modifier.area = modifierBuilding.area;

		if (Volume > 0.25) {
			Type = BuildingType.Commercial;
		} else {
			Type = BuildingType.Residential;
		}

		var entity = GetComponent<GameObjectEntity> ().Entity;
		var manager = GetComponent<GameObjectEntity> ().EntityManager;
		manager.AddComponent (entity, typeof (BuildingData));
		manager.SetComponentData (entity, new BuildingData () { Entity = entity, Type = Type, Position = Position });
	}

	private void OnDrawGizmos () {
		Gizmos.color = Color.red;
		Gizmos.DrawCube (Position, Vector3.one);
	}
}