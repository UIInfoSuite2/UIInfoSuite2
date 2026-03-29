using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Layout.Measurement;

namespace UIInfoSuite2.Models.Sprite;

internal class PulseTimer
{
  private int _pulseTimeDefault;
  private int _pulseCooldownDefault;

  private int _pulseCooldown;

  public PulseTimer(int pulseTime, int cooldownTime)
  {
    _pulseTimeDefault = pulseTime;
    _pulseCooldownDefault = cooldownTime;
  }

  public int PulseTimeDefault
  {
    get => _pulseTimeDefault;
    set
    {
      _pulseTimeDefault = value;
      Reset();
    }
  }

  public int PulseCooldownDefault
  {
    get => _pulseCooldownDefault;
    set
    {
      _pulseCooldownDefault = value;
      Reset();
    }
  }

  public int PulseTime { get; private set; }

  public void Reset()
  {
    PulseTime = _pulseTimeDefault;
    _pulseCooldown = _pulseCooldownDefault;
  }

  public void Update()
  {
    var elapsed = (int)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;

    if (IsPulsing)
    {
      PulseTime -= elapsed;
    }
    else if (_pulseCooldown > 0)
    {
      _pulseCooldown -= elapsed;
    }
    else
    {
      Reset();
    }
  }

  public bool IsPulsing => PulseTime > 0;
}

internal class ShakingSprite
{
  private Texture2D _texture;
  private Rectangle _sourceBounds;
  private AspectLockedDimensions _dimensions;
  private Vector2 _shake = Vector2.Zero;
  private float _scale = 1.0f;

  public PulseTimer PulseTimer { get; set; } = new(1000, 3000);
  private float PulseTime => PulseTimer.PulseTime;
  private float BaseScale => _dimensions.ScaleFactor;

  public ShakingSprite(
    Texture2D texture,
    Rectangle sourceBounds,
    float finalSize,
    PrimaryDimension primaryDimension
  )
  {
    _texture = texture;
    _sourceBounds = sourceBounds;
    _dimensions = new AspectLockedDimensions(sourceBounds, finalSize, primaryDimension);
  }

  public void Update()
  {
    PulseTimer.Update();

    _scale = BaseScale;
    if (!PulseTimer.IsPulsing)
    {
      return;
    }

    float pulseScale = 1f / (Math.Max(300f, Math.Abs(PulseTime % 1000 - 500)) / 500f);
    _scale = BaseScale * pulseScale;
    if (pulseScale > 1f)
    {
      _shake = new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
    }
  }

  public void Draw(SpriteBatch batch, Vector2 pos)
  {
    Vector2 origin = new(_sourceBounds.Width / 2f, _sourceBounds.Height / 2f);
    Vector2 scaleOffset = origin * BaseScale;

    batch.Draw(
      _texture,
      pos + scaleOffset + _shake,
      _sourceBounds,
      Color.White,
      0f,
      origin,
      _scale,
      SpriteEffects.None,
      1f
    );
  }
}
