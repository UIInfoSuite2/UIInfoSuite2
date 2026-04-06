using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Layout;
using UIInfoSuite2.Models.Tooltip;

namespace UIInfoSuite2.Modules.Overlay.ObjectInfo.Components;

internal class FruitTreeTooltipContainer : LayoutContainer
{
  private readonly TooltipText _daysRemainingElement = new(
    "UIIS2::UnknownTime",
    0.75f,
    identifier: "TreeGrowTimeRemaining"
  );

  private readonly TooltipText _harvestElement = new(
    "UIIS2::UnknownDrops",
    0.75f,
    identifier: "TreeHarvestInfo"
  );

  private readonly TooltipIcon _cropIcon = new(
    Game1.mouseCursors,
    new Rectangle(322, 498, 12, 12),
    40
  );

  private readonly TooltipText _nameElement = TooltipText.Bold(
    "UIIS2::UnknownTree",
    identifier: "FruitTreeName"
  );
  private readonly DropsHelper _dropsHelper;
  private FruitTree? _fruitTree;

  public FruitTreeTooltipContainer(FruitTree? fruitTree = null)
    : base("FruitTreeTooltip")
  {
    _dropsHelper = ModEntry.GetSingleton<DropsHelper>();

    ComponentSpacing = 10;
    AddChildren(Column(null, _nameElement, _daysRemainingElement, _harvestElement), _cropIcon);
    IsHidden = true;

    FruitTree = fruitTree;
  }

  public FruitTree? FruitTree
  {
    get => _fruitTree;
    set => SetFruitTree(value);
  }

  private void SetFruitTree(FruitTree? fruitTree)
  {
    if (fruitTree == _fruitTree)
    {
      return;
    }

    _fruitTree = fruitTree;
    UpdateTree();
  }

  private void UpdateTree()
  {
    if (FruitTree is null)
    {
      ModEntry.LayoutDebug("Tree is null, skipping render");
      IsHidden = true;
      return;
    }

    IsHidden = false;

    FruitTreeInfo treeInfo = _dropsHelper.GetFruitTreeInfo(FruitTree);
    _nameElement.Text = treeInfo.TreeName;
    UpdateIcon();
    UpdateDaysToMature();
    UpdateHarvestInfo();
  }

  private void UpdateIcon()
  {
    if (FruitTree is null)
    {
      return;
    }

    ParsedItemData? itemData = ItemRegistry.GetData(FruitTree.treeId.Value);
    if (itemData != null)
    {
      _cropIcon.SetIcon(itemData.GetTexture(), itemData.GetSourceRect(), 40);
    }
  }

  private void UpdateDaysToMature()
  {
    if (FruitTree == null || FruitTree.daysUntilMature.Value <= 0)
    {
      _daysRemainingElement.IsHidden = true;
      return;
    }

    _daysRemainingElement.IsHidden = false;
    _daysRemainingElement.Text = $"{FruitTree.daysUntilMature.Value} {I18n.DaysToMature()}";
  }

  private void UpdateHarvestInfo()
  {
    if (FruitTree == null || FruitTree.fruit.Count == 0)
    {
      _harvestElement.IsHidden = true;
      return;
    }

    _harvestElement.IsHidden = false;

    var grouped = FruitTree
      .fruit.GroupBy(item => item.DisplayName)
      .Select(g => g.Count() > 1 ? $"{g.Key} x{g.Count()}" : g.Key);

    _harvestElement.Text = $"{I18n.ReadyToHarvest()}\n{string.Join("\n", grouped)}";
  }
}
