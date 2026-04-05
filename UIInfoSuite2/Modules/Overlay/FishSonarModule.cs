using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Config;
using UIInfoSuite2.Interfaces;
using UIInfoSuite2.Modules.Base;

namespace UIInfoSuite2.Modules.Overlay;

/// <summary>
/// Module to show fish quality stars in the fishing minigame.
/// </summary>
/// <author>DazUki</author>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patch names")]
[SuppressMessage(
  "ReSharper",
  "UnusedType.Global",
  Justification = "Instantiated by SimpleInjector"
)]
public class FishSonarModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager)
  : BaseModule(modEvents, logger, configManager),
    IPatchable,
    IConfigurable
{
  private static FishSonarModule Module => ModEntry.GetSingleton<FishSonarModule>();

  public override bool ShouldEnable()
  {
    return Config.ShowFishSonar;
  }

  public override void OnEnable() { }

  public override void OnDisable() { }

  public void Patch(Harmony harmony)
  {
    harmony.Patch(
      original: AccessTools.Method(
        typeof(BobberBar),
        nameof(BobberBar.draw),
        [typeof(SpriteBatch)]
      ),
      prefix: new HarmonyMethod(typeof(FishSonarModule), nameof(BeforeDraw)),
      postfix: new HarmonyMethod(typeof(FishSonarModule), nameof(AfterDraw))
    );
  }

  // Temporarily add SonarBobber to the bobbers list so the game's own
  // rendering code draws the fish identity inside the correct coordinate space
  private static void BeforeDraw(List<string> ___bobbers)
  {
    if (Module.Enabled && !___bobbers.Contains("(O)SonarBobber"))
    {
      ___bobbers.Add("(O)SonarBobber");
    }
  }

  // Remove the injected SonarBobber after drawing, then draw quality star
  private static void AfterDraw(
    BobberBar __instance,
    SpriteBatch b,
    List<string> ___bobbers,
    int ___fishQuality,
    Vector2 ___everythingShake
  )
  {
    if (!Module.Enabled)
    {
      return;
    }

    ___bobbers.Remove("(O)SonarBobber");

    // Only show star when enabled, minigame is fully visible, and actively playing
    if (!Module.Config.ShowFishQuality || __instance.scale < 1f || __instance.fadeOut)
    {
      return;
    }

    // Calculate effective quality: perfect catch boosts +1 if at least silver
    int quality = ___fishQuality;
    if (__instance.perfect && quality >= 1)
    {
      quality++;
    }
    // Quality 3 doesn't exist in Stardew — jumps to 4 (iridium)
    if (quality == 3)
    {
      quality = 4;
    }

    if (quality <= 0)
    {
      return;
    }

    // Re-enter world draw coordinate space to match the fish icon position
    Game1.StartWorldDrawInUI(b);

    int xPos = __instance.xPositionOnScreen;
    int yPos = __instance.yPositionOnScreen;

    // Mirrors BobberBar.draw() fish icon layout - position flips when bar is in right 25% of screen
    int iconX = (xPos > Game1.viewport.Width * 0.75f) ? (xPos - 80) : (xPos + 216);
    bool flipped = iconX < xPos;

    // Fish icon is drawn at this position by the game
    Vector2 fishIconPos =
      new Vector2(iconX, yPos) + new Vector2(flipped ? -8 : -4, 4f) * 4f + ___everythingShake;

    // Quality star sprite from Game1.mouseCursors
    Rectangle starRect =
      quality < 4
        ? new Rectangle(338 + (quality - 1) * 8, 400, 8, 8)
        : new Rectangle(346, 392, 8, 8);

    // Iridium star pulsing effect
    float pulseScale =
      quality < 4
        ? 0f
        : ((float)Math.Cos(Game1.currentGameTime.TotalGameTime.Milliseconds * Math.PI / 512.0) + 1f)
          * 0.05f;

    // Draw quality star at bottom-left of the fish icon (matching inventory style)
    b.Draw(
      Game1.mouseCursors,
      fishIconPos + new Vector2(12f, 52f + pulseScale),
      starRect,
      Color.White,
      0f,
      new Vector2(4f, 4f),
      3f * (1f + pulseScale),
      SpriteEffects.None,
      0.89f
    );

    Game1.EndWorldDrawInUI(b);
  }

  #region Configuration Setup
  public string GetConfigPage()
  {
    return ConfigPageNames.Tooltips;
  }

  public string GetConfigSection()
  {
    return ConfigSectionNames.EmptySection;
  }

  public string GetSubHeader()
  {
    return I18n.Gmcm_Group_FishingSonar();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Tooltips_FishSonar_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_FishSonar_Enable_Tooltip,
      getValue: () => Config.ShowFishSonar,
      setValue: value => Config.ShowFishSonar = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Tooltips_FishSonar_ShowQuality,
      tooltip: I18n.Gmcm_Modules_Tooltips_FishSonar_ShowQuality_Tooltip,
      getValue: () => Config.ShowFishQuality,
      setValue: value => Config.ShowFishQuality = value
    );
  }
  #endregion
}
