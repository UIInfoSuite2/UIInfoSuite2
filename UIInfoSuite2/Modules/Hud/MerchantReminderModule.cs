using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Objects;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Config;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Managers;
using UIInfoSuite2.Models.Icons;
using UIInfoSuite2.Modules.Base;

namespace UIInfoSuite2.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class MerchantReminderModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconManager iconManager,
  BundleHelper bundleHelper
) : HudIconModule(modEvents, logger, configManager, iconManager)
{
  protected const string IconPrefix = "MerchantIcon";

  // Lazy init because the icon init uses textures that aren't loaded yet
  private readonly Lazy<MerchantIcon> _booksellerIcon = new(() =>
    new MerchantIcon(MerchantIcon.Type.Bookseller)
  );
  private readonly Lazy<MerchantIcon> _travelerIcon = new(() =>
    new MerchantIcon(MerchantIcon.Type.Traveler)
  );

  public override bool ShouldEnable()
  {
    return Config.ShowMerchantIcons;
  }

  protected override void SetupIcons()
  {
    IconManager.AddIcon($"{IconPrefix}-Traveler", _travelerIcon.Value);
    IconManager.AddIcon($"{IconPrefix}-Bookseller", _booksellerIcon.Value);
  }

  protected override void RemoveIcons()
  {
    RemoveIconsWhere(IconPrefix, 2);
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.DayStarted += OnDayStarted;
    ModEvents.Display.MenuChanged += OnMenuChanged;
    UpdateIcons();
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.DayStarted -= OnDayStarted;
    ModEvents.Display.MenuChanged -= OnMenuChanged;
    base.OnDisable();
  }

  public override void OnConfigChange()
  {
    UpdateIcons();
  }

  private void OnDayStarted(object? sender, EventArgs e)
  {
    UpdateIcons();
  }

  private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
  {
    if (e.NewMenu is not ShopMenu menu)
    {
      return;
    }

    // TODO this can probably be replaced with a shopid check like below, was implemented before the shop data rework
    if (menu.forSale.Any(s => s is not Hat) && Game1.currentLocation.Name == "Forest")
    {
      _travelerIcon.Value.VisitedMerchant = true;
    }

    if (menu.ShopId == "Bookseller")
    {
      _booksellerIcon.Value.VisitedMerchant = true;
    }
  }

  private void UpdateIcons()
  {
    _travelerIcon.Value.UpdateMerchantIcon();
    _booksellerIcon.Value.UpdateMerchantIcon();
    CheckForBundleItems();
  }

  private void CheckForBundleItems()
  {
    try
    {
      Dictionary<ISalable, ItemStockInformation> stock = ShopBuilder.GetShopStock("Traveler");
      HashSet<string> bundles = [];
      foreach (ISalable salable in stock.Keys)
      {
        if (salable is not Item item)
        {
          continue;
        }

        bundles.AddRange(
          bundleHelper
            .BundlesRequiringItem(item)
            .Select(data => $"{item.DisplayName} ({data.Bundle.DisplayName})")
        );
      }

      List<string> bundleList = bundles.ToList();
      if (Config.MerchantAlwaysHasBundleItem)
      {
        bundleList.Add("Debug Leek (Spring Foraging)");
      }
      bundleList.Sort();
      _travelerIcon.Value.SetBundleItems(bundleList);
    }
    catch (Exception e)
    {
      Logger.Log(
        $"MerchantReminderModule: merchant stock check failed, {e.Message}",
        LogLevel.Warn
      );
    }
  }

  #region Configuration Setup
  public override string GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public override string GetConfigSection()
  {
    return ConfigSectionNames.NotificationIcons;
  }

  public override string GetSubHeader()
  {
    return I18n.Gmcm_Group_Merchant();
  }

  public override void AddConfigOptions(
    IGenericModConfigMenuApi modConfigMenuApi,
    IManifest manifest
  )
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_Enable_Tooltip,
      getValue: () => Config.ShowMerchantIcons,
      setValue: value => Config.ShowMerchantIcons = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_Traveler_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_Traveler_Enable_Tooltip,
      getValue: () => Config.ShowTravelingMerchantIcon,
      setValue: value => Config.ShowTravelingMerchantIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_Bookseller_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_Bookseller_Enable_Tooltip,
      getValue: () => Config.ShowBooksellerIcon,
      setValue: value => Config.ShowBooksellerIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_HideOnVisit,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_HideOnVisit_Tooltip,
      getValue: () => Config.HideMerchantIconWhenVisited,
      setValue: value => Config.HideMerchantIconWhenVisited = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_ShowBundleIcon,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_ShowBundleIcon_Tooltip,
      getValue: () => Config.ShowMerchantBundleIcon,
      setValue: value => Config.ShowMerchantBundleIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_ShowBundleItems,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_ShowBundleItems_Tooltip,
      getValue: () => Config.ShowMerchantBundleItems,
      setValue: value => Config.ShowMerchantBundleItems = value
    );
  }
  #endregion
}
