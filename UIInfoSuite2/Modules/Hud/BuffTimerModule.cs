using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Config;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Interfaces;
using UIInfoSuite2.Modules.Base;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class BuffTimerModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  IReflectionHelper reflectionHelper,
  SoundHelper soundHelper
) : BaseModule(modEvents, logger, configManager), IConfigurable
{
  private const float DigitScale = 2f;

  private static readonly Color ShadowColor = Color.Black * 0.35f;
  private static readonly Color DigitColor = Color.White * 0.8f;
  private static readonly Color DotColor = Color.White * 0.8f;
  private static readonly Color FadeColor = new(255, 75, 75, 255);
  private static readonly Color FadingDigitColor = FadeColor * 0.9f;
  private static readonly Color FadingDotColor = FadeColor * 0.9f;

  private readonly PerScreen<HashSet<string>> _previousBuffIds = new(() => []);

  public override bool ShouldEnable()
  {
    return Config.ShowBuffTimers || Config.PlayBuffExpireSound;
  }

  public override void OnEnable()
  {
    if (Config.ShowBuffTimers)
    {
      ModEvents.Display.RenderedHud += OnRenderedHud;
    }

    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked;
  }

  public override void OnDisable()
  {
    ModEvents.Display.RenderedHud -= OnRenderedHud;
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked;
  }

  public override void OnConfigChange()
  {
    ModEvents.Display.RenderedHud -= OnRenderedHud;
    if (Config.ShowBuffTimers)
    {
      ModEvents.Display.RenderedHud += OnRenderedHud;
    }
  }

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!Context.IsWorldReady)
    {
      _previousBuffIds.Value.Clear();
      return;
    }

    HashSet<string> currentBuffIds = [];
    foreach (KeyValuePair<string, Buff> pair in Game1.player.buffs.AppliedBuffs)
    {
      if (pair.Value.millisecondsDuration != -2)
      {
        currentBuffIds.Add(pair.Key);
      }
    }

    if (Config.PlayBuffExpireSound && _previousBuffIds.Value.Count > 0)
    {
      foreach (string id in _previousBuffIds.Value)
      {
        if (!currentBuffIds.Contains(id))
        {
          soundHelper.Play(Sounds.BuffExpired);
          break;
        }
      }
    }

    _previousBuffIds.Value.Clear();
    foreach (string id in currentBuffIds)
    {
      _previousBuffIds.Value.Add(id);
    }
  }

  private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally())
    {
      return;
    }

    Dictionary<ClickableTextureComponent, Buff>? buffs = GetBuffComponents();
    if (buffs == null || buffs.Count == 0)
    {
      return;
    }

    SpriteBatch b = e.SpriteBatch;

    foreach (KeyValuePair<ClickableTextureComponent, Buff> pair in buffs)
    {
      Buff buff = pair.Value;

      if (buff.millisecondsDuration == -2)
      {
        continue;
      }

      ClickableTextureComponent icon = pair.Key;
      int totalSeconds = Math.Max(0, buff.millisecondsDuration / 1000);
      int minutes = totalSeconds / 60;
      int seconds = totalSeconds % 60;

      int totalWidth =
        TinyDigitHelper.MeasureNumber(minutes, DigitScale)
        + TinyDigitHelper.MeasureColon()
        + TinyDigitHelper.MeasureNumber(seconds < 10 ? 10 : seconds, DigitScale);

      float x = icon.bounds.X + icon.bounds.Width / 2f - totalWidth / 2f;
      float y = icon.bounds.Y + icon.bounds.Height + 2;

      float alpha =
        buff.displayAlphaTimer > 0f
          ? (float)(Math.Cos(buff.displayAlphaTimer / 100f) + 3.0) / 4f
          : 1f;

      bool isFading = buff.displayAlphaTimer > 0f;

      DrawTimer(b, minutes, seconds, new Vector2(x, y), alpha, isFading);
    }
  }

  private static void DrawTimer(
    SpriteBatch b,
    int minutes,
    int seconds,
    Vector2 position,
    float alpha,
    bool isFading
  )
  {
    Color digitColor = (isFading ? FadingDigitColor : DigitColor) * alpha;
    Color dotColor = (isFading ? FadingDotColor : DotColor) * alpha;
    Color shadowColor = ShadowColor * alpha;

    float xOffset = TinyDigitHelper.DrawNumber(
      b,
      minutes,
      position,
      DigitScale,
      digitColor,
      shadowColor
    );
    xOffset += TinyDigitHelper.DrawColon(b, position, xOffset, DigitScale, dotColor, shadowColor);
    TinyDigitHelper.DrawNumber(
      b,
      seconds / 10,
      position + new Vector2(xOffset, 0),
      DigitScale,
      digitColor,
      shadowColor
    );
    xOffset += TinyDigitHelper.MeasureNumber(seconds / 10, DigitScale);
    TinyDigitHelper.DrawNumber(
      b,
      seconds % 10,
      position + new Vector2(xOffset, 0),
      DigitScale,
      digitColor,
      shadowColor
    );
  }

  private Dictionary<ClickableTextureComponent, Buff>? GetBuffComponents()
  {
    try
    {
      return reflectionHelper
        .GetField<Dictionary<ClickableTextureComponent, Buff>>(Game1.buffsDisplay, "buffs")
        .GetValue();
    }
    catch
    {
      return null;
    }
  }

  #region Configuration Setup
  public string GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public string GetConfigSection()
  {
    return ConfigSectionNames.HudGlobal;
  }

  public string GetSubHeader()
  {
    return I18n.Gmcm_Group_BuffTimers();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Hud_BuffTimers_Enable,
      tooltip: I18n.Gmcm_Modules_Hud_BuffTimers_Enable_Tooltip,
      getValue: () => Config.ShowBuffTimers,
      setValue: value => Config.ShowBuffTimers = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Hud_BuffTimers_ExpireSound,
      tooltip: I18n.Gmcm_Modules_Hud_BuffTimers_ExpireSound_Tooltip,
      getValue: () => Config.PlayBuffExpireSound,
      setValue: value => Config.PlayBuffExpireSound = value
    );
  }
  #endregion
}
