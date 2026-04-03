using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using UIInfoSuite2.Config;
using UIInfoSuite2.Extensions;
using UIInfoSuite2.Layout.Measurement;

namespace UIInfoSuite2.Models.Icons;

internal class ClickableIcon
{
  /// <summary>
  ///   If the icon has decided it shouldn't be rendered, or some other event that might
  ///   invalidate caching
  /// </summary>
  private readonly PerScreen<bool> _hasRenderingChanged = new(() => false);

  private readonly PerScreen<string> _hoverText = new(() => string.Empty);
  private readonly PerScreen<ClickableTextureComponent> _icon;
  private readonly PerScreen<bool> _lastShouldDraw = new(() => false);
  protected readonly PerScreen<Texture2D> BaseTexture;
  protected readonly PerScreen<Color> _color = new(() => Color.White);

  protected readonly ConfigManager ConfigManager = ModEntry.GetSingleton<ConfigManager>();
  protected readonly PerScreen<AspectLockedDimensions> ScalingDimensions;
  protected readonly IMonitor Logger;

  public ClickableIcon(
    ParsedItemData itemData,
    float finalSize,
    PrimaryDimension primaryDimension = PrimaryDimension.Width,
    Action<object?, ButtonPressedEventArgs, Vector2>? clickHandlerAction = null,
    SpriteFont? hoverFont = null
  )
    : this(
      itemData.GetTexture(),
      itemData.GetSourceRect(),
      finalSize,
      primaryDimension,
      clickHandlerAction,
      hoverFont
    ) { }

  public ClickableIcon(
    Texture2D baseTexture,
    Rectangle sourceBounds,
    float finalSize,
    PrimaryDimension primaryDimension = PrimaryDimension.Width,
    Action<object?, ButtonPressedEventArgs, Vector2>? clickHandlerAction = null,
    SpriteFont? hoverFont = null
  )
  {
    BaseTexture = new PerScreen<Texture2D>(() => baseTexture);
    ScalingDimensions = new PerScreen<AspectLockedDimensions>(() =>
      new AspectLockedDimensions(sourceBounds, finalSize, primaryDimension)
    );

    _icon = new PerScreen<ClickableTextureComponent>(GenerateTextureComponent);

    ResetTextureComponent();
    ClickHandlerAction = clickHandlerAction;
    HoverFont = hoverFont ?? Game1.dialogueFont;
    AutoDrawDelegate = Draw;
    Logger = ModEntry.GetSingleton<IMonitor>();
  }

  protected ModConfig Config => ConfigManager.Config;

  public int RenderPriority { get; set; }

  public virtual string HoverText
  {
    get => _hoverText.Value;
    set => _hoverText.Value = FormatHoverText(value);
  }

  public AspectLockedDimensions Dimensions => ScalingDimensions.Value;

  public SpriteFont HoverFont { get; }

  public Rectangle SourceBounds
  {
    get => ScalingDimensions.Value.SourceDimensions;
    set
    {
      _hasRenderingChanged.Value = true;
      ScalingDimensions.Value.SourceDimensions = value;
    }
  }

  public Color Color
  {
    get => _color.Value;
    set => _color.Value = value;
  }

  public Action<SpriteBatch> AutoDrawDelegate { get; set; }

  public Action<object?, ButtonPressedEventArgs, Vector2>? ClickHandlerAction { private get; set; }

  protected ClickableTextureComponent Icon => _icon.Value;

  public Vector2 IconPosition => new(Icon.bounds.X, Icon.bounds.Y);

  public string FormatHoverText(string text)
  {
    return Game1.parseText(text, HoverFont, 600);
  }

  public void SetSourceBounds(Rectangle rectangle, bool generateComponent = true)
  {
    SourceBounds = rectangle;
    if (generateComponent)
    {
      ResetTextureComponent();
    }
  }

  private ClickableTextureComponent GenerateTextureComponent()
  {
    return new ClickableTextureComponent(
      new Rectangle(0, 0, Dimensions.Width, Dimensions.Height),
      BaseTexture.Value,
      SourceBounds,
      Dimensions.ScaleFactor
    );
  }

  protected void ResetTextureComponent()
  {
    _icon.Value = GenerateTextureComponent();
  }

  protected static Lazy<Texture2D> LazyLoadModTexture(params string[] pathStrings)
  {
    return new Lazy<Texture2D>(() =>
    {
      var helper = ModEntry.GetSingleton<IModHelper>();
      string path = pathStrings.Aggregate(helper.DirectoryPath, Path.Combine);
      return Texture2D.FromFile(Game1.graphics.GraphicsDevice, path);
    });
  }

  public void MarkDirty()
  {
    _hasRenderingChanged.Value = true;
  }

  public bool HasRenderingChanged(bool markClean = true)
  {
    bool dirty = _hasRenderingChanged.Value;
    if (markClean)
    {
      _hasRenderingChanged.Value = false;
    }

    return dirty;
  }

  protected virtual bool _ShouldDraw()
  {
    return true;
  }

  public bool ShouldDraw()
  {
    bool res = _ShouldDraw();
    if (res == _lastShouldDraw.Value)
    {
      return res;
    }

    _hasRenderingChanged.Value = true;
    _lastShouldDraw.Value = res;
    return res;
  }

  public void MoveTo(int x, int y)
  {
    Icon.setPosition(x, y);
    Icon.baseScale = Dimensions.ScaleFactor;
  }

  public void MoveTo(Point point)
  {
    Icon.setPosition(point.X, point.Y);
    Icon.baseScale = Dimensions.ScaleFactor;
  }

  public virtual void Draw(SpriteBatch b)
  {
    if (!ShouldDraw())
    {
      return;
    }

    // Assume default depth from stardew valley source
    float depth = 0.86f + IconPosition.Y / 20000.0f;

    Icon.draw(b, Color, depth, 0, 0, 0);
  }

  public virtual void DrawHoverText(SpriteBatch batch)
  {
    if (!Icon.IsHoveredOver() || !ShouldDraw())
    {
      return;
    }

    if (string.IsNullOrEmpty(HoverText))
    {
      return;
    }

    IClickableMenu.drawHoverText(batch, HoverText, HoverFont);
  }

  public virtual void OnClick(object? sender, ButtonPressedEventArgs args)
  {
    if (
      ClickHandlerAction is null
      || args.Button != SButton.MouseLeft
      || Game1.player.CursorSlotItem is not null
      || Game1.activeClickableMenu is not GameMenu gameMenu
      || gameMenu.currentTab == GameMenu.mapTab
    )
    {
      return;
    }

    Vector2 mouseCoords = Utility.ModifyCoordinatesForUIScale(
      new Vector2(Game1.getMouseX(), Game1.getMouseY())
    );
    if (!Icon.containsPoint((int)mouseCoords.X, (int)mouseCoords.Y))
    {
      return;
    }

    ClickHandlerAction.Invoke(sender, args, mouseCoords);
  }
}
