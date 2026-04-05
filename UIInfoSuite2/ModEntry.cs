// #define LAYOUT_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SimpleInjector;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Compatibility.CustomBush;
using UIInfoSuite2.Compatibility.SpaceCore;
using UIInfoSuite2.Config;
using UIInfoSuite2.Config.Configurable;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Helpers.GameStateHelpers;
using UIInfoSuite2.Interfaces;
using UIInfoSuite2.Layout.DebugMenu;
using UIInfoSuite2.Managers;
using UIInfoSuite2.Modules.Base;
using UIInfoSuite2.Modules.Hud;
using UIInfoSuite2.Modules.MenuAdditions;
using UIInfoSuite2.Modules.MenuAdditions.ExtendedItemInfo;
using UIInfoSuite2.Modules.MenuAdditions.MenuShortcuts;
using UIInfoSuite2.Modules.Overlay;
using UIInfoSuite2.Modules.Overlay.ObjectInfo;
using UIInfoSuite2.Patches;
using UIInfoSuite2.Patches.ExtensibleItemTooltips;

#if DEBUG
[assembly: MetadataUpdateHandler(typeof(HotReloadService))]

#endif

namespace UIInfoSuite2;

internal class ModEntry : Mod
{
  private static readonly Type[] BucketTypes =
  [
    typeof(BaseModule),
    typeof(HudIconModule),
    typeof(IPatchable),
    typeof(IConfigurable),
    typeof(IGameEventHolder),
  ];

  private readonly Container _container = new();

  public static ModEntry Instance { get; private set; } = null!;

  public static ModConfig Config => GetSingleton<ConfigManager>().Config;

  public static T GetSingleton<T>()
    where T : class
  {
    return Instance._container.GetInstance<T>();
  }

  public static Lazy<T> LazyGetSingleton<T>()
    where T : class
  {
    return new Lazy<T>(() => Instance._container.GetInstance<T>());
  }

  public static IEnumerable<BaseModule> GetAllModules()
  {
    return GetContainerCollection<BaseModule>();
  }

  public static IEnumerable<T> GetContainerCollection<T>()
    where T : class
  {
    return Instance._container.GetAllInstances<T>();
  }

  #region Entry
  public override void Entry(IModHelper helper)
  {
    Instance = this;
    I18n.Init(helper.Translation);
#if DEBUG
    Harmony.DEBUG = true;
#endif

    // Add Mod singletons to container
    _container.RegisterInstance(Helper);
    _container.RegisterInstance(ModManifest);
    _container.RegisterInstance(Monitor);
    _container.RegisterInstance(Helper.ConsoleCommands);
    _container.RegisterInstance(Helper.Data);
    _container.RegisterInstance(Helper.Events);
    _container.RegisterInstance(Helper.GameContent);
    _container.RegisterInstance(Helper.Input);
    _container.RegisterInstance(Helper.ModContent);
    _container.RegisterInstance(Helper.ModRegistry);
    _container.RegisterInstance(Helper.Reflection);
    _container.RegisterInstance(Helper.Translation);
    _container.RegisterInstance(new Harmony(Helper.ModContent.ModID));

    // Set up UI Info Suite Helpers
    Register<GameStateResolverCaches>();
    Register<GameStateHelper>();
    Register<BundleHelper>();
    Register<DropsHelper>();
    Register<SoundHelper>();
    Register<WorldHelper>();
    Register<SpaceCoreHelper>();
    Register<TvHelper>();

    // Set up Managers
    Register<ApiManager>();
    Register<EventsManager>();
    Register<ConfigManager>();
    Register<HudIconManager>();
    Register<FloatingTextManager>();

    // Set up empty registry sets
    foreach (Type bucketType in BucketTypes)
    {
      _container.Collection.Register(bucketType, Enumerable.Empty<Type>(), Lifestyle.Singleton);
    }

    // Register Modules
    Register<ConfigurableHudIconPositioning>();
    Register<ConfigurableDebugOptions>();
    Register<ExtensibleItemTooltips>();
    Register<PatchBushShakeItemEvent>();
    Register<PatchRenderingMenuContentStep>();
    Register<PatchMasteryXpGainEvent>();
    Register<MenuShortcutModule>();
    Register<ArtifactTrackerModule>();
    Register<BirthdayReminderModule>();
    Register<ConstructionTrackerModule>();
    Register<DailyLuckModule>();
    Register<DailyWeatherModule>();
    Register<SeasonalForageDisplayModule>();
    Register<WeeklyRecipeModule>();
    Register<ToolUpgradeReminderModule>();
    Register<MerchantReminderModule>();
    Register<ExtendedItemInfoModule>();
    Register<GiftLockModule>();
    Register<PartialHeartFillModule>();
    Register<ShopHarvestPriceModule>();
    Register<SocialPageFilterModule>();
    Register<AnimalInteractModule>();
    Register<ObjectContentsModule>();
    Register<ObjectEffectRangeModule>();
    Register<ObjectInfoModule>();
    Register<ExperienceModule>();
    Register<BuffTimerModule>();
    Register<QuestCountModule>();
    Register<FishSonarModule>();

    _container.Verify();

    foreach (IGameEventHolder eventHolder in GetContainerCollection<IGameEventHolder>())
    {
      eventHolder.RegisterEarlyEvents();
    }

    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    _container.GetInstance<EventsManager>().OnConfigChange += (_, _) => ReloadModules();
    _container.GetInstance<MenuShortcutModule>().Register(helper);

    var harmony = _container.GetInstance<Harmony>();
    foreach (IPatchable patchable in GetContainerCollection<IPatchable>())
    {
      patchable.Patch(harmony);
    }

    helper.ConsoleCommands.Add(
      "uiis2_layout_test",
      "Opens the layout system test menu.",
      (_, _) =>
      {
        if (!Context.IsWorldReady)
        {
          Monitor.Log("Load a save first.", LogLevel.Warn);
          return;
        }

        Game1.activeClickableMenu = new LayoutTestMenu();
      }
    );
  }
  #endregion


  private void ReloadModules()
  {
    if (Game1.gameMode == Game1.titleScreenGameMode)
    {
      return;
    }

    foreach (IGameEventHolder eventHolder in GetContainerCollection<IGameEventHolder>())
    {
      eventHolder.OnConfigChanged();
    }

    // Recalculate the icon rows if necessary
    _container.GetInstance<HudIconManager>().MarkRowsDirty();

    foreach (BaseModule module in GetAllModules())
    {
      switch (module.Enabled)
      {
        case false when module.ShouldEnable():
          module.Enable();
          break;
        case true when !module.ShouldEnable():
          module.Disable();
          break;
        default:
          module.OnConfigChange();
          break;
      }
    }
  }

  private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs eventArgs)
  {
    // Unload if the main player quits.
    if (Context.ScreenId != 0)
    {
      return;
    }

    foreach (BaseModule module in GetAllModules())
    {
      module.Disable();
    }

    _container.GetInstance<GameStateResolverCaches>().Clear();
    _container.GetInstance<HudIconManager>().UnregisterEvents();
    _container.GetInstance<FloatingTextManager>().UnregisterEvents();
  }

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    // Only load once for split screen.
    if (Context.ScreenId != 0)
    {
      return;
    }

    foreach (IGameEventHolder eventHolder in GetContainerCollection<IGameEventHolder>())
    {
      eventHolder.RegisterGameEvents();
    }

    foreach (BaseModule module in GetAllModules())
    {
      if (module.ShouldEnable())
      {
        module.Enable();
      }
    }
  }

  private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
  {
    var apiManager = _container.GetInstance<ApiManager>();
    apiManager.TryRegisterApi<ICustomBushApi>(Helper, ModCompat.CustomBush, "1.5.0", true);
    apiManager.TryRegisterApi<IBetterGameMenuApi>(Helper, ModCompat.BetterGameMenu, "1.0.1");
    apiManager.TryRegisterApi<ICloudySkiesApi>(Helper, ModCompat.CloudySkies, "1.9.0");
    apiManager.TryRegisterApi<ISpaceCoreApi>(Helper, ModCompat.SpaceCore, "1.28.4");
    apiManager.TryRegisterApi<IVanillaPlusProfessions>(Helper, ModCompat.Vpp, "1.1.0");
  }

  #region Module Setup
  private void Register<T>()
    where T : class
  {
    _container.RegisterSingleton<T>();

    foreach (Type bucketType in BucketTypes)
    {
      if (bucketType.IsAssignableFrom(typeof(T)))
      {
        _container.Collection.Append(bucketType, typeof(T));
      }
    }
  }
  #endregion

  #region Debug
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void DebugLog(string message, LogLevel level = LogLevel.Trace)
  {
#if DEBUG
    Instance.Monitor.Log(message, level);
#endif
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void LayoutDebug(string message, LogLevel level = LogLevel.Trace)
  {
#if LAYOUT_DEBUG
    Instance.Monitor.Log(message, level);
#endif
  }
  #endregion
}
