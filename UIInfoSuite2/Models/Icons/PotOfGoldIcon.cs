using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using UIInfoSuite2.Layout.Measurement;

namespace UIInfoSuite2.Models.Icons;

internal class PotOfGoldIcon()
  : ClickableIcon(Game1.mouseCursors_1_6, new Rectangle(131, 0, 16, 16), 40)
{
  // Pot of Gold Position in Forest
  private static readonly Vector2 _potPosition = new(52f, 98f);
  private readonly Lazy<GameLocation?> _forest = new(() => Game1.getLocationFromName("Forest"));
  private bool _potOfGoldOnMap = false;

  public void Update()
  {
    _potOfGoldOnMap = IsPotOfGoldStillThere();
  }

  private bool IsPotOfGoldStillThere()
  {
    GameLocation? forest = _forest.Value;
    if (forest == null)
      return false;

    if (forest.objects.TryGetValue(_potPosition, out StardewValley.Object obj))
    {
      return obj.QualifiedItemId == "(O)PotOfGold";
    }

    return false;
  }

  protected override bool _ShouldDraw()
  {
    return Config.ShowPotOfGoldIcon && _potOfGoldOnMap && base._ShouldDraw();
  }
}
