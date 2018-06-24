using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Town.Geom;

namespace Town
{
    public class Building
    {
        public Building (string description, Geom.Polygon shape)
        {
            Shape = shape;
            Description = description;
        }

        public Polygon Shape { get; }
        public string Description { get; }
    }

    public class BuildingPlacer
    {
        private readonly List<Geom.Polygon> _buildings;
        private readonly float _avgPopulationPrBuilding;

        public BuildingPlacer (List<Geom.Polygon> buildings, float avgPopulationPrBuilding = 10)
        {
            _buildings = buildings.OrderByDescending (b => b.Area ()).ToList ();
            _avgPopulationPrBuilding = avgPopulationPrBuilding;
        }

        public List<Building> PopulateBuildings ()
        {
            var populated = new List<Building> ();

            var estimatedPopulation = (int) (_avgPopulationPrBuilding * _buildings.Count);

            var allBuildingTypes = Enum
                .GetValues (typeof (BuildingType))
                .OfType<BuildingType> ()
                .Select (bt => typeof (BuildingType).GetField (bt.ToString ()).GetCustomAttribute<BuildingStatsAttribute> ())
                .Where (bt => bt.Population <= estimatedPopulation)
                .OrderByDescending (bt => bt.Population)
                .ThenByDescending (bt => bt.MinSize)
                .ToList ();

            foreach (var buildingType in allBuildingTypes)
            {
                var numOfType = (int) Math.Ceiling (estimatedPopulation / (float) buildingType.Population);
                for (var i = 0; i < numOfType; i++)
                {
                    if (_buildings.Any ())
                    {
                        var building = new Building (buildingType.Description, _buildings[0]);
                        populated.Add (building);
                        _buildings.RemoveAt (0);
                    }
                }
            }

            return populated;
        }

    }

    public enum BuildingType
    {
        [BuildingStats (MinSize = 1, Population = 1, Description = "Empty")] Empty, [BuildingStats (MinSize = 1, Population = 10, Description = "Home")] Home, [BuildingStats (MinSize = 1, Population = 150, Description = "Shoemakers")] Shoemakers, [BuildingStats (MinSize = 1, Population = 250, Description = "Furriers")] Furriers, [BuildingStats (MinSize = 1, Population = 250, Description = "Maidservants")] Maidservants, [BuildingStats (MinSize = 1, Population = 250, Description = "Tailors")] Tailors, [BuildingStats (MinSize = 1, Population = 350, Description = "Barbers")] Barbers, [BuildingStats (MinSize = 1, Population = 350, Description = "Healer")] Healer, [BuildingStats (MinSize = 1, Population = 400, Description = "Jewelers")] Jewelers, [BuildingStats (MinSize = 1, Population = 400, Description = "Old-Clothes")] OldClothes, [BuildingStats (MinSize = 2, Population = 400, Description = "Taverns/Restaurants")] TavernsRestaurants, [BuildingStats (MinSize = 1, Population = 500, Description = "Masons")] Masons, [BuildingStats (MinSize = 1, Population = 500, Description = "Pastrycooks")] Pastrycooks, [BuildingStats (MinSize = 1, Population = 500, Description = "Shrine")] Shrine, [BuildingStats (MinSize = 2, Population = 550, Description = "Carpenters")] Carpenters, [BuildingStats (MinSize = 2, Population = 600, Description = "Weavers")] Weavers, [BuildingStats (MinSize = 2, Population = 700, Description = "Barrel maker (cooper)")] Barrelmaker, [BuildingStats (MinSize = 1, Population = 700, Description = "Chandlers")] Chandlers, [BuildingStats (MinSize = 2, Population = 700, Description = "Textile trader (mercer)")] Textiletrader, [BuildingStats (MinSize = 2, Population = 800, Description = "Bakers")] Bakers, [BuildingStats (MinSize = 1, Population = 850, Description = "Scabbardmakers")] Scabbardmakers, [BuildingStats (MinSize = 1, Population = 850, Description = "Watercarriers")] Watercarriers, [BuildingStats (MinSize = 1, Population = 900, Description = "Wine-Sellers")] WineSellers, [BuildingStats (MinSize = 1, Population = 950, Description = "Hatmakers")] Hatmakers, [BuildingStats (MinSize = 3, Population = 1000, Description = "Chicken Butchers")] ChickenButchers, [BuildingStats (MinSize = 2, Population = 1000, Description = "Saddlers")] Saddlers, [BuildingStats (MinSize = 2, Population = 1100, Description = "Pursemakers")] Pursemakers, [BuildingStats (MinSize = 3, Population = 1200, Description = "Butchers")] Butchers, [BuildingStats (MinSize = 2, Population = 1200, Description = "Fishmongers")] Fishmongers, [BuildingStats (MinSize = 2, Population = 1400, Description = "Beer-Sellers")] BeerSellers, [BuildingStats (MinSize = 2, Population = 1400, Description = "Buckle Makers")] BuckleMakers, [BuildingStats (MinSize = 1, Population = 1400, Description = "Plasterers")] Plasterers, [BuildingStats (MinSize = 1, Population = 1400, Description = "Spice Merchants")] SpiceMerchants, [BuildingStats (MinSize = 2, Population = 1500, Description = "Blacksmiths")] Blacksmiths, [BuildingStats (MinSize = 1, Population = 1500, Description = "Painters")] Painters, [BuildingStats (MinSize = 2, Population = 1700, Description = "Doctors")] Doctors, [BuildingStats (MinSize = 1, Population = 1800, Description = "Roofers")] Roofers, [BuildingStats (MinSize = 3, Population = 1900, Description = "Bathers")] Bathers, [BuildingStats (MinSize = 1, Population = 1900, Description = "Locksmiths")] Locksmiths, [BuildingStats (MinSize = 2, Population = 1900, Description = "Ropemakers")] Ropemakers, [BuildingStats (MinSize = 2, Population = 2000, Description = "Copyists")] Copyists, [BuildingStats (MinSize = 2, Population = 2000, Description = "Harness-Makers")] HarnessMakers, [BuildingStats (MinSize = 3, Population = 2000, Description = "Inns")] Inns, [BuildingStats (MinSize = 2, Population = 2000, Description = "Rugmakers")] Rugmakers, [BuildingStats (MinSize = 3, Population = 2000, Description = "Sculptors")] Sculptors, [BuildingStats (MinSize = 3, Population = 2000, Description = "Tanners")] Tanners, [BuildingStats (MinSize = 3, Population = 2100, Description = "Bleachers")] Bleachers, [BuildingStats (MinSize = 2, Population = 2300, Description = "Cutlers")] Cutlers, [BuildingStats (MinSize = 3, Population = 2300, Description = "Hay Merchants")] HayMerchants, [BuildingStats (MinSize = 2, Population = 2400, Description = "Glovemakers")] Glovemakers, [BuildingStats (MinSize = 2, Population = 2400, Description = "Woodcarvers")] Woodcarvers, [BuildingStats (MinSize = 2, Population = 2400, Description = "Woodsellers")] Woodsellers, [BuildingStats (MinSize = 2, Population = 2800, Description = "Magic-Shops")] MagicShops, [BuildingStats (MinSize = 2, Population = 3000, Description = "Bookbinders")] Bookbinders, [BuildingStats (MinSize = 2, Population = 3900, Description = "Illuminators")] Illuminators, [BuildingStats (MinSize = 4, Population = 6000, Description = "Temple")] Temple, [BuildingStats (MinSize = 2, Population = 6300, Description = "Booksellers")] Booksellers, [BuildingStats (MinSize = 4, Population = 10000, Description = "University")] University,
    }

    public class BuildingStatsAttribute : Attribute
    {
        public int MinSize { get; set; }
        public int Population { get; set; }
        public string Description { get; set; }
    }
}