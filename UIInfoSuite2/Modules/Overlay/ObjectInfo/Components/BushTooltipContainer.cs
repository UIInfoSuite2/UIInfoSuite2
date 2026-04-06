using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Compatibility.CustomBush;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Helpers.GameStateHelpers;
using UIInfoSuite2.Layout;
using UIInfoSuite2.Models.Tooltip;

namespace UIInfoSuite2.Modules.Overlay.ObjectInfo.Components;

internal class BushContext
{
  private static readonly Lazy<string> TeaBushName = new(() =>
    ItemRegistry.GetData("(O)251").DisplayName
  );
  public static readonly Lazy<ParsedItemData> TeaLeafData = new(() =>
    ItemRegistry.GetData("(O)815")
  );

  private readonly ICustomBushApi? _customBushApi;
  private readonly ICustomBushData? _customBushData;
  private readonly string? _customBushId;
  private readonly DropsHelper _dropsHelper;
  public readonly Bush Bush;
  public readonly List<PossibleDroppedItem> DroppedItems = new();
  private bool _inProductionPeriod;

  public BushContext(Bush bush)
  {
    _dropsHelper = ModEntry.GetSingleton<DropsHelper>();
    Bush = bush;

    var apiManager = ModEntry.GetSingleton<ApiManager>();
    if (
      apiManager.GetApi(ModCompat.CustomBush, out _customBushApi)
      && _customBushApi.TryGetBush(bush, out _customBushData, out _customBushId)
    )
    {
      PopulateCustomBush();
    }
    else
    {
      PopulateNormalBush();
    }
  }

  public int AgeToMature { get; private set; } = 20;

  public bool IsMature => Bush.getAge() >= AgeToMature;
  public bool IsReadyToday { get; private set; }
  public bool WillProduceThisSeason { get; private set; }
  public string BushName { get; private set; } = TeaBushName.Value;

  public bool InProductionPeriod
  {
    get => _inProductionPeriod;
    private set
    {
      _inProductionPeriod = value;
      DaysUntilProductionPeriod = InProductionPeriod ? 0 : 22 - Game1.dayOfMonth;
    }
  }

  public int DaysUntilProductionPeriod { get; private set; }

  [MemberNotNullWhen(true, nameof(_customBushData))]
  [MemberNotNullWhen(true, nameof(_customBushApi))]
  public bool IsCustomBush => _customBushApi != null && _customBushData != null;

  private void PopulateNormalBush()
  {
    DroppedItems.Clear();
    AgeToMature = 20;
    IsReadyToday = Bush.tileSheetOffset.Value == 1;
    WillProduceThisSeason = Game1.season != Season.Winter || Bush.IsSheltered();
    BushName = TeaBushName.Value;
    InProductionPeriod = Game1.dayOfMonth >= 22;

    if (IsReadyToday)
    {
      DroppedItems.Add(
        new PossibleDroppedItem(ConditionFutureResult.Today(), TeaLeafData.Value, 1.0f)
      );
    }
    else if (Game1.dayOfMonth >= 21 && Game1.dayOfMonth < 28)
    {
      DroppedItems.Add(
        new PossibleDroppedItem(ConditionFutureResult.Tomorrow(), TeaLeafData.Value, 1.0f)
      );
    }
  }

  private void PopulateCustomBush()
  {
    if (!IsCustomBush)
    {
      throw new InvalidOperationException("Custom bush is not initialized, but was expected");
    }

    DroppedItems.Clear();
    AgeToMature = _customBushData.AgeToProduce;
    WillProduceThisSeason = _customBushData.Seasons.Contains(Game1.season) || Bush.IsSheltered();
    InProductionPeriod = Game1.dayOfMonth >= _customBushData.DayToBeginProducing;

    if (_customBushData.GetShakeOffItemIfReady(Bush, out Item? shakeOffItem))
    {
      DroppedItems.Add(
        new PossibleDroppedItem(
          ConditionFutureResult.Today(),
          ItemRegistry.GetDataOrErrorItem(shakeOffItem.QualifiedItemId),
          1.0f,
          _customBushId
        )
      );
      IsReadyToday = true;
    }
    else
    {
      DroppedItems.AddRange(_customBushApi.GetCustomBushDropItems(_customBushData, _customBushId));
    }

    BushName = ResolveCustomBushName();
  }

  private string ResolveCustomBushName()
  {
    if (!IsCustomBush)
    {
      return TeaBushName.Value;
    }

    if (DroppedItems.Count == 1)
    {
      string suffix = IsMature ? "Bush" : "Sapling";
      return $"{DroppedItems[0].Item.DisplayName} {suffix}";
    }

    string displayName = _customBushData.DisplayName;
    if (displayName.Contains("LocalizedText"))
    {
      displayName = TokenParser.ParseText(displayName);
    }

    return displayName;
  }
}

internal class BushTooltipContainer : LayoutContainer
{
  private readonly TooltipText _bushDaysRemainingElement = new(
    "UIIS2::UnknownTime",
    0.75f,
    identifier: "BushTimeRemaining"
  );

  private readonly TooltipIcon _bushIcon = new(
    Game1.mouseCursors,
    new Rectangle(322, 498, 12, 12),
    40
  );

  private readonly TooltipText _bushNameElement = TooltipText.Bold(
    "UIIS2::UnknownBush",
    identifier: "BushName"
  );

  private readonly TooltipText _doesNotProduceElement = new(
    I18n.DoesNotProduceThisSeason(),
    0.75f,
    identifier: "BushDoesNotProduce"
  );

  private readonly TooltipText _dropsText = new(
    "UIIS2::UnknownDrops",
    identifier: "BushDrops",
    scale: 0.75f
  );
  private BushContext? _bushContext;

  public BushTooltipContainer()
    : base("BushTooltip")
  {
    Direction = LayoutDirection.Row;

    ComponentSpacing = 10;
    AddChildren(
      Column(null, _bushNameElement, _bushDaysRemainingElement, _dropsText, _doesNotProduceElement),
      _bushIcon
    );

    _doesNotProduceElement.IsHidden = true;
    IsHidden = true;
  }

  public Bush? Bush
  {
    get => _bushContext?.Bush;
    set => SetBush(value);
  }

  public void SetBush(Bush? newBush, bool force = false)
  {
    if (newBush == _bushContext?.Bush && !force)
    {
      return;
    }

    _bushContext = newBush is null ? null : new BushContext(newBush);
    UpdateBush();
  }

  public void ForceUpdate()
  {
    SetBush(Bush, true);
    ModEntry.DebugLog("BushTooltipContainer forced update");
  }

  private void UpdateBushIcon()
  {
    if (_bushContext is null)
    {
      return;
    }

    ParsedItemData? iconData = null;

    if (_bushContext.IsCustomBush && _bushContext.DroppedItems.Count > 0)
    {
      iconData = _bushContext.DroppedItems[0].Item;
    }
    else if (!_bushContext.IsCustomBush)
    {
      iconData = BushContext.TeaLeafData.Value;
    }

    if (iconData != null)
    {
      _bushIcon.SetIcon(iconData.GetTexture(), iconData.GetSourceRect(), 40);
    }
  }

  private void SetTextElement(TooltipText element, string newName)
  {
    element.Text = newName;
    element.IsHidden = false;
  }

  private void HideAll()
  {
    _bushNameElement.IsHidden = false;
    _doesNotProduceElement.IsHidden = true;
    _bushDaysRemainingElement.IsHidden = true;
    _dropsText.IsHidden = true;
  }

  private void UpdateBush()
  {
    HideAll();

    if (Bush is null || _bushContext is null)
    {
      ModEntry.LayoutDebug("Bush is null, skipping render");
      IsHidden = true;
      return;
    }

    IsHidden = false;
    SetTextElement(_bushNameElement, _bushContext.BushName);
    UpdateBushIcon();

    if (!_bushContext.IsMature || !_bushContext.WillProduceThisSeason)
    {
      if (!_bushContext.IsMature)
      {
        SetTextElement(
          _bushDaysRemainingElement,
          $"{_bushContext.AgeToMature - Bush.getAge()} {I18n.DaysToMature()}"
        );
      }

      if (!_bushContext.WillProduceThisSeason)
      {
        _doesNotProduceElement.IsHidden = false;
      }

      return;
    }

    // Too early in the season to produce
    if (!_bushContext.InProductionPeriod)
    {
      SetTextElement(
        _bushDaysRemainingElement,
        $"{_bushContext.DaysUntilProductionPeriod} {I18n.Days()}"
      );
      return;
    }

    if (_bushContext.IsReadyToday)
    {
      var grouped = _bushContext.DroppedItems.Select(item => item.Item.DisplayName).Distinct();

      SetTextElement(_dropsText, $"{I18n.ReadyToHarvest()}\n{string.Join("\n", grouped)}");
    }
    else
    {
      _dropsText.IsHidden = true;
    }
  }
}
