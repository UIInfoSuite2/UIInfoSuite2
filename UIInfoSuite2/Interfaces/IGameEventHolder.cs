namespace UIInfoSuite2.Interfaces;

public interface IGameEventHolder
{
  /// <summary>
  /// Register events that need to be present before the game starts.
  /// </summary>
  void RegisterEarlyEvents() { }

  /// <summary>
  /// Register events that can be loaded after a save
  /// </summary>
  void RegisterGameEvents() { }

  void UnregisterEvents() { }
  void OnConfigChanged() { }
}
