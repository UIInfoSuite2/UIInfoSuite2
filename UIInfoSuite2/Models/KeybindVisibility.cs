using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using UIInfoSuite2.Models.Enums;

namespace UIInfoSuite2.Models;

public class KeybindVisibility(Func<KeybindList> keybindGetter, Func<VisibilityMode> modeGetter)
{
  private readonly IInputHelper _inputHelper = ModEntry.GetSingleton<IInputHelper>();

  private VisibilityMode Mode => modeGetter();
  private KeybindList Keybinds => keybindGetter();
  private readonly PerScreen<bool> _isToggled = new(() => false);

  public bool IsVisible =>
    Mode switch
    {
      VisibilityMode.AlwaysOn => true,
      VisibilityMode.KeyPressToggle => _isToggled.Value,
      VisibilityMode.KeyHold => Keybinds.IsDown(),
      _ => false,
    };

  public void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
  {
    if (!Context.IsPlayerFree || !Keybinds.JustPressed() || Mode != VisibilityMode.KeyPressToggle)
    {
      return;
    }

    _inputHelper.SuppressActiveKeybinds(Keybinds);
    _isToggled.Value = !_isToggled.Value;
  }
}
