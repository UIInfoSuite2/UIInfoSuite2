using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UIInfoSuite2.Helpers;

/// <summary>
/// Draws small numbers using the game's tiny digit sprites from Game1.mouseCursors (368, 56).
/// Same sprites the game uses for e.g. stack count in inventory.
/// </summary>
internal static class TinyDigitHelper
{
  private const int SpriteX = 368;
  private const int SpriteY = 56;
  private const int DigitWidth = 5;
  private const int DigitHeight = 7;
  private const int ColonPadding = 2;
  private const int ColonDotGap = 4;

  /// <summary>Draws a number at the given position with optional shadow.</summary>
  public static float DrawNumber(
    SpriteBatch b,
    int number,
    Vector2 position,
    float scale,
    Color color,
    Color? shadowColor = null
  )
  {
    float xOffset = 0;
    int step = (int)(DigitWidth * scale) - 1;

    if (shadowColor.HasValue)
    {
      float shadowOffset = 0;
      DrawDigits(
        b,
        number,
        position + Vector2.One,
        scale,
        step,
        shadowColor.Value,
        ref shadowOffset
      );
    }

    DrawDigits(b, number, position, scale, step, color, ref xOffset);
    return xOffset;
  }

  /// <summary>Draws a colon separator (two dots) at the given position.</summary>
  public static float DrawColon(
    SpriteBatch b,
    Vector2 position,
    float xOffset,
    float scale,
    Color color,
    Color? shadowColor = null
  )
  {
    float dotSize = scale;
    float scaledHeight = DigitHeight * scale;
    float dotX = position.X + xOffset + ColonPadding + (ColonDotGap - dotSize) / 2f;

    var upperPos = new Vector2(dotX, position.Y + scaledHeight * 0.25f);
    var lowerPos = new Vector2(dotX, position.Y + scaledHeight * 0.6f);

    if (shadowColor.HasValue)
    {
      DrawDot(b, upperPos + Vector2.One, dotSize, shadowColor.Value, 0.99f);
      DrawDot(b, lowerPos + Vector2.One, dotSize, shadowColor.Value, 0.99f);
    }

    DrawDot(b, upperPos, dotSize, color, 1f);
    DrawDot(b, lowerPos, dotSize, color, 1f);

    return ColonPadding + ColonDotGap + ColonPadding;
  }

  /// <summary>Measures the pixel width of a number at the given scale.</summary>
  public static int MeasureNumber(int number, float scale)
  {
    int step = (int)(DigitWidth * scale) - 1;
    int digitCount = number == 0 ? 1 : (int)Math.Floor(Math.Log10(number)) + 1;
    return digitCount * step;
  }

  /// <summary>Measures the pixel width of a colon separator.</summary>
  public static int MeasureColon()
  {
    return ColonPadding + ColonDotGap + ColonPadding;
  }

  private static void DrawDigits(
    SpriteBatch b,
    int number,
    Vector2 position,
    float scale,
    int step,
    Color color,
    ref float xOffset
  )
  {
    if (number == 0)
    {
      DrawSingleDigit(b, 0, position, scale, step, color, ref xOffset);
      return;
    }

    int digitCount = 0;
    int temp = number;
    while (temp > 0)
    {
      digitCount++;
      temp /= 10;
    }

    int divisor = (int)Math.Pow(10, digitCount - 1);
    for (int i = 0; i < digitCount; i++)
    {
      int digit = number / divisor % 10;
      DrawSingleDigit(b, digit, position, scale, step, color, ref xOffset);
      divisor /= 10;
    }
  }

  private static void DrawSingleDigit(
    SpriteBatch b,
    int digit,
    Vector2 position,
    float scale,
    int step,
    Color color,
    ref float xOffset
  )
  {
    var sourceRect = new Rectangle(SpriteX + digit * DigitWidth, SpriteY, DigitWidth, DigitHeight);

    b.Draw(
      Game1.mouseCursors,
      position + new Vector2(xOffset, 0f),
      sourceRect,
      color,
      0f,
      Vector2.Zero,
      scale,
      SpriteEffects.None,
      1f
    );

    xOffset += step;
  }

  private static void DrawDot(
    SpriteBatch b,
    Vector2 position,
    float size,
    Color color,
    float layerDepth
  )
  {
    b.Draw(
      Game1.staminaRect,
      new Rectangle((int)position.X, (int)position.Y, (int)size, (int)size),
      null,
      color,
      0f,
      Vector2.Zero,
      SpriteEffects.None,
      layerDepth
    );
  }
}
