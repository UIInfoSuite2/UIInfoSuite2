using StardewValley.Buildings;
using StardewValley.TokenizableStrings;
using UIInfoSuite2.Layout;
using UIInfoSuite2.Layout.Measurement;
using UIInfoSuite2.Models.Tooltip;
using UIInfoSuite2.Modules.Overlay.ObjectInfo.Components.BuildingContainers;

namespace UIInfoSuite2.Modules.Overlay.ObjectInfo.Components;

internal class BuildingTooltipContainer : LayoutContainer
{
  private readonly TooltipText _buildingName = TooltipText.Bold(
    "UIIS2::UnknownBuilding",
    identifier: "BuildingName"
  );
  private readonly BuildingChestTooltipContainer _chestsContainer = new();
  private readonly FishPondTooltipContainer _fishPondContainer = new();
  private Building? _building;

  public BuildingTooltipContainer(Building? building = null)
    : base("BuildingTooltip")
  {
    Direction = LayoutDirection.Column;
    ComponentSpacing = 0;
    Margin.SetAll(0);
    AddChildren(_buildingName, _fishPondContainer, _chestsContainer);
    IsHidden = true;

    Building = building;
  }

  public Building? Building
  {
    get => _building;
    set => SetBuilding(value);
  }

  private void SetBuilding(Building? newBuilding)
  {
    if (ReferenceEquals(_building, newBuilding))
    {
      return;
    }

    _building = newBuilding;
    _fishPondContainer.Pond = newBuilding as FishPond;
    _chestsContainer.Building = newBuilding;
    if (Building is null)
    {
      ModEntry.LayoutDebug("Building is null, skipping render");
      IsHidden = true;
      return;
    }

    _buildingName.Text = TokenParser.ParseText(Building.GetData().Name);
    if (_fishPondContainer.IsHidden && _chestsContainer.IsHidden)
    {
      IsHidden = true;
      return;
    }

    IsHidden = false;
  }
}
