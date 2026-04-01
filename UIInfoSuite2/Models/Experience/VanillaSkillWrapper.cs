using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Layout.Measurement;

namespace UIInfoSuite2.Models.Experience;

internal class VanillaSkillWrapper : SkillWrapperBase
{
  private readonly AspectLockedDimensions _dimensions;

  private readonly int _skillType;
  private readonly Lazy<int[]> _vppExperienceCurve;

  public VanillaSkillWrapper(int skillType)
  {
    _skillType = skillType;
    _dimensions = new AspectLockedDimensions(
      TextureHelper.SkillIconRectangles[skillType],
      SkillIconSize,
      PrimaryDimension.Width
    );

    _vppExperienceCurve = new Lazy<int[]>(() =>
    {
      var apiManager = ModEntry.GetSingleton<ApiManager>();
      return !apiManager.GetApi(ModCompat.Vpp, out IVanillaPlusProfessions? vppApi)
        ? []
        : vppApi.LevelExperiences;
    });
  }

  public override int GetMaxLevel()
  {
    int[] vppCurve = _vppExperienceCurve.Value;
    return vppCurve.Length > 0 ? 20 : base.GetMaxLevel();
  }

  public override int GetUnmodifiedSkillLevel()
  {
    return Game1.player.GetUnmodifiedSkillLevel(_skillType);
  }

  public override int GetBaseExperienceForLevel(int level)
  {
    int[] vppXpCurve = _vppExperienceCurve.Value;
    int vppLevelIdx = level - 11;
    if (level > 10 && vppXpCurve.Length > vppLevelIdx)
    {
      return vppXpCurve[vppLevelIdx];
    }
    return level == 0 ? 0 : Farmer.getBaseExperienceForLevel(level);
  }

  public override int GetTotalExperience()
  {
    return Game1.player.experiencePoints[_skillType];
  }

  public override Texture2D GetTexture()
  {
    return Game1.mouseCursors;
  }

  public override Rectangle GetTextureRect()
  {
    return TextureHelper.SkillIconRectangles[_skillType];
  }

  public override Color GetColor()
  {
    return TextureHelper.SkillFillColors[_skillType];
  }

  public override float GetIconScale()
  {
    return _dimensions.ScaleFactor;
  }
}
