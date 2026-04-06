using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using UIInfoSuite2.Config;
using UIInfoSuite2.Managers;
using UIInfoSuite2.Models.Icons;

namespace UIInfoSuite2.Modules.Base;

internal abstract class SingleHudIconModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconManager iconManager
) : SingleHudIconModule<ClickableIcon>(modEvents, logger, configManager, iconManager);

internal abstract class SingleHudIconModule<T>(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconManager iconManager
) : HudIconModule(modEvents, logger, configManager, iconManager)
  where T : ClickableIcon
{
  private readonly PerScreen<T?> _icon = new(() => null);

  protected T Icon
  {
    get
    {
      if (_icon.Value == null)
      {
        SetupIcons();
      }

      return _icon.Value!;
    }
    set => _icon.Value = value;
  }

  protected abstract string IconKey { get; }

  protected abstract T GenerateNewIcon();

  protected override void SetupIcons()
  {
    RemoveIcons();
    _icon.Value = GenerateNewIcon();
    IconManager.AddIcon(IconKey, _icon.Value);
  }

  protected override void RemoveIcons()
  {
    IconManager.RemoveIcon(IconKey);
  }
}
