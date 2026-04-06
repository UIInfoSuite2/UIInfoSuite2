using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Config;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Managers;
using UIInfoSuite2.Models.Enums;
using UIInfoSuite2.Models.Icons;
using UIInfoSuite2.Modules.Base;

namespace UIInfoSuite2.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class DailyLuckModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconManager iconManager
) : SingleHudIconModule<LuckIcon>(modEvents, logger, configManager, iconManager)
{
  protected override string IconKey => "Luck";

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(30))
    {
      return;
    }

    Icon.Update();
  }

  protected override LuckIcon GenerateNewIcon()
  {
    return new LuckIcon();
  }

  public override bool ShouldEnable()
  {
    return Config.ShowLuckIcon;
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked;
    Icon.Update(true);
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked;
    base.OnDisable();
  }

  public override void OnConfigChange()
  {
    base.OnConfigChange();
    Icon.SetType(Config.LuckIconType);
  }

  #region Configuration Setup
  public override string GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public override string GetConfigSection()
  {
    return ConfigSectionNames.StatusIcons;
  }

  public override string GetSubHeader()
  {
    return I18n.Gmcm_Group_Luck();
  }

  public override void AddConfigOptions(
    IGenericModConfigMenuApi modConfigMenuApi,
    IManifest manifest
  )
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Luck_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Luck_Enable_Tooltip,
      getValue: () => Config.ShowLuckIcon,
      setValue: value => Config.ShowLuckIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Luck_Exact,
      tooltip: I18n.Gmcm_Modules_Icons_Luck_Exact_Tooltip,
      getValue: () => Config.ShowExactLuckValue,
      setValue: value => Config.ShowExactLuckValue = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Tv_RequireWatched,
      tooltip: I18n.Gmcm_Modules_Icons_Tv_RequireWatched_Tooltip,
      getValue: () => Config.RequireTvForLuckIcon,
      setValue: value => Config.RequireTvForLuckIcon = value
    );
    modConfigMenuApi.AddTextOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Luck_IconType,
      tooltip: I18n.Gmcm_Modules_Icons_Luck_IconType_Tooltip,
      getValue: () => Config.LuckIconType.ToModConfigString(),
      setValue: value => Config.LuckIconType = LuckIconTypeExtensions.FromModConfigString(value),
      allowedValues: LuckIconTypeExtensions.StringToType.Keys.ToArray()
    );
  }
  #endregion
}
