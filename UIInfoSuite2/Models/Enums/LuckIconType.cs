using System;
using System.Collections.Generic;

namespace UIInfoSuite2.Models.Enums;

public enum LuckIconType
{
  Classic,
  Clover,
  Tv,
}

public static class LuckIconTypeExtensions
{
  public static readonly Dictionary<string, LuckIconType> StringToType = new()
  {
    [I18n.Gmcm_Enum_LuckIconType_Classic()] = LuckIconType.Classic,
    [I18n.Gmcm_Enum_LuckIconType_Clover()] = LuckIconType.Clover,
    [I18n.Gmcm_Enum_LuckIconType_Tv()] = LuckIconType.Tv,
  };

  public static LuckIconType FromModConfigString(string value)
  {
    return StringToType.GetValueOrDefault(value, LuckIconType.Clover);
  }

  public static string ToModConfigString(this LuckIconType type)
  {
    return type switch
    {
      LuckIconType.Classic => I18n.Gmcm_Enum_LuckIconType_Classic(),
      LuckIconType.Clover => I18n.Gmcm_Enum_LuckIconType_Clover(),
      LuckIconType.Tv => I18n.Gmcm_Enum_LuckIconType_Tv(),
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
  }
}
