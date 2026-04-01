using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace UIInfoSuite2.Compatibility.SpaceCore;

using Refl_GetSkill_Func = Func<string, object?>;
using Refl_GetSkillLevel_Func = Func<Farmer, string, int>;

/// <summary>
/// For the things we *just can't do* with the SpaceCore API
/// </summary>
public class SpaceCoreHelper
{
  // Helper cache and locals
  private readonly IMonitor _logger;
  private readonly IReflectionHelper _reflectionHelper;
  private readonly Dictionary<string, SpaceCoreSkill?> _skillCache = new();
  private readonly Lazy<ISpaceCoreApi?> _spaceCoreApi;

  // Reflected types and methods
  private readonly Lazy<Refl_GetSkill_Func?> _getSkillMethod;
  private readonly Lazy<Refl_GetSkillLevel_Func?> _getSkillLevelMethod;

  public SpaceCoreHelper(IMonitor logger, IReflectionHelper reflectionHelper, ApiManager apiManager)
  {
    _logger = logger;
    _reflectionHelper = reflectionHelper;
    _getSkillMethod = ReflectSkillStaticMethod<Refl_GetSkill_Func>("GetSkill");
    _getSkillLevelMethod = ReflectSkillStaticMethod<Refl_GetSkillLevel_Func>("GetSkillLevel");

    _spaceCoreApi = new Lazy<ISpaceCoreApi?>(() =>
    {
      bool api = apiManager.GetApi(ModCompat.SpaceCore, out ISpaceCoreApi? spaceCoreApi);
      return api ? spaceCoreApi : null;
    });
  }

  private static Lazy<TDelegate?> ReflectSkillStaticMethod<TDelegate>(string methodName)
    where TDelegate : Delegate
  {
    return new Lazy<TDelegate?>(() =>
    {
      if (!ModEntry.Instance.Helper.ModRegistry.IsLoaded(ModCompat.SpaceCore))
      {
        return null;
      }

      Type? type = AccessTools.TypeByName("SpaceCore.Skills");
      if (type is null)
      {
        LogReflectionError(
          "Could not find class SpaceCore.Skills , experience tracking may be inaccurate!"
        );
        return null;
      }

      MethodInfo? method = AccessTools.DeclaredMethod(type, methodName);
      if (method is null)
      {
        LogReflectionError(
          $"Could not find Method SpaceCore.Skills#{methodName}, experience tracking may be inaccurate!"
        );
        return null;
      }

      ModEntry.Instance.Monitor.LogOnce($"Resolving SpaceCore.Skills#{methodName}");
      return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), null, method);
    });
  }

  private static void LogReflectionError(string msg)
  {
    IMonitor logger = ModEntry.Instance.Monitor;
    logger.LogOnce(msg, LogLevel.Error);
  }

  // Actual helper logic

  public SpaceCoreSkill? GetSkill(string skillId)
  {
    ISpaceCoreApi? spaceCoreApi = _spaceCoreApi.Value;
    if (spaceCoreApi is null)
    {
      return null;
    }

    if (_skillCache.TryGetValue(skillId, out SpaceCoreSkill? skill))
    {
      return skill;
    }

    try
    {
      object? skillInstance = _getSkillMethod.Value?.Invoke(skillId);
      if (skillInstance is null)
      {
        _logger.Log(
          $"GetSkill method call succeeded but returned null for skill {skillId}",
          LogLevel.Warn
        );
        return null;
      }
      var wrappedSkill = new SpaceCoreSkill(spaceCoreApi, skillInstance);
      _skillCache[skillId] = wrappedSkill;
      return wrappedSkill;
    }
    catch (Exception e)
    {
      _skillCache[skillId] = null;
      _logger.LogOnce($"Failed to wrap skill {skillId}: {e.Message}", LogLevel.Error);
    }
    return null;
  }

  public string[] GetSkillIds()
  {
    ISpaceCoreApi? spaceCoreApi = _spaceCoreApi.Value;
    return spaceCoreApi is null ? [] : spaceCoreApi.GetCustomSkills();
  }

  public int GetSkillLevel(Farmer farmer, string skillId)
  {
    Refl_GetSkillLevel_Func? getLevel = _getSkillLevelMethod.Value;
    if (_spaceCoreApi.Value is null || getLevel is null)
    {
      return 0;
    }

    try
    {
      return getLevel.Invoke(farmer, skillId);
    }
    catch (Exception e)
    {
      _logger.LogOnce($"Failed to get skill level for {skillId}: {e.Message}", LogLevel.Error);
      return 0;
    }
  }
}
