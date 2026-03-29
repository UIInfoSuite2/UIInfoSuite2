using System;
using System.Collections.Generic;

namespace UIInfoSuite2.Models.Enums;

public enum VisibilityMode
{
  AlwaysOn = 0,
  KeyPressToggle = 1,
  KeyHold = 2,
  Off = 3,
}

public static class VisibilityModeExtensions
{
  public static readonly Dictionary<string, VisibilityMode> StringToMode = new()
  {
    [I18n.Gmcm_Enum_VisibilityMode_Always()] = VisibilityMode.AlwaysOn,
    [I18n.Gmcm_Enum_VisibilityMode_KeyToggle()] = VisibilityMode.KeyPressToggle,
    [I18n.Gmcm_Enum_VisibilityMode_KeyHold()] = VisibilityMode.KeyHold,
    [I18n.Gmcm_Enum_VisibilityMode_Off()] = VisibilityMode.Off,
  };

  public static VisibilityMode FromModConfigString(string value)
  {
    return StringToMode.GetValueOrDefault(value, VisibilityMode.AlwaysOn);
  }

  public static string ToModConfigString(this VisibilityMode mode)
  {
    return mode switch
    {
      VisibilityMode.AlwaysOn => I18n.Gmcm_Enum_VisibilityMode_Always(),
      VisibilityMode.KeyPressToggle => I18n.Gmcm_Enum_VisibilityMode_KeyToggle(),
      VisibilityMode.KeyHold => I18n.Gmcm_Enum_VisibilityMode_KeyHold(),
      VisibilityMode.Off => I18n.Gmcm_Enum_VisibilityMode_Off(),
      _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };
  }
}
