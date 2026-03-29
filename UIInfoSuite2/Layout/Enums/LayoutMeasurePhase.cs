namespace UIInfoSuite2.Layout.Enums;

public enum LayoutMeasurePhase
{
  Initial, // First pass - measure minimal content sizes
  Constrained, // Second pass - measure with constraints from parent/siblings
}
