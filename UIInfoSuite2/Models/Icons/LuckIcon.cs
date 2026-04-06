using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Models.Enums;

namespace UIInfoSuite2.Models.Icons;

internal class LuckIcon : ClickableIcon
{
  private const int CloverFrameSize = 26;
  private const int TvFrameSize = 20;
  private static readonly Color Luck1Color = new(87, 255, 106, 255);
  private static readonly Color Luck2Color = new(148, 255, 210, 255);
  private static readonly Color Luck3Color = new(246, 255, 145, 255);
  private static readonly Color Luck4Color = new(255, 255, 255, 255);
  private static readonly Color Luck5Color = new(255, 155, 155, 255);
  private static readonly Color Luck6Color = new(165, 165, 165, 204);
  private static readonly Rectangle DiceSourceBounds = new(50, 428, 10, 10);

  private static readonly Lazy<LuckInfo[]> LuckInfoTable = new(() =>
    [
      new LuckInfo(0, 0, Luck6Color, I18n.LuckStatus6()),
      new LuckInfo(1, 1, Luck6Color, I18n.LuckStatus6()),
      new LuckInfo(2, 2, Luck5Color, I18n.LuckStatus5()),
      new LuckInfo(3, 3, Luck4Color, I18n.LuckStatus4()),
      new LuckInfo(4, 3, Luck3Color, I18n.LuckStatus3()),
      new LuckInfo(5, 4, Luck2Color, I18n.LuckStatus2()),
      new LuckInfo(6, 5, Luck1Color, I18n.LuckStatus1()),
      new LuckInfo(7, 6, Luck1Color, I18n.LuckStatus1()),
    ]
  );

  private static readonly Lazy<Texture2D> tvTexture = TextureHelper.LazyLoadModTexture(
    "assets",
    "tv_group.png"
  );

  private static readonly Lazy<Texture2D> cloverTexture = TextureHelper.LazyLoadModTexture(
    "assets",
    "clover_group.png"
  );

  private LuckInfo _luckInfo;

  public LuckIcon()
    : base(Game1.mouseCursors, DiceSourceBounds, 40)
  {
    _luckInfo = GetLuckInfo();
    SetType(Config.LuckIconType);
  }

  public LuckIconType LuckIconType { get; private set; } = LuckIconType.Clover;

  public int LuckLevel => _luckInfo.LuckLevel;

  private static LuckInfo GetLuckInfo()
  {
    // Author: DazuKi
    // Shrine extremes use sharedDailyLuck (base value before Special Charm);
    // all other tiers use DailyLuck (includes Special Charm) with the same
    // thresholds the TV fortune teller uses: -0.07, -0.02, +0.02, +0.07
    double luck = Game1.player.LuckLevel;
    double sharedLuck = Game1.player.team.sharedDailyLuck.Value;
    LuckInfo[] luckInfoList = LuckInfoTable.Value;
    if (sharedLuck <= -0.12)
    {
      // Shrine extreme bad
      return luckInfoList[0];
    }

    switch (luck)
    {
      case < -0.07:
        // Very bad luck
        return luckInfoList[1];
      case < -0.02:
        // Bad luck
        return luckInfoList[2];
      case 0:
        // Absolutely neutral
        return luckInfoList[3];
      case <= 0.02:
        // Near-neutral (non-zero, between -0.02 and +0.02)
        return luckInfoList[4];
      case <= 0.07:
        // Good luck
        return luckInfoList[5];
    }

    return sharedLuck >= 0.12
      ? luckInfoList[7] // Shrine extreme good
      : luckInfoList[6]; // Very good luck
  }

  public void Update(bool force = false)
  {
    LuckInfo luckInfo = GetLuckInfo();
    int lastLuckLevel = LuckLevel;
    _luckInfo = luckInfo;
    if (lastLuckLevel == LuckLevel && !force)
    {
      return;
    }

    // Update style
    Color = luckInfo.Color;
    HoverText = Config.ShowExactLuckValue
      ? string.Format(I18n.DailyLuckValue(), Game1.player.DailyLuck.ToString("N3"))
      : luckInfo.HoverText;
    UpdateIconStyle();
  }

  private void UpdateIconStyle()
  {
    Rectangle newBounds;
    switch (LuckIconType)
    {
      case LuckIconType.Classic:
        newBounds = DiceSourceBounds;
        break;
      case LuckIconType.Clover:
        newBounds = new Rectangle(LuckLevel * CloverFrameSize, 0, CloverFrameSize, CloverFrameSize);
        break;
      case LuckIconType.Tv:
        newBounds = new Rectangle(_luckInfo.TvIndex * TvFrameSize, 0, TvFrameSize, TvFrameSize);
        break;
      default:
        SetType(LuckIconType.Clover);
        return;
    }

    // Set the new bounds and regenerate the icon component automatically
    SetSourceBounds(newBounds);
  }

  public void SetType(LuckIconType type)
  {
    LuckIconType = type;
    switch (type)
    {
      case LuckIconType.Classic:
        BaseTexture = Game1.mouseCursors;
        break;
      case LuckIconType.Tv:
        BaseTexture = tvTexture.Value;
        break;
      default:
      case LuckIconType.Clover:
        BaseTexture = cloverTexture.Value;
        break;
    }

    Update(true);
  }

  protected override bool _ShouldDraw()
  {
    if (Config.RequireTvForLuckIcon && !TvHelper.HasWatchedFortune.Value)
    {
      return false;
    }
    return base._ShouldDraw();
  }

  #region Nested type: LuckInfo
  internal readonly record struct LuckInfo(
    int LuckLevel,
    int TvIndex,
    Color Color,
    string HoverText
  );
  #endregion
}
