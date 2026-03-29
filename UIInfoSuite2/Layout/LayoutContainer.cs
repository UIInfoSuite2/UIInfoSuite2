using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Extensions;
using UIInfoSuite2.Helpers;
using UIInfoSuite2.Layout.Enums;
using UIInfoSuite2.Layout.Measurement;
using UIInfoSuite2.Layout.Strategy;

namespace UIInfoSuite2.Layout;

internal class LayoutContainer : LayoutElement
{
  /// <summary>
  ///   Row/Column shorthand for the <see cref="Direction" /> property. Subset of
  ///   <see cref="FlexDirection" /> kept for backward compatibility. Use
  ///   <see cref="FlexDirection" /> when reverse directions are needed.
  /// </summary>
  public enum LayoutDirection
  {
    Row,
    Column,
  }

  private readonly List<LayoutElement> _children = [];
  private readonly FlexLayoutStrategy _flexStrategy = new();
  private readonly List<LayoutElement> _visibleChildren = [];
  private LayoutStrategy _strategy = null!; // set in every constructor path

  public LayoutContainer(string? identifier, params LayoutElement[] children)
    : base(identifier)
  {
    _strategy = _flexStrategy;
    AddChildren(children);
  }

  public LayoutContainer(params LayoutElement[] children)
    : this(null, children) { }

  /// <summary>
  ///   When true, the container automatically hides itself whenever all of its children are hidden,
  ///   and shows itself again as soon as any child becomes visible. Applied during the layout pass.
  /// </summary>
  public bool AutoHideWhenEmpty { get; set; }

  // region Layout Strategy

  public LayoutStrategy Strategy
  {
    get => _strategy;
    set
    {
      _strategy = value ?? _flexStrategy;
      MarkLayoutDirty(Id);
    }
  }

  public LayoutDirection Direction
  {
    get =>
      _flexStrategy.Direction is FlexDirection.Row or FlexDirection.RowReverse
        ? LayoutDirection.Row
        : LayoutDirection.Column;
    set
    {
      FlexDirection mapped =
        value == LayoutDirection.Row ? FlexDirection.Row : FlexDirection.Column;
      if (_flexStrategy.Direction == mapped)
      {
        return;
      }

      _flexStrategy.Direction = mapped;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  public FlexDirection FlexDirection
  {
    get => _flexStrategy.Direction;
    set
    {
      if (_flexStrategy.Direction == value)
      {
        return;
      }

      _flexStrategy.Direction = value;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  public int ComponentSpacing
  {
    get => _flexStrategy.Gap;
    set
    {
      if (_flexStrategy.Gap == value)
      {
        return;
      }

      _flexStrategy.Gap = value;
      MarkLayoutDirty(Id);
    }
  }

  public Alignment Alignment
  {
    get => _flexStrategy.GridAlignment ?? Alignment.TopLeft;
    set
    {
      if (_flexStrategy.GridAlignment == value)
      {
        return;
      }

      _flexStrategy.GridAlignment = value;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  public JustifyContent JustifyContent
  {
    get => _flexStrategy.JustifyContent;
    set
    {
      if (_flexStrategy.JustifyContent == value)
      {
        return;
      }

      // Clear GridAlignment so it does not override the explicit setting
      _flexStrategy.GridAlignment = null;
      _flexStrategy.JustifyContent = value;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  public AlignItems AlignItems
  {
    get => _flexStrategy.AlignItems;
    set
    {
      if (_flexStrategy.AlignItems == value)
      {
        return;
      }

      // Clear GridAlignment so it does not override the explicit setting
      _flexStrategy.GridAlignment = null;
      _flexStrategy.AlignItems = value;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  // endregion

  public override void Dispose()
  {
    if (Parent is LayoutContainer parentContainer)
    {
      parentContainer.RemoveChild(this);
    }

    UnsetParent();
    foreach (LayoutElement child in _children)
    {
      child.UnsetParent();
    }

    _children.Clear();
  }

  // region Helper Chain Methods

  public LayoutContainer WithSpacing(int spacing)
  {
    ComponentSpacing = spacing;
    return this;
  }

  public LayoutContainer WithAlignment(Alignment alignment)
  {
    Alignment = alignment;
    return this;
  }

  public LayoutContainer WithFlexDirection(FlexDirection direction)
  {
    FlexDirection = direction;
    return this;
  }

  public LayoutContainer WithJustifyContent(JustifyContent justify)
  {
    JustifyContent = justify;
    return this;
  }

  public LayoutContainer WithAlignItems(AlignItems align)
  {
    AlignItems = align;
    return this;
  }

  // endregion
  // region Factory Helper Methods

  public static LayoutContainer Row(string? identifier, params LayoutElement[] children)
  {
    var c = new LayoutContainer(identifier);
    c.Direction = LayoutDirection.Row;
    c.AddChildren(children);
    return c;
  }

  public static LayoutContainer Row(
    string? identifier,
    int spacing,
    params LayoutElement[] children
  )
  {
    var c = new LayoutContainer(identifier);
    c.Direction = LayoutDirection.Row;
    c.ComponentSpacing = spacing;
    c.AddChildren(children);
    return c;
  }

  public static LayoutContainer Column(string? identifier, params LayoutElement[] children)
  {
    var c = new LayoutContainer(identifier);
    c.Direction = LayoutDirection.Column;
    c.AddChildren(children);
    return c;
  }

  public static LayoutContainer Column(
    string? identifier,
    int spacing,
    params LayoutElement[] children
  )
  {
    var c = new LayoutContainer(identifier);
    c.ComponentSpacing = spacing;
    c.Direction = LayoutDirection.Column;
    c.AddChildren(children);
    return c;
  }

  // endregion

  public void AddChildren(params LayoutElement[] components)
  {
    _children.EnsureCapacity(_children.Count + components.Length);
    foreach (LayoutElement component in components)
    {
      component.Parent = this;
      _children.Add(component);
    }

    MarkLayoutDirty(Id);
  }

  public void RemoveChildren()
  {
    foreach (LayoutElement component in _children)
    {
      component.UnsetParent();
    }

    _children.Clear();
    MarkLayoutDirty(Id);
  }

  public void RemoveChild(LayoutElement element)
  {
    element.UnsetParent();
    _children.Remove(element);
    MarkLayoutDirty(Id);
  }

  protected bool AllChildrenHidden()
  {
    return _children.TrueForAll(e => e.IsHidden);
  }

  // region Layout

  protected internal override void PropagateLayoutChange(LayoutElement? caller = null)
  {
    if (caller is not null && IsDirty)
    {
      return;
    }

    if (caller is not null)
    {
      DirtyFlags |= LayoutDirtyFlags.Layout;
    }

    ModEntry.LayoutDebug($"{GetType().Name}::{caller?.Id} Propagated layout change");
    Parent?.PropagateLayoutChange(this);
  }

  protected internal override void Layout()
  {
    if (!NeedsLayout)
    {
      return;
    }

    UpdateBounds();

    if (AutoHideWhenEmpty)
    {
      bool allHidden = AllChildrenHidden();
      if (allHidden != IsHidden)
      {
        IsHidden = allHidden;
        UpdateBounds();
      }
    }

    _strategy.ArrangeChildren(ContentSize, _children, _visibleChildren);

    ResetDirty();
  }

  protected override Dimensions MeasureContent()
  {
    if (_children.Count == 0)
    {
      return Dimensions.Empty;
    }

    return _strategy.MeasureContent(_children);
  }

  // endregion

  // region Drawing

  public override void Draw(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    if (IsHidden && !NeedsLayout)
    {
      return;
    }

    Layout();

    if (_visibleChildren.Count == 0)
    {
      return;
    }

    DrawSelf(spriteBatch, positionX, positionY);
    if (ModEntry.Config.DrawDebugBounds)
    {
      DrawDebugBounds(spriteBatch, positionX, positionY);
    }

    int baseX = positionX + Margin.Left.OrZero() + Padding.Left.OrZero();
    int baseY = positionY + Margin.Top.OrZero() + Padding.Top.OrZero();

    foreach (LayoutElement component in _visibleChildren)
    {
      component.Draw(
        spriteBatch,
        baseX + component.Bounds.OffsetX,
        baseY + component.Bounds.OffsetY
      );
    }
  }

  protected void DrawContainerBox(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    int x = positionX + Margin.Left.OrZero();
    int y = positionY + Margin.Top.OrZero();
    int finalWidth = ContentSize.Width + Padding.HorizontalTotal();
    int finalHeight = ContentSize.Height + Padding.VerticalTotal();
    IClickableMenu.drawTextureBox(
      spriteBatch,
      Game1.menuTexture,
      TextureHelper.OutlinedTextureBox,
      x,
      y,
      finalWidth,
      finalHeight,
      Color.White
    );
  }

  // endregion
}
