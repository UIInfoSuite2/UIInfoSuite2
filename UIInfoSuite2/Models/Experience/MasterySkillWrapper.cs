using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Layout.Measurement;

namespace UIInfoSuite2.Models.Experience;

internal class MasterySkillWrapper : SkillWrapperBase
{
  private static readonly Color _color = new(0x45, 0x09, 0x72);
  private readonly AspectLockedDimensions _dimensions = new(
    TextureHelper.MasteryIconRectangle,
    SkillIconSize,
    PrimaryDimension.Width
  );

  public override int GetMaxLevel()
  {
    return 5;
  }

  public override int GetUnmodifiedSkillLevel()
  {
    return MasteryTrackerMenu.getCurrentMasteryLevel();
  }

  public override int GetBaseExperienceForLevel(int level)
  {
    return MasteryTrackerMenu.getMasteryExpNeededForLevel(level);
  }

  public override int GetTotalExperience()
  {
    return (int)Game1.stats.Get("MasteryExp");
  }

  public override Texture2D GetTexture()
  {
    return Game1.mouseCursors;
  }

  public override Rectangle GetTextureRect()
  {
    return TextureHelper.MasteryIconRectangle;
  }

  public override Color GetColor()
  {
    return _color;
  }

  public override float GetIconScale()
  {
    return _dimensions.ScaleFactor;
  }
}
