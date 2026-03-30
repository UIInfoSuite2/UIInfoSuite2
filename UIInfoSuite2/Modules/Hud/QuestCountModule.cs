using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Config;
using UIInfoSuite2.Interfaces;
using UIInfoSuite2.Modules.Base;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
public class QuestCountModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager)
  : BaseModule(modEvents, logger, configManager),
    IConfigurable
{
  private const float _digitScale = 3f;
  private const int _scaledHeight = (int)(7f * _digitScale); // tinyDigits are 5x7px

  public override bool ShouldEnable()
  {
    return Config.ShowQuestCount;
  }

  public override void OnEnable()
  {
    ModEvents.Display.RenderingHud += OnRenderingHud;
  }

  public override void OnDisable()
  {
    ModEvents.Display.RenderingHud -= OnRenderingHud;
  }

  #region Event subscriptions
  private static int GetVisibleQuestCount()
  {
    return Game1.player.questLog.Count(q => q != null && !q.IsHidden())
      + Game1.player.team.specialOrders.Count(so => !so.IsHidden());
  }

  private static void GetPositionAndSize(
    Rectangle bounds,
    int questCount,
    out float centerX,
    out float y,
    out int bgWidth,
    out int bgHeight
  )
  {
    int scaledWidth = Utility.getWidthOfTinyDigitString(questCount, _digitScale);

    centerX = bounds.X + bounds.Width / 2f;
    y = bounds.Y + bounds.Height + 20;

    int padding = 6;
    bgWidth = scaledWidth + padding * 2 + 3;
    bgHeight = _scaledHeight + padding * 2;
  }

  // Draw background and number BEFORE HUD so journal icon renders on top
  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally() || !Game1.player.hasVisibleQuests)
    {
      return;
    }

    int questCount = GetVisibleQuestCount();
    if (questCount <= 0)
    {
      return;
    }

    Rectangle bounds = Game1.dayTimeMoneyBox.questButton.bounds;
    GetPositionAndSize(
      bounds,
      questCount,
      out float centerX,
      out float y,
      out int bgWidth,
      out int bgHeight
    );

    // Draw background
    var bgSource = new Rectangle(432, 439, 9, 9);
    var bgDest = new Rectangle(
      (int)(centerX - bgWidth / 2f),
      (int)(y - bgHeight / 2f) + 3,
      bgWidth,
      bgHeight
    );
    Game1.spriteBatch.Draw(Game1.mouseCursors, bgDest, bgSource, Color.White);

    // Draw number centered on background
    int digitStringWidth = Utility.getWidthOfTinyDigitString(questCount, _digitScale);
    float numberX = centerX - digitStringWidth / 2f;
    float numberY = y - 8;

    Utility.drawTinyDigits(
      questCount,
      Game1.spriteBatch,
      new Vector2(numberX, numberY),
      _digitScale,
      0.99f,
      Color.White
    );
  }
  #endregion
  #region Configuration Setup
  public string GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public string GetConfigSection()
  {
    return ConfigSectionNames.NotificationIcons;
  }

  public string GetSubHeader()
  {
    return I18n.Gmcm_Group_QuestCount();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_JournalQuestCount_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_JournalQuestCount_Enable_Tooltip,
      getValue: () => Config.ShowQuestCount,
      setValue: value => Config.ShowQuestCount = value
    );
  }
  #endregion
}
