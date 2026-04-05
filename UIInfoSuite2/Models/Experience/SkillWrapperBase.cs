using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Compatibility.SpaceCore;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Layout.Measurement;
using UIInfoSuite2.Utilities;

namespace UIInfoSuite2.Models.Experience;

internal record XpThreshold(int TotalXp, int LevelXp, int NextLevelXp);

internal abstract class SkillWrapperBase
{
  protected const int SkillIconSize = 30;

  private static readonly XpThreshold _emptyThreshold = new(0, 0, 0);
  private XpThreshold _xpThreshold = new(1, 0, 0);
  private XpThreshold _lastXpThreshold = _emptyThreshold;

  // Tracks levelup across update (lastLevel, curLevel)
  private (int, int) _levelData = (0, 0);

  public bool UpdateExperience()
  {
    int totalSkillXp = GetTotalExperience();
    if (totalSkillXp == _xpThreshold.TotalXp && _xpThreshold.Equals(_lastXpThreshold))
    {
      return false;
    }

    int skillLevel = GetUnmodifiedSkillLevel();
    int xpToGetToLevel = GetBaseExperienceForLevel(skillLevel);
    if (IsMaxLevel || xpToGetToLevel == -1)
    {
      _xpThreshold = _emptyThreshold;
      return false;
    }

    int xpThisLevel = totalSkillXp - xpToGetToLevel;
    int xpForNextLevel = GetBaseExperienceForLevel(skillLevel + 1) - xpToGetToLevel;

    _levelData = (_levelData.Item2, skillLevel);
    _lastXpThreshold = _xpThreshold;
    _xpThreshold = new XpThreshold(totalSkillXp, xpThisLevel, xpForNextLevel);
    return true;
  }

  public bool HasGainedXp => _xpThreshold.TotalXp > _lastXpThreshold.TotalXp;
  public bool HasLevelledUp => _levelData.Item1 != _levelData.Item2;

  public int ExpSinceLastUpdate => _xpThreshold.TotalXp - _lastXpThreshold.TotalXp;

  public bool IsMaxLevel => GetUnmodifiedSkillLevel() >= GetMaxLevel();

  public int GetLiveXpGained()
  {
    return GetTotalExperience() - _xpThreshold.TotalXp;
  }

  public XpThreshold GetThresholdData()
  {
    return _xpThreshold;
  }

  public virtual int GetMaxLevel()
  {
    return 10;
  }

  public abstract int GetUnmodifiedSkillLevel();
  public abstract int GetBaseExperienceForLevel(int level);
  public abstract int GetTotalExperience();

  public abstract Texture2D GetTexture();
  public abstract Rectangle GetTextureRect();
  public abstract Color GetColor();

  public abstract float GetIconScale();
}
