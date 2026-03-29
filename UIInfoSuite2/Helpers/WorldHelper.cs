using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;

namespace UIInfoSuite2.Helpers;

public class WorldHelper(IMonitor logger)
{
  private enum WorldSelector
  {
    ObjectsInViewport,
    BuildingsInViewport,
  }

  private readonly PerScreen<Dictionary<WorldSelector, int>> _lastProcessedTick = new(() =>
    new Dictionary<WorldSelector, int>()
  );
  private readonly PerScreen<List<Object>> _viewportObjects = new(() => []);
  private readonly PerScreen<List<Building>> _buildingsInViewport = new(() => []);

  private bool IsStale(WorldSelector selector)
  {
    if (!_lastProcessedTick.Value.TryGetValue(selector, out int value))
    {
      return true;
    }
    return Game1.ticks - value > 10;
  }

  public static Rectangle GetTileViewport()
  {
    Rectangle gameViewport = Game1.viewport.ToXna();
    int startX = gameViewport.X / Game1.tileSize - 1;
    int startY = gameViewport.Y / Game1.tileSize - 1;
    int endX = (gameViewport.X + gameViewport.Width) / Game1.tileSize + 1;
    int endY = (gameViewport.Y + gameViewport.Height) / Game1.tileSize + 1;
    return new Rectangle(startX, startY, endX - startX, endY - startY);
  }

  public List<Object> GetObjectsInViewport()
  {
    if (!IsStale(WorldSelector.ObjectsInViewport))
    {
      return _viewportObjects.Value;
    }

    _viewportObjects.Value.Clear();
    Rectangle tileViewport = GetTileViewport();

    foreach ((Vector2 tile, Object obj) in Game1.currentLocation.Objects.Pairs)
    {
      if (!tileViewport.Contains((int)tile.X, (int)tile.Y))
      {
        continue;
      }
      _viewportObjects.Value.Add(obj);
    }

    _lastProcessedTick.Value[WorldSelector.ObjectsInViewport] = Game1.ticks;
    return _viewportObjects.Value;
  }

  public List<Building> GetBuildingsInViewport()
  {
    if (!IsStale(WorldSelector.BuildingsInViewport))
    {
      return _buildingsInViewport.Value;
    }

    _buildingsInViewport.Value.Clear();
    Rectangle tileViewport = GetTileViewport();

    foreach (Building building in Game1.currentLocation.buildings)
    {
      Vector2 buildingCenter;
      if (building is FishPond fishPond)
      {
        buildingCenter = fishPond.GetCenterTile();
      }
      else
      {
        buildingCenter = new Vector2(
          building.tileX.Value + (building.tilesWide.Value / 2f) + 1,
          building.tileY.Value + (building.tilesHigh.Value / 2f) + 1
        );
      }

      if (!tileViewport.Contains(buildingCenter))
      {
        continue;
      }
      _buildingsInViewport.Value.Add(building);
    }

    _lastProcessedTick.Value[WorldSelector.BuildingsInViewport] = Game1.ticks;
    return _buildingsInViewport.Value;
  }
}
