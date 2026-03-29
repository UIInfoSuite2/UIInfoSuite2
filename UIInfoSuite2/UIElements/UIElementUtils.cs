using StardewModdingAPI;
using StardewValley;

namespace UIInfoSuite2.UIElements;

public static class UIElementUtils
{
  public static bool IsRenderingNormally()
  {
    return Game1.gameMode == 3
      && Game1.displayHUD
      && !Game1.game1.takingMapScreenshot
      && (!Game1.eventUp || Game1.isFestival())
      && !Game1.viewportFreeze
      && !Game1.freezeControls
      && !Game1.panMode
      && !Game1.HostPaused
      && Game1.viewportHold <= 0
      && Game1.farmEvent == null
      && Game1.currentMinigame == null
      && !Game1.showingEndOfNightStuff
      && Game1.farmEventOverride == null
      && Context.IsWorldReady
      && Game1.currentLocation != null;
  }
}
