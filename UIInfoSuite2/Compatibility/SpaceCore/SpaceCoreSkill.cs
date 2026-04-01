using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using UIInfoSuite2.Modules.Hud;

namespace UIInfoSuite2.Compatibility.SpaceCore;

/// <summary>
/// A representation of SpaceCore's internal skill data.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class SpaceCoreSkill
{
  // ReSharper disable once NotAccessedField.Local
  private object _backingObject;
  private readonly ISpaceCoreApi _spaceCoreApi;

  private readonly IReflectedProperty<string> _refl_Id;
  private readonly IReflectedProperty<Texture2D> _refl_Icon;
  private readonly IReflectedProperty<Texture2D> _refl_SkillsPageIcon;
  private readonly IReflectedProperty<int[]> _refl_ExperienceCurve;
  private readonly IReflectedProperty<Color> _refl_ExperienceBarColor;
  private readonly Func<string> _refl_GetName;

  /// <summary>
  /// If constructed by itself, this will throw an exception if any of its required method calls fail.
  /// It's recommended to get these through SpaceCoreSkillHelper which will return a safe/nullable reference
  /// </summary>
  public SpaceCoreSkill(ISpaceCoreApi spaceCoreApi, object backingObject)
  {
    _spaceCoreApi = spaceCoreApi;
    _backingObject = backingObject;

    var reflectionHelper = ModEntry.GetSingleton<IReflectionHelper>();
    _refl_Id = reflectionHelper.GetProperty<string>(backingObject, "Id");
    _refl_Icon = reflectionHelper.GetProperty<Texture2D>(backingObject, "Icon");
    _refl_SkillsPageIcon = reflectionHelper.GetProperty<Texture2D>(backingObject, "SkillsPageIcon");
    _refl_ExperienceCurve = reflectionHelper.GetProperty<int[]>(backingObject, "ExperienceCurve");
    _refl_ExperienceBarColor = reflectionHelper.GetProperty<Color>(
      backingObject,
      "ExperienceBarColor"
    );
    MethodInfo getNameMethod = reflectionHelper.GetMethod(backingObject, "GetName").MethodInfo;
    _refl_GetName =
      (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), backingObject, getNameMethod);
  }

  public string Id => _refl_Id.GetValue();
  public Texture2D Icon => _refl_Icon.GetValue();
  public Texture2D SkillsPageIcon => _refl_SkillsPageIcon.GetValue();
  public int[] ExperienceCurve => _refl_ExperienceCurve.GetValue();
  public Color ExperienceBarColor => _refl_ExperienceBarColor.GetValue();

  public string GetName()
  {
    return _refl_GetName.Invoke();
  }

  public int GetMaximumLevel()
  {
    return ExperienceCurve.Length;
  }

  /// <returns>Total accumulated experience.</returns>
  public int GetTotalCurrentExperience()
  {
    return _spaceCoreApi.GetExperienceForCustomSkill(Game1.player, Id);
  }

  /// <returns>Experience required to reach this level from the previous level.</returns>
  public int GetExperienceRequiredForLevel(int level)
  {
    if (level > ExperienceCurve.Length)
    {
      return -1;
    }

    return level switch
    {
      0 => 0,
      1 => ExperienceCurve[level - 1],
      _ => ExperienceCurve[level - 1] - ExperienceCurve[level - 2],
    };
  }

  /// <returns>Accumulated experience required to reach this level from zero.</returns>
  public int GetBaseExperienceForLevel(int level)
  {
    if (level > ExperienceCurve.Length)
    {
      return -1;
    }

    return level switch
    {
      0 => 0,
      _ => ExperienceCurve[level - 1],
    };
  }

  /// <returns>
  /// Difference between total accumulated experience and experience required to reach level.
  /// Negative if level is lower than current skill level.
  /// </returns>
  public int GetExperienceRemainingUntilLevel(int level)
  {
    return GetBaseExperienceForLevel(level) - GetTotalCurrentExperience();
  }
}
