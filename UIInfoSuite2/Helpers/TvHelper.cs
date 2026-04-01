using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using UIInfoSuite2.Interfaces;

namespace UIInfoSuite2.Helpers;

internal class TvHelper(IModEvents modEvents) : IPatchable, IGameEventHolder
{
  private static Lazy<IMonitor> _logger = ModEntry.LazyGetSingleton<IMonitor>();
  public static readonly PerScreen<bool> HasWatchedWeather = new();
  public static readonly PerScreen<bool> HasWatchedFortune = new();

  public void Patch(Harmony harmony)
  {
    harmony.Patch(
      original: AccessTools.Method(typeof(TV), nameof(TV.selectChannel)),
      postfix: new HarmonyMethod(typeof(TvHelper), nameof(OnSelectChannel))
    );
  }

  public void RegisterGameEvents()
  {
    modEvents.GameLoop.DayStarted += OnDayStarted;
  }

  public void UnregisterEvents()
  {
    modEvents.GameLoop.DayStarted -= OnDayStarted;
  }

  private static void OnSelectChannel(string answer)
  {
    string channel = ArgUtility.SplitBySpaceAndGet(answer, 0) ?? "";
    switch (channel)
    {
      case "Weather":
        HasWatchedWeather.Value = true;
        break;
      case "Fortune":
        HasWatchedFortune.Value = true;
        break;
    }

    if (channel is "Weather" or "Fortune")
    {
      _logger.Value.Log($"TvChannelWatcher: channel watched, channel={channel}");
    }
  }

  private static void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    HasWatchedWeather.Value = false;
    HasWatchedFortune.Value = false;
  }
}
