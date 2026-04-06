using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Compatibility.SpaceCore;
using UIInfoSuite2.Config;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Interfaces;
using UIInfoSuite2.Managers;
using UIInfoSuite2.Models.Events;
using UIInfoSuite2.Models.Experience;
using UIInfoSuite2.Modules.Base;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class ExperienceModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  SoundHelper soundHelper,
  FloatingTextManager floatingTextManager,
  EventsManager eventsManager,
  ApiManager apiManager,
  SpaceCoreHelper spaceCoreHelper
) : BaseModule(modEvents, logger, configManager), IConfigurable
{
  private const string FloatingTextKey = "ExperienceGain";
  private const int XpVisibleTicks = 100;
  private readonly PerScreen<SkillWrapperBase> MasterySkill = new(() => new MasterySkillWrapper());

  private readonly PerScreen<ExperienceBarModel> _displayedExperienceBar = new(() =>
    new ExperienceBarModel()
  );

  private readonly PerScreen<Dictionary<int, VanillaSkillWrapper>> _baseSkills = new(() =>
    new Dictionary<int, VanillaSkillWrapper>()
  );
  private readonly PerScreen<Dictionary<string, SpaceCoreSkillWrapper>> _spaceCoreSkills = new(() =>
    new Dictionary<string, SpaceCoreSkillWrapper>()
  );

  private readonly PerScreen<Item?> _previousItem = new();

  public override bool ShouldEnable()
  {
    return Config.ShowExperienceBar || Config.ShowExperienceGain || Config.ShowLevelUpAnimation;
  }

  public override void OnEnable()
  {
    ModEvents.Display.RenderingHud += OnRenderingHud;
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked_HandleTimers;
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked_CheckTools;
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked_CheckSkillLevels;
    ModEvents.Player.LevelChanged += OnLevelChanged;
    eventsManager.OnMasteryXpGain += OnMasteryXpChange;

    _displayedExperienceBar.Value.UpdatePosition();

    AddXpValueListeners();
  }

  public override void OnDisable()
  {
    ModEvents.Display.RenderingHud -= OnRenderingHud;
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked_HandleTimers;
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked_CheckTools;
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked_CheckSkillLevels;
    ModEvents.Player.LevelChanged -= OnLevelChanged;
    eventsManager.OnMasteryXpGain -= OnMasteryXpChange;

    if (Context.IsWorldReady)
    {
      RemoveXpValueListeners();
    }
  }

  /*********
   ** Event Handlers
   *********/

  private void OnMasteryXpChange(object? sender, MasteryXpGainArgs args)
  {
    UpdateTrackedSkill(MasterySkill.Value);
  }

  private void OnLevelChanged(object? sender, LevelChangedEventArgs e)
  {
    if (
      !e.IsLocalPlayer
      || !_baseSkills.Value.TryGetValue((int)e.Skill, out VanillaSkillWrapper? skill)
    )
    {
      return;
    }

    ShowLevelUpMessage(skill);
  }

  private void OnUpdateTicked_CheckTools(object? sender, UpdateTickedEventArgs e)
  {
    Item? currentItem = Game1.player.CurrentItem;
    if (!e.IsMultipleOf(15) || currentItem == _previousItem.Value)
    {
      return;
    }

    _previousItem.Value = currentItem;
    int skillIndex = GetSkillIndexFromTool(currentItem);
    if (_baseSkills.Value.TryGetValue(skillIndex, out VanillaSkillWrapper? skill))
    {
      UpdateTrackedSkill(skill, true);
    }
  }

  private void OnUpdateTicked_CheckSkillLevels(object? sender, UpdateTickedEventArgs e)
  {
    foreach ((int key, VanillaSkillWrapper value) in _baseSkills.Value)
    {
      if (value.IsMaxLevel || value.GetLiveXpGained() <= 0)
      {
        continue;
      }

      UpdateTrackedSkill(value);
      ModEntry.DebugLog($"Stardew skill {key} updated");
    }

    foreach ((string key, SpaceCoreSkillWrapper value) in _spaceCoreSkills.Value)
    {
      if (value.IsMaxLevel || value.GetLiveXpGained() <= 0)
      {
        continue;
      }

      UpdateTrackedSkill(value);

      if (value.HasLevelledUp)
      {
        ShowLevelUpMessage(value);
      }
      ModEntry.DebugLog($"SpaceCore skill {key} updated");
    }
  }

  private void OnUpdateTicked_HandleTimers(object? sender, UpdateTickedEventArgs e)
  {
    _displayedExperienceBar.Value.Update();
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally())
    {
      return;
    }

    _displayedExperienceBar.Value.Draw(e.SpriteBatch);
  }

  /*********
   ** Helper Functions
   *********/

  private void UpdateTrackedSkill(SkillWrapperBase skill, bool forceShow = false)
  {
    if (!skill.UpdateExperience() && !forceShow)
    {
      return;
    }

    XpThreshold threshold = skill.GetThresholdData();
    int expGained = skill.ExpSinceLastUpdate;

    if (threshold.NextLevelXp > 0 && expGained > 0)
    {
      AddFloatingXpText(expGained);
    }
    _displayedExperienceBar.Value.SetTrackedSkill(skill);
  }

  private void ShowLevelUpMessage(SkillWrapperBase skill)
  {
    if (!Config.ShowLevelUpAnimation)
    {
      return;
    }

    floatingTextManager.Add(new LevelUpMessage(skill));
    soundHelper.Play(Sounds.LevelUp);
  }

  private static int GetSkillIndexFromTool(Item? currentItem)
  {
    return currentItem switch
    {
      FishingRod => (int)SkillType.Fishing,
      Pickaxe => (int)SkillType.Mining,
      MeleeWeapon weapon when weapon.Name != "Scythe" => (int)SkillType.Combat,
      _ when Game1.currentLocation is Farm && currentItem is not Axe => (int)SkillType.Farming,
      _ => (int)SkillType.Foraging,
    };
  }

  private void AddFloatingXpText(int experienceGain)
  {
    if (!Config.ShowExperienceGain)
    {
      return;
    }

    floatingTextManager.Add(
      new FloatingText(
        $"Exp: {experienceGain}",
        XpVisibleTicks,
        new Vector2(0, -0.5f),
        true,
        FloatingTextKey,
        fullAlphaTicks: 20
      )
    );
  }

  #region NetField listeners
  private void AddXpValueListeners()
  {
    for (var i = 0; i < Game1.player.experiencePoints.Length; i++)
    {
      var newWrapper = new VanillaSkillWrapper(i);
      newWrapper.UpdateExperience();
      _baseSkills.Value[i] = newWrapper;
    }

    // Spacecore listeners

    string[] allSkills = spaceCoreHelper.GetSkillIds();
    foreach (string skillId in allSkills)
    {
      if (_spaceCoreSkills.Value.ContainsKey(skillId))
      {
        continue;
      }
      SpaceCoreSkill? skillInstance = spaceCoreHelper.GetSkill(skillId);
      if (skillInstance is null)
      {
        continue;
      }
      var newWrapper = new SpaceCoreSkillWrapper(skillInstance);
      newWrapper.UpdateExperience();
      _spaceCoreSkills.Value[skillId] = newWrapper;
      ModEntry.DebugLog($"SpaceCore skill {skillId} added");
    }
  }

  private void RemoveXpValueListeners()
  {
    _baseSkills.Value.Clear();
    _spaceCoreSkills.Value.Clear();
  }
  #endregion

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
    return I18n.Gmcm_Group_XpBar();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Xpbar_Enable,
      tooltip: I18n.Gmcm_Modules_Xpbar_Enable_Tooltip,
      getValue: () => Config.ShowExperienceBar,
      setValue: value => Config.ShowExperienceBar = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Xpbar_Fadeout,
      tooltip: I18n.Gmcm_Modules_Xpbar_Fadeout_Tooltip,
      getValue: () => Config.AllowExperienceBarToFadeOut,
      setValue: value => Config.AllowExperienceBarToFadeOut = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Xpbar_Gain,
      tooltip: I18n.Gmcm_Modules_Xpbar_Gain_Tooltip,
      getValue: () => Config.ShowExperienceGain,
      setValue: value =>
      {
        if (!value)
        {
          floatingTextManager.ClearId(FloatingTextKey);
        }

        Config.ShowExperienceGain = value;
      }
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Xpbar_Levelup,
      tooltip: I18n.Gmcm_Modules_Xpbar_Levelup_Tooltip,
      getValue: () => Config.ShowLevelUpAnimation,
      setValue: value => Config.ShowLevelUpAnimation = value
    );
  }
  #endregion
}
