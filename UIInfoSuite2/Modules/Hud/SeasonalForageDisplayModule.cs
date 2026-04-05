using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Config;
using UIInfoSuite2.Managers;
using UIInfoSuite2.Models.Icons;
using UIInfoSuite2.Modules.Base;

namespace UIInfoSuite2.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class SeasonalForageDisplayModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconManager iconManager
) : HudIconModule(modEvents, logger, configManager, iconManager)
{
  private static readonly string IconKey = "ForageIcon";

  private readonly PerScreen<ForageIcon> _forageIcon = new(() => new ForageIcon());
  private readonly PerScreen<PotOfGoldIcon> _potOfGoldIcon = new(() => new PotOfGoldIcon());

  public override bool ShouldEnable()
  {
    return Config.ShowSeasonalForageIcon;
  }

  protected override void SetupIcons()
  {
    IconManager.AddIcon($"{IconKey}-Forage", _forageIcon.Value);
    IconManager.AddIcon($"{IconKey}-PotOfGold", _potOfGoldIcon.Value);
  }

  protected override void RemoveIcons()
  {
    RemoveIconsWhere(IconKey, 2);
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked;
  }

  public override void OnDisable()
  {
    base.OnDisable();
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked;
  }

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(10))
    {
      return;
    }

    _potOfGoldIcon.Value.Update();
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
    return I18n.Gmcm_Group_SeasonalForage();
  }

  public override void AddConfigOptions(
    IGenericModConfigMenuApi modConfigMenuApi,
    IManifest manifest
  )
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Forage_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Forage_Enable_Tooltip,
      getValue: () => Config.ShowSeasonalForageIcon,
      setValue: value => Config.ShowSeasonalForageIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Forage_Hazelnut_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Forage_Hazelnut_Enable_Tooltip,
      getValue: () => Config.ShowSeasonalBerryHazelnutIcon,
      setValue: value => Config.ShowSeasonalBerryHazelnutIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Forage_Beach_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Forage_Beach_Enable_Tooltip,
      getValue: () => Config.ShowSeasonalForageBeachIcon,
      setValue: value => Config.ShowSeasonalForageBeachIcon = value
    );
  }
  #endregion
}
