using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace UIInfoSuite2.Helpers;

internal static class TextureHelper
{
  public static readonly Dictionary<int, Rectangle> SkillIconRectangles = new()
  {
    { Farmer.farmingSkill, new Rectangle(10, 428, 10, 10) },
    { Farmer.fishingSkill, new Rectangle(20, 428, 10, 10) },
    { Farmer.foragingSkill, new Rectangle(60, 428, 10, 10) },
    { Farmer.miningSkill, new Rectangle(30, 428, 10, 10) },
    { Farmer.combatSkill, new Rectangle(120, 428, 10, 10) },
    { Farmer.luckSkill, new Rectangle(50, 428, 10, 10) },
  };

  public static readonly Rectangle MasteryIconRectangle = new(346, 392, 8, 8);

  public static readonly Dictionary<int, Color> SkillFillColors = new()
  {
    { Farmer.farmingSkill, new Color(255, 251, 35, 0.38f) },
    { Farmer.fishingSkill, new Color(17, 84, 252, 0.63f) },
    { Farmer.foragingSkill, new Color(0, 234, 0, 0.63f) },
    { Farmer.miningSkill, new Color(145, 104, 63, 0.63f) },
    { Farmer.combatSkill, new Color(204, 0, 3, 0.63f) },
    { Farmer.luckSkill, new Color(232, 223, 42, 0.63f) },
  };

  public static readonly Rectangle OutlinedTextureBox = new(0, 256, 60, 60);

  public static Lazy<Texture2D> LazyLoadModTexture(params string[] pathStrings)
  {
    return new Lazy<Texture2D>(() =>
    {
      var helper = ModEntry.GetSingleton<IModHelper>();
      string path = pathStrings.Aggregate(helper.DirectoryPath, Path.Combine);
      return Texture2D.FromFile(Game1.graphics.GraphicsDevice, path);
    });
  }

  public static void DrawOutlinedSprite(
    SpriteBatch spriteBatch,
    Texture2D texture,
    Vector2 position,
    Rectangle sourceRectangle,
    Color? color = null,
    float rotation = 0f,
    Vector2? origin = null,
    float scale = 1.0f,
    SpriteEffects effects = SpriteEffects.None,
    float layerDepth = 1.0f,
    float outlineSize = 2f,
    Color? outlineColor = null
  )
  {
    Vector2 resolvedOrigin = origin ?? Vector2.Zero;
    Color resolvedColor = color ?? Color.White * 0.9f;
    Color resolvedOutlineColor = outlineColor ?? Color.Black * 0.5f;

    float outlineScale = scale + outlineSize / sourceRectangle.Width;
    var outlineOffset = new Vector2(outlineSize / 2f, outlineSize / 2f);
    spriteBatch.Draw(
      texture,
      position - outlineOffset,
      sourceRectangle,
      resolvedOutlineColor,
      rotation,
      resolvedOrigin,
      outlineScale,
      effects,
      1f
    );
    spriteBatch.Draw(
      texture,
      position,
      sourceRectangle,
      resolvedColor,
      rotation,
      resolvedOrigin,
      scale,
      effects,
      layerDepth
    );
  }
}
