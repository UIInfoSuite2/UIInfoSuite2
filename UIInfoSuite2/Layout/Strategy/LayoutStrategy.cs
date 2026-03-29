using System.Collections.Generic;
using UIInfoSuite2.Layout.Measurement;

namespace UIInfoSuite2.Layout.Strategy;

/// <summary>
///   Base class for all container layout strategies. A strategy is responsible for two phases:
///   * MeasureContent - computes how much space the children collectively require.
///   * ArrangeChildren - positions each child within the measured space and populates the visible-children list for the
///   draw pass.
///   The container calls each phase in order during its own layout cycle.
/// </summary>
internal abstract class LayoutStrategy
{
  /// <summary>
  ///   Measures the space required by allChildren. Called during the
  ///   <c>UpdateBounds</c> phase. Implementations should call <c>child.Layout()</c> on each
  ///   child so they measure themselves before reading <c>child.Bounds.Size</c>.
  /// </summary>
  public abstract Dimensions MeasureContent(IReadOnlyList<LayoutElement> allChildren);

  /// <summary>
  ///   Positions children within the provided contentSize and populates
  ///   visibleChildren with every child that should be drawn this frame.
  ///   Implementations must call <c>visibleChildren.Clear()</c> at the start.
  /// </summary>
  public abstract void ArrangeChildren(
    Dimensions contentSize,
    IReadOnlyList<LayoutElement> allChildren,
    List<LayoutElement> visibleChildren
  );
}
