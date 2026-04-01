using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Compatibility.SpaceCore;
using UIInfoSuite2.Layout.Measurement;

namespace UIInfoSuite2.Models.Experience;

internal class SpaceCoreSkillWrapper(SpaceCoreSkill skillInstance) : SkillWrapperBase
{
  private static readonly Rectangle _defaultRect = new(0, 0, 16, 16);
  private readonly SpaceCoreHelper _spaceCoreHelper = ModEntry.GetSingleton<SpaceCoreHelper>();

  private readonly AspectLockedDimensions _dimensions = new(
    _defaultRect,
    SkillIconSize,
    PrimaryDimension.Width
  );

  public override int GetUnmodifiedSkillLevel()
  {
    return _spaceCoreHelper.GetSkillLevel(Game1.player, skillInstance.Id);
  }

  public override int GetBaseExperienceForLevel(int level)
  {
    return skillInstance.GetBaseExperienceForLevel(level);
  }

  public override int GetTotalExperience()
  {
    return skillInstance.GetTotalCurrentExperience();
  }

  public override Texture2D GetTexture()
  {
    return skillInstance.Icon;
  }

  public override Rectangle GetTextureRect()
  {
    return _defaultRect;
  }

  public override Color GetColor()
  {
    return skillInstance.ExperienceBarColor;
  }

  public override float GetIconScale()
  {
    return _dimensions.ScaleFactor;
  }
}
