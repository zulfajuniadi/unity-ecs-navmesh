using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NavJob.Systems;
using Town;

namespace Demo.Behaviours
{

	public class TownBuilder : MonoBehaviour
	{
		public int Seed = 100;
		public Transform TownRoot;

		[Range (10, 50)]
		public int Patches = 15;
		public bool Walls = true;
		public bool Water = true;

		public Material GateMaterial;
		public Material TowerMaterial;
		public Material WallMaterial;
		public Material RoadMaterial;
		public Material WaterMaterial;
		public Material BuildingMaterial;
		public Material OverlayMaterial;

		public void Generate ()
		{

			for (int i = TownRoot.childCount - 1; i > -1; i--)
			{
				DestroyImmediate (TownRoot.GetChild (i).gameObject);
			}

			var townOptions = new TownOptions
			{
				Overlay = true,
					Patches = Patches,
					Walls = Walls,
					Water = Water,
					Seed = Seed
			};

			var town = new Town.Town (townOptions);

			var townRenderer = new TownMeshRenderer (town, townOptions);
			townRenderer.root = TownRoot;
			townRenderer.GateMaterial = GateMaterial;
			townRenderer.TowerMaterial = TowerMaterial;
			townRenderer.WallMaterial = WallMaterial;
			townRenderer.RoadMaterial = RoadMaterial;
			townRenderer.WaterMaterial = WaterMaterial;
			townRenderer.BuildingMaterial = BuildingMaterial;
			townRenderer.OverlayMaterial = OverlayMaterial;
			townRenderer.Generate ();

			if (townRenderer.Walls != null)
			{
				foreach (Transform child in townRenderer.Walls.transform)
				{
					var modifier = child.gameObject.AddComponent<NavMeshModifier> ();
					modifier.overrideArea = true;
					modifier.area = 6;
				}
			}

			if (townRenderer.Waters != null)
			{
				foreach (Transform child in townRenderer.Waters.transform)
				{
					var modifier = child.gameObject.AddComponent<NavMeshModifier> ();
					modifier.overrideArea = true;
					modifier.area = 5;
				}
			}

			foreach (Transform child in townRenderer.Roads.transform)
			{
				var modifier = child.gameObject.AddComponent<NavMeshModifier> ();
				modifier.overrideArea = true;
				modifier.area = 3;
			}

			foreach (Transform child in townRenderer.Buildings.transform)
			{
				var modifier = child.gameObject.AddComponent<NavMeshModifier> ();
				modifier.overrideArea = true;
				modifier.area = 4;
				child.gameObject.AddComponent<Building> ();
			}

			GetComponent<BuildNavMesh> ().Build ((AsyncOperation operation) =>
			{
				NavMeshQuerySystem.PurgeCacheStatic ();
				Debug.Log ("Town built. Cache purged.");
			});
		}
	}
}