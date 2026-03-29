using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace UIInfoSuite2.Models.Sprite;

internal class VariantTexture2D<T>(Texture2D baseTexture) : IDisposable
  where T : IConvertible
{
  private readonly Dictionary<T, Texture2D> _variants = new();

  public Texture2D BaseTexture { get; } = baseTexture;

  public void AddVariant(T variant, Action<Texture2D> apply)
  {
    var clone = new Texture2D(BaseTexture.GraphicsDevice, BaseTexture.Width, BaseTexture.Height);
    clone.CopyFromTexture(BaseTexture);
    apply(clone);
    _variants.Add(variant, clone);
  }

  public Texture2D GetTexture(T variant)
  {
    return _variants.GetValueOrDefault(variant, BaseTexture);
  }

  public void Dispose()
  {
    BaseTexture.Dispose();
    foreach (Texture2D variant in _variants.Values)
      variant.Dispose();
  }
}
