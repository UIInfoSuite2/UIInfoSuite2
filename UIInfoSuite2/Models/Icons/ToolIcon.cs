using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace UIInfoSuite2.Models.Icons;

internal class ToolIcon : ClickableIcon
{
  private Tool? _tool;

  public ToolIcon()
    : base(Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 40)
  {
    UpdateTool();
  }

  public void UpdateTool()
  {
    if (_tool == Game1.player.toolBeingUpgraded.Value)
    {
      return;
    }

    _tool = Game1.player.toolBeingUpgraded.Value;
    if (_tool is null)
    {
      return;
    }

    ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(_tool.QualifiedItemId);
    if (itemData.IsErrorItem)
    {
      Logger.LogOnce(
        $"ToolIcon: Tool {_tool.QualifiedItemId} did not return valid item data for some reason, was the mod removed?",
        LogLevel.Alert
      );
    }
    BaseTexture = itemData.GetTexture();
    SetSourceBounds(itemData.GetSourceRect());
    UpdateHoverText();
  }

  public void UpdateHoverText()
  {
    if (_tool is null)
    {
      return;
    }

    if (Game1.player.daysLeftForToolUpgrade.Value > 0)
    {
      HoverText = string.Format(
        I18n.DaysUntilToolIsUpgraded(),
        Game1.player.daysLeftForToolUpgrade.Value,
        _tool.DisplayName
      );
    }
    else
    {
      HoverText = string.Format(I18n.ToolIsFinishedBeingUpgraded(), _tool.DisplayName);
    }
  }

  protected override bool _ShouldDraw()
  {
    return _tool is not null;
  }
}
