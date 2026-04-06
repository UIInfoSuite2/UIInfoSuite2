using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FruitTrees;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using UIInfoSuite2.Extensions;
using UIInfoSuite2.Helpers.GameStateHelpers;
using UIInfoSuite2.Utilities;
using Object = StardewValley.Object;

namespace UIInfoSuite2.Helpers;

public record DropInfo(string? Condition, float Chance, string ItemId)
{
  public int? GetNextDay(bool includeToday)
  {
    return DropsHelper.GetNextDay(Condition, includeToday);
  }
}

internal record PossibleDroppedItem(
  ConditionFutureResult FutureHarvestDates,
  ParsedItemData Item,
  float Chance,
  string? CustomId = null
)
{
  public bool ReadyToPick => FutureHarvestDates.HasDate(Game1.Date);
}

internal record FruitTreeInfo(string TreeName, List<PossibleDroppedItem> Items);

internal class DropsHelper
{
  private readonly Dictionary<string, string> _cropNamesCache = new();
  private readonly GameStateHelper _gameStateHelper;
  private readonly IMonitor _logger;

  public DropsHelper(IMonitor logger, GameStateHelper gameStateHelper)
  {
    _logger = logger;
    _gameStateHelper = gameStateHelper;
  }

  public static int? GetNextDay(string? condition, bool includeToday)
  {
    return string.IsNullOrEmpty(condition)
      ? Game1.dayOfMonth + (includeToday ? 0 : 1)
      : Tools.GetNextDayFromCondition(condition, includeToday);
  }

  public static int? GetLastDay(string? condition)
  {
    return Tools.GetLastDayFromCondition(condition);
  }

  public static string? GetCropHarvestItemId(Crop crop)
  {
    if (crop.forageCrop.Value)
    {
      return crop.whichForageCrop.Value switch
      {
        "1" => "399", // Spring Onion
        "2" => "829", // Ginger
        _ => crop.whichForageCrop.Value,
      };
    }

    if (crop.indexOfHarvest.Value is null)
    {
      return null;
    }

    return crop.isWildSeedCrop() ? crop.whichForageCrop.Value : crop.indexOfHarvest.Value;
  }

  public string GetCropHarvestName(Crop crop)
  {
    string? itemId = GetCropHarvestItemId(crop);
    return itemId is null ? "Unknown Crop" : GetOrCacheCropName(itemId);
  }

  private string GetOrCacheCropName(string itemId)
  {
    if (_cropNamesCache.TryGetValue(itemId, out string? harvestName))
    {
      return harvestName;
    }

    // Technically has the best compatibility for looking up items vs ItemRegistry.
    harvestName = new Object(itemId, 1).DisplayName;
    _cropNamesCache.Add(itemId, harvestName);

    return harvestName;
  }

  public List<PossibleDroppedItem> GetFruitTreeDropItems(FruitTree tree)
  {
    FruitTreeData? treeData = tree.GetData();
    return GetGenericDropItems(treeData.Fruit, null, "Fruit Tree", FruitTreeDropConverter);

    DropInfo FruitTreeDropConverter(FruitTreeFruitData input)
    {
      List<string> conditions = new();
      conditions.AddIfNotNull(input.Condition);

      if (input.Season.HasValue)
      {
        conditions.Add($"SEASON {Utility.getSeasonKey(input.Season.Value)}");
      }
      else if (treeData.Seasons.Count != 0)
      {
        var seasonsCondition =
          $"SEASON {string.Join(' ', treeData.Seasons.Select(Utility.getSeasonKey))}";
        conditions.Add(seasonsCondition);
      }

      return new DropInfo(string.Join(", ", conditions), input.Chance, input.ItemId);
    }
  }

  public FruitTreeInfo GetFruitTreeInfo(FruitTree tree)
  {
    var treeData = tree.GetData();
    string? displayName = null;

    if (treeData?.DisplayName != null)
    {
      displayName = TokenParser.ParseText(treeData.DisplayName);

      if (displayName.Contains("(no translation:"))
      {
        displayName = null;
      }
    }

    if (string.IsNullOrEmpty(displayName))
    {
      var itemData = ItemRegistry.GetData(tree.treeId.Value);
      if (itemData != null)
      {
        displayName = itemData.DisplayName;
      }
    }

    List<PossibleDroppedItem> drops = GetFruitTreeDropItems(tree);

    if (string.IsNullOrEmpty(displayName) && drops.Count > 0)
    {
      displayName = drops[0].Item.DisplayName;
    }

    if (string.IsNullOrEmpty(displayName))
    {
      displayName = tree.treeId.Value;
    }

    string cleanName = displayName.Replace(" Sapling", "");
    string treeSuffix = I18n.Tree();

    string finalName =
      cleanName.EndsWith(treeSuffix.Trim(), StringComparison.OrdinalIgnoreCase)
      || cleanName.EndsWith("Tree", StringComparison.OrdinalIgnoreCase)
        ? cleanName
        : $"{cleanName}{treeSuffix}";

    return new FruitTreeInfo(finalName, drops);
  }

  public List<PossibleDroppedItem> GetGenericDropItems<T>(
    IEnumerable<T> drops,
    string? customId,
    string displayName,
    Func<T, DropInfo> extractDropInfo,
    bool independentRolls = false
  )
  {
    List<PossibleDroppedItem> items = new();

    foreach (T drop in drops)
    {
      DropInfo dropInfo = extractDropInfo(drop);
      ConditionFutureResult validDays = _gameStateHelper.ResolveQueryFuture(
        dropInfo.Condition ?? ""
      );
      string nextDayStr = validDays.GetNextDate()?.ToString() ?? "No Next Date";

      if (!validDays.ErroredConditions.IsEmpty())
      {
        foreach (string erroredCondition in validDays.ErroredConditions)
        {
          _logger.LogOnce(
            $"Couldn't parse the next day the {displayName} will drop {dropInfo.ItemId}. Condition: {erroredCondition}. Please report this error.",
            LogLevel.Error
          );
        }

        continue;
      }

      ParsedItemData? itemData = ItemRegistry.GetData(dropInfo.ItemId);
      if (itemData == null)
      {
        _logger.Log(
          $"Couldn't parse the correct item {displayName} will drop. ItemId: {dropInfo.ItemId}. Please report this error.",
          LogLevel.Error
        );
        continue;
      }

      items.Add(new PossibleDroppedItem(validDays, itemData, dropInfo.Chance, customId));
    }

    return items;
  }
}
