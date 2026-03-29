using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Mods;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Config;
using UIInfoSuite2.Extensions;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Interfaces;
using UIInfoSuite2.Models;
using UIInfoSuite2.Models.Enums;
using UIInfoSuite2.Modules.Base;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Modules.Overlay;

using SObject = Object;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class ObjectContentsModule : BaseModule, IConfigurable
{
  // Custom icon offsets from the machine's sprite center
  private static readonly Dictionary<string, Vector2> CustomOffsets = new()
  {
    ["Cask"] = new Vector2(0f, -20f),
  };

  private readonly KeybindVisibility _iconVisibility;
  private readonly WorldHelper _worldHelper;
  private readonly PerScreen<List<MachineIconData>> _visibleMachines = new(() => []);
  private readonly PerScreen<List<FishPondIconData>> _visibleFishPonds = new(() => []);

  public ObjectContentsModule(
    IModEvents modEvents,
    IMonitor logger,
    ConfigManager configManager,
    WorldHelper worldHelper
  )
    : base(modEvents, logger, configManager)
  {
    _iconVisibility = new KeybindVisibility(
      () => Config.ObjectContentsKeybind,
      () => Config.ObjectContentsVisibility
    );
    _worldHelper = worldHelper;
  }

  public override bool ShouldEnable()
  {
    return Config.ObjectContentsVisibility != VisibilityMode.Off;
  }

  public override void OnEnable()
  {
    ModEvents.Input.ButtonsChanged += _iconVisibility.OnButtonsChanged;
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked;
    ModEvents.Display.RenderedStep += OnRenderStep;
  }

  public override void OnDisable()
  {
    ModEvents.Input.ButtonsChanged -= _iconVisibility.OnButtonsChanged;
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked;
    ModEvents.Display.RenderedStep -= OnRenderStep;
  }

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(4))
    {
      return;
    }

    _visibleMachines.Value.Clear();
    _visibleFishPonds.Value.Clear();

    if (!_iconVisibility.IsVisible)
    {
      return;
    }

    foreach (SObject obj in _worldHelper.GetObjectsInViewport())
    {
      if (!MachineHelper.IsTrackableMachine(obj) || !obj.IsWorking())
      {
        continue;
      }

      ParsedItemData? itemData = MachineHelper.GetItemBeingProcessed(obj);
      ParsedItemData? machineData = ItemRegistry.GetData(obj.QualifiedItemId);

      if (itemData == null || machineData == null)
      {
        continue;
      }

      int machineSpriteHeight = machineData.GetSourceRect().Height;
      CustomOffsets.TryGetValue(obj.Name, out Vector2 offset);
      _visibleMachines.Value.Add(
        new MachineIconData(obj.TileLocation, itemData, machineSpriteHeight, offset)
      );
    }

    if (!Config.ShowFishPondResidents)
    {
      return;
    }

    foreach (FishPond fishPond in _worldHelper.GetBuildingsInViewport().OfType<FishPond>())
    {
      if (fishPond.fishType.Value == null || fishPond.currentOccupants.Value <= 0)
      {
        continue;
      }

      ParsedItemData? fishData = ItemRegistry.GetData("(O)" + fishPond.fishType.Value);
      if (fishData == null)
      {
        continue;
      }

      _visibleFishPonds.Value.Add(new FishPondIconData(fishPond.GetCenterTile(), fishData));
    }
  }

  private void OnRenderStep(object? sender, RenderedStepEventArgs e)
  {
    if (
      e.Step != RenderSteps.World_AlwaysFront
      || !UIElementUtils.IsRenderingNormally()
      || Game1.activeClickableMenu != null
    )
    {
      return;
    }

    List<MachineIconData> machines = _visibleMachines.Value;
    List<FishPondIconData> fishPonds = _visibleFishPonds.Value;
    if (machines.Count == 0 && fishPonds.Count == 0)
    {
      return;
    }

    SpriteBatch spriteBatch = e.SpriteBatch;
    // Fish pond icons: draw fish species at pond center
    foreach (FishPondIconData pond in fishPonds)
    {
      Vector2 screenPos = Game1.GlobalToLocal(
        new Vector2(pond.CenterTile.X * Game1.tileSize, pond.CenterTile.Y * Game1.tileSize)
      );

      Texture2D texture = pond.FishData.GetTexture();
      Rectangle sourceRect = pond.FishData.GetSourceRect();

      // Center the icon on the tile
      float iconSize = sourceRect.Width * Game1.pixelZoom;
      var iconPos = new Vector2(
        screenPos.X + Game1.tileSize / 2f - iconSize / 2f,
        screenPos.Y + Game1.tileSize / 2f - iconSize / 2f
      );

      TextureHelper.DrawOutlinedSprite(
        spriteBatch,
        texture,
        iconPos,
        sourceRect,
        scale: Game1.pixelZoom,
        outlineSize: 2f
      );
    }

    // Machine icons: draw processing item on each machine
    foreach (MachineIconData machine in machines)
    {
      Vector2 screenPos = Game1.GlobalToLocal(
        new Vector2(machine.Tile.X * Game1.tileSize, machine.Tile.Y * Game1.tileSize)
      );

      // Center icon on the machine sprite.
      // Machine renders from (tileY + tileSize - spriteHeight) to (tileY + tileSize).
      int spriteHeight = machine.MachineSpriteHeight * Game1.pixelZoom;
      float machineCenterX = screenPos.X + Game1.tileSize / 2f;
      float machineCenterY = screenPos.Y + Game1.tileSize - spriteHeight / 2f;
      var iconPos = new Vector2(
        machineCenterX - 16f + machine.Offset.X,
        machineCenterY - 16f + machine.Offset.Y
      );

      Texture2D texture = machine.ItemData.GetTexture();
      Rectangle sourceRect = machine.ItemData.GetSourceRect();

      // Outline: 2px larger black silhouette centered behind the icon
      TextureHelper.DrawOutlinedSprite(
        spriteBatch,
        texture,
        iconPos,
        sourceRect,
        scale: 2f,
        outlineSize: 2f
      );
    }
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
    return I18n.Gmcm_Group_ObjectContentsIcons();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddTextOption(
      manifest,
      name: I18n.Gmcm_Modules_Tooltips_Machines_ObjectContents_Mode,
      tooltip: I18n.Gmcm_Modules_Tooltips_Machines_ObjectContents_Mode_Tooltip,
      getValue: () => Config.ObjectContentsVisibility.ToModConfigString(),
      setValue: value =>
        Config.ObjectContentsVisibility = VisibilityModeExtensions.FromModConfigString(value),
      allowedValues: VisibilityModeExtensions.StringToMode.Keys.ToArray()
    );
    modConfigMenuApi.AddKeybindList(
      manifest,
      name: I18n.Gmcm_Modules_Tooltips_Machines_ObjectContents_Keybind,
      tooltip: I18n.Gmcm_Modules_Tooltips_Machines_ObjectContents_Keybind_Tooltip,
      getValue: () => Config.ObjectContentsKeybind,
      setValue: value => Config.ObjectContentsKeybind = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Tooltips_Machines_ObjectContents_FishPonds,
      tooltip: I18n.Gmcm_Modules_Tooltips_Machines_ObjectContents_FishPonds_Tooltip,
      getValue: () => Config.ShowFishPondResidents,
      setValue: value => Config.ShowFishPondResidents = value
    );
  }
  #endregion

  #region Private record types
  private readonly record struct MachineIconData(
    Vector2 Tile,
    ParsedItemData ItemData,
    int MachineSpriteHeight,
    Vector2 Offset
  );

  private readonly record struct FishPondIconData(Vector2 CenterTile, ParsedItemData FishData);
  #endregion
}
