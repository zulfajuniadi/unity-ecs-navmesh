using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MeshUtils;
using Town.Geom;
using UnityEngine;

namespace Town
{
    public class TownMeshRenderer
    {
        private readonly Town _town;
        private readonly TownOptions _options;
        Transform child;
        public Transform root;
        public Material GateMaterial;
        public Material TowerMaterial;
        public Material WallMaterial;
        public Material RoadMaterial;
        public Material WaterMaterial;
        public Material BuildingMaterial;
        public Material OverlayMaterial;

        public GameObject Waters;
        public GameObject Buildings;
        public GameObject BuildingsMesh;
        public GameObject Walls;
        public GameObject WallsMesh;
        public GameObject Roads;

        public TownMeshRenderer (Town town, TownOptions options = null)
        {
            if (options == null)
            {
                options = TownOptions.Default;
            }

            _town = town;
            _options = options;
        }

        public GameObject Generate ()
        {
            var go = new GameObject ("Town");
            go.transform.parent = root;
            child = go.transform;
            var bounds = _town.GetCityWallsBounds ().Expand (100);

            var geometry = _town.GetTownGeometry (_options);
            List<Vector3> vertices = new List<Vector3> ();
            UnityEngine.Random.InitState (_options.Seed.GetHashCode ());

            if (_options.Water)
            {
                Waters = new GameObject ("Waters");
                Waters.transform.parent = child;
                foreach (var water in geometry.Water)
                {
                    foreach (var vertex in water.Vertices)
                    {
                        vertices.Add (new Vector3 (vertex.x, 0, vertex.y));
                    }
                    new MeshUtils.Polygon ("Water", vertices, 0.1f, WaterMaterial, Waters.transform);
                    vertices.Clear ();
                }
            }
            else
            {
                Waters = null;
            }

            BuildingsMesh = new GameObject ("BuildingsMesh");
            BuildingsMesh.transform.parent = child;
            Buildings = new GameObject ("Buildings");
            Buildings.transform.parent = child;
            foreach (var building in geometry.Buildings)
            {
                foreach (var vertex in building.Shape.Vertices)
                {
                    vertices.Add (new Vector3 (vertex.x, 0, vertex.y));
                }
                new MeshUtils.Polygon (building.Description + "Base", vertices, 0.1f, BuildingMaterial, Buildings.transform, true);
                new MeshUtils.Polygon (building.Description, vertices, UnityEngine.Random.Range (2f, 4f), BuildingMaterial, BuildingsMesh.transform, false);
                vertices.Clear ();
            }
            DrawRoads (geometry, null);
            if (_options.Walls)
            {
                DrawWalls (geometry, null);
            }
            else
            {
                Walls = null;
            }

            if (_options.Overlay)
            {
                DrawOverlay (geometry, null);
            }

            return go;
        }

        private void DrawOverlay (TownGeometry geometry, StringBuilder sb)
        {
            List<Vector3> vertices = new List<Vector3> ();
            var overlays = new GameObject ("Overlays");
            overlays.transform.parent = child;
            foreach (var patch in geometry.Overlay)
            {
                foreach (var vertex in patch.Shape.Vertices)
                {
                    vertices.Add (new Vector3 (vertex.x, 0, vertex.y));
                }
                new MeshUtils.Polygon ("Patch", vertices, 0.05f, OverlayMaterial, overlays.transform, true);
                vertices.Clear ();
            }
        }

        private void DrawRoads (TownGeometry geometry, StringBuilder sb)
        {
            Roads = new GameObject ("Roads");
            Roads.transform.parent = child;
            foreach (var road in geometry.Roads)
            {
                Geom.Vector2 last = new Geom.Vector2 (0, 0);
                foreach (var current in road)
                {
                    if (last.x != 0 && last.y != 0)
                    {
                        new Cube ("Road", GetLineVertices (
                            last.x,
                            current.x,
                            last.y,
                            current.y,
                            2
                        ), 0.2f, RoadMaterial, Roads.transform);
                    }
                    last = current;
                }
            }
        }

        private void DrawWalls (TownGeometry geometry, StringBuilder sb)
        {
            WallsMesh = new GameObject ("WallsMesh");
            WallsMesh.transform.parent = child;
            Walls = new GameObject ("Walls");
            Walls.transform.parent = child;
            var replacedGates = new List<Geom.Vector2> ();
            foreach (var wall in geometry.Walls)
            {
                var start = wall.A;
                var end = wall.B;

                if (geometry.Gates.Contains (start))
                {
                    replacedGates.Add (start);
                    start = start + Geom.Vector2.Scale (end - start, 0.3f);
                    wall.A = start;
                    geometry.Gates.Add (start);
                }

                if (geometry.Gates.Contains (end))
                {
                    replacedGates.Add (end);
                    end = end - Geom.Vector2.Scale (end - start, 0.3f);
                    wall.B = end;
                    geometry.Gates.Add (end);
                }
                new Cube ("Wall", GetLineVertices (
                    start.x,
                    end.x,
                    start.y,
                    end.y
                ), 0.1f, WallMaterial, Walls.transform);
                new Cube ("WallMesh", GetLineVertices (
                    start.x,
                    end.x,
                    start.y,
                    end.y
                ), 4, WallMaterial, WallsMesh.transform, false);
            }

            foreach (var replacedGate in replacedGates.Distinct ())
            {
                geometry.Gates.Remove (replacedGate);
            }

            foreach (var tower in geometry.Towers)
            {
                new Cube ("Tower", GetVertices (4, 4, tower.x - 2, tower.y - 2), 0.1f, TowerMaterial, Walls.transform);
                new Cube ("TowerMesh", GetVertices (4, 4, tower.x - 2, tower.y - 2), 8, TowerMaterial, WallsMesh.transform, false);
            }

            foreach (var gate in geometry.Gates)
            {
                new Cube ("Gate", GetVertices (4, 4, gate.x - 2, gate.y - 2), 0.1f, GateMaterial, Walls.transform);
                new Cube ("GateMesh", GetVertices (4, 4, gate.x - 2, gate.y - 2), 6, GateMaterial, WallsMesh.transform, false);
            }
        }

        private Vector3[] GetLineVertices (float startX, float endX, float startY, float endY, float thickness = 1f)
        {
            var p1 = new Vector3 (startX, 0, startY);
            var p2 = new Vector3 (endX, 0, endY);
            var dir = (p1 - p2).normalized;
            var norm = Vector3.Cross (dir, Vector3.up);
            var halfThickness = (norm * thickness) / 2;
            var p3 = p2 + halfThickness;
            var p4 = p1 + halfThickness + dir / 2;
            p1 = p1 - halfThickness + dir / 2;
            p2 = p2 - halfThickness;
            return new Vector3[]
            {
                p1,
                p2,
                p3,
                p4
            };
        }

        private Vector3[] GetVertices (int width, int length, float offsetX, float offsetZ)
        {
            return new Vector3[]
            {
                new Vector3 (offsetX, 0, offsetZ),
                    new Vector3 (offsetX, 0, offsetZ + length),
                    new Vector3 (offsetX + width, 0, offsetZ + length),
                    new Vector3 (offsetX + width, 0, offsetZ)
            };
        }
    }
}