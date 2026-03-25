using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.DebugMenu;

/// <summary>
///   A solid-color rectangle used to visually exercise the layout system.
///   Renders using the element's actual bounds so FlexGrow and Stretch results are visible.
/// </summary>
internal class TestColorBox(int width, int height, Color color) : LayoutElement
{
  protected override Dimensions MeasureContent()
  {
    return new Dimensions(width, height);
  }

  protected override void DrawContent(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    int w = Bounds.Width - Margin.HorizontalTotal() - Padding.HorizontalTotal();
    int h = Bounds.Height - Margin.VerticalTotal() - Padding.VerticalTotal();

    if (w > 0 && h > 0)
    {
      spriteBatch.Draw(Game1.staminaRect, new Rectangle(positionX, positionY, w, h), color);
    }
  }
}
