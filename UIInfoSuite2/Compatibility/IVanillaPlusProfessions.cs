using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2.Compatibility;

public interface IVanillaPlusProfessions
{
  /// <summary>
  /// Exposes XP limits for VPP's new levels. Index 0 is total experience required for level 11.
  /// </summary>
  int[] LevelExperiences { get; }

  /// <summary>
  /// The config value.
  /// </summary>
  int MasteryCaveChanges { get; }
}
