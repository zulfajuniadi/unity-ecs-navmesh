using System.Collections.Generic;
using System.Linq;
using System.Text;
using Town.Geom;

namespace Town
{
    public class TownRenderer
    {
        private readonly Town _town;
        private readonly TownOptions _options;

        public TownRenderer (Town town, TownOptions options = null)
        {
            if (options == null)
            {
                options = TownOptions.Default;
            }

            _town = town;
            _options = options;
        }

        public string DrawTown ()
        {
            var bounds = _town.GetCityWallsBounds ().Expand (100);

            var sb = new StringBuilder (@"<?xml version=""1.0"" standalone=""yes""?>");
            sb.Append ($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1500\" height=\"1500\" viewBox=\"{bounds.ToSvgViewport()}\">");

            sb.Append (@"<defs><style type=""text/css""><![CDATA[");
            sb.Append (".building { stroke-width: 0.1; stroke: #777; fill: #DDD; }");
            sb.Append (".building-description { visibility: hidden; font-size: 4; font-family: Verdana; fill: #666; }");
            sb.Append (".wall { stroke-width: 5; stroke: black; fill: none; }");
            sb.Append (".road-outer { stroke-width: 3.6; stroke: black; fill: none; }");
            sb.Append (".road-inner { stroke-width: 3; stroke: white; fill: none; }");
            sb.Append (".tower { fill: black; }");
            sb.Append (".water { fill: #7799FF; fill-opacity: 0.6; stroke: none; }");
            sb.Append (".overlay { fill: #FFFF00; fill-opacity: 0.2; stroke: black; stroke-width: 0.5; }");
            sb.Append ("]]></style></defs>");

            sb.Append (@"<rect width=""1500"" height=""1500"" x=""0"" y=""0"" fill=""#FCFCFA"" />");

            var geometry = _town.GetTownGeometry (_options);

            foreach (var water in geometry.Water)
            {
                sb.Append (water.ToSvgPolygon ("water"));
            }

            var id = 0;
            foreach (var building in geometry.Buildings)
            {
                var hover = $"onmouseover =\"document.getElementById('building{id}').style.visibility = 'visible'\" onmouseout=\"document.getElementById('building{id}').style.visibility = 'hidden'\"";
                sb.Append (building.Shape.ToSvgPolygon ("building", hover));
                id++;
            }

            DrawRoads (geometry, sb);
            DrawWalls (geometry, sb);

            id = 0;
            foreach (var building in geometry.Buildings)
            {
                sb.Append ("<text z-index=\"9\" id=\"building" + id + "\" class=\"building-description\" x=\"" + building.Shape.Vertices[0].x + "\" y=\"" + building.Shape.Vertices[0].y +
                    "\">" + building.Description + "</text>");
                id++;
            }

            if (_options.Overlay)
            {
                DrawOverlay (geometry, sb);
            }

            //sb.Append(geometry.WaterBorder.ToSvgPolygon("road-outer"));

            sb.Append ($"<text x=\"{bounds.X + 20}\" y=\"{bounds.Y + 30}\">" + _options.Seed + "</text>");

            sb.Append ("</svg>");
            return sb.ToString ();
        }

        private static void DrawOverlay (TownGeometry geometry, StringBuilder sb)
        {
            foreach (var patch in geometry.Overlay)
            {
                sb.Append (patch.Shape.ToSvgPolygon ("overlay", $" id=\"{patch.Id}\""));
            }
        }

        private static void DrawRoads (TownGeometry geometry, StringBuilder sb)
        {
            foreach (var road in geometry.Roads)
            {
                var path = string.Join (" ", road.Select (v => v.ToString ()));
                sb.Append ($"<polyline class=\"road-outer\" points=\"{path}\"  />");
                sb.Append ($"<polyline class=\"road-inner\" points=\"{path}\"  />");
            }
        }

        private static void DrawWalls (TownGeometry geometry, StringBuilder sb)
        {
            var replacedGates = new List<Vector2> ();
            foreach (var wall in geometry.Walls)
            {
                var start = wall.A;
                var end = wall.B;

                if (geometry.Gates.Contains (start))
                {
                    replacedGates.Add (start);
                    start = start + Vector2.Scale (end - start, 0.3f);
                    wall.A = start;
                    geometry.Gates.Add (start);
                }

                if (geometry.Gates.Contains (end))
                {
                    replacedGates.Add (end);
                    end = end - Vector2.Scale (end - start, 0.3f);
                    wall.B = end;
                    geometry.Gates.Add (end);
                }
                sb.Append ($"<line x1=\"{start.x}\" y1=\"{start.y}\" x2=\"{end.x}\" y2=\"{end.y}\" class=\"wall\" />");
            }

            foreach (var replacedGate in replacedGates.Distinct ())
            {
                geometry.Gates.Remove (replacedGate);
            }

            foreach (var tower in geometry.Towers)
            {
                sb.Append ($"<rect width=\"8\" height=\"8\" x=\"{tower.x - 4}\" y=\"{tower.y - 4}\" class=\"tower\" />");
            }

            foreach (var gate in geometry.Gates)
            {
                sb.Append ($"<rect width=\"8\" height=\"8\" x=\"{gate.x - 4}\" y=\"{gate.y - 4}\" class=\"gate\" />");
            }
        }
    }
}