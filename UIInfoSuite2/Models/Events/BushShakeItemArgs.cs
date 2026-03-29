using System;
using StardewValley.TerrainFeatures;

namespace UIInfoSuite2.Models.Events;

public class BushShakeItemArgs(Bush bush) : EventArgs
{
  public Bush Bush = bush;
}
