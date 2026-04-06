using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
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
  private bool _hasRenderingChanged;

  private string _hoverText = "";
  private bool _lastShouldDraw;

  protected readonly ConfigManager ConfigManager = ModEntry.GetSingleton<ConfigManager>();
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
    BaseTexture = baseTexture;
    Dimensions = new AspectLockedDimensions(sourceBounds, finalSize, primaryDimension);

    Icon = GenerateTextureComponent();

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
    get => _hoverText;
    set => _hoverText = FormatHoverText(value);
  }

  protected Texture2D BaseTexture { get; set; }
  protected ClickableTextureComponent Icon { get; set; }
  public AspectLockedDimensions Dimensions { get; protected set; }

  public SpriteFont HoverFont { get; }

  public Rectangle SourceBounds
  {
    get => Dimensions.SourceDimensions;
    set
    {
      _hasRenderingChanged = true;
      Dimensions.SourceDimensions = value;
    }
  }

  public Color Color { get; set; } = Color.White;

  public Action<SpriteBatch> AutoDrawDelegate { get; set; }

  public Action<object?, ButtonPressedEventArgs, Vector2>? ClickHandlerAction { private get; set; }

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
      BaseTexture,
      SourceBounds,
      Dimensions.ScaleFactor
    );
  }

  public void ResetTextureComponent()
  {
    Icon = GenerateTextureComponent();
  }

  public void MarkDirty()
  {
    _hasRenderingChanged = true;
  }

  public bool HasRenderingChanged(bool markClean = true)
  {
    // Have the icon refresh if it's supposed to be drawn
    ShouldDraw();

    bool dirty = _hasRenderingChanged;
    if (markClean)
    {
      _hasRenderingChanged = false;
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
    if (res == _lastShouldDraw)
    {
      return res;
    }

    _hasRenderingChanged = true;
    _lastShouldDraw = res;
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

    Icon.draw(b, Color, depth);
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
