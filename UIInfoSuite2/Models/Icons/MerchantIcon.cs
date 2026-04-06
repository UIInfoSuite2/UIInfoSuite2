using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Extensions;
using UIInfoSuite2.Layout.Measurement;
using UIInfoSuite2.Models.Sprite;

namespace UIInfoSuite2.Models.Icons;

internal class MerchantIcon : ClickableIcon
{
  public enum Type
  {
    Traveler,
    RsvTraveler,
    Bookseller,
  }

  private const float _rsvHueShift = -60f;
  private static readonly Vector2 _bundleNotifyIconOffset = new(27, 11);

  // Keep the merchant icon texture alive for the entirety of the game so we don't have to worry about disposing
  private static readonly Lazy<VariantTexture2D<Type>> _merchantIconTexture = new(() =>
  {
    var tex = ModEntry.GetSingleton<IModHelper>().ModContent.Load<Texture2D>("assets/merchant.png");
    VariantTexture2D<Type> variantTexture = new(tex);
    variantTexture.AddVariant(
      Type.RsvTraveler,
      t =>
      {
        var pixels = new Color[t.Width * t.Height];
        t.GetData(pixels);

        for (var i = 0; i < pixels.Length; i++)
        {
          if (pixels[i].A > 0)
          {
            pixels[i] = pixels[i].ShiftHue(_rsvHueShift);
          }
        }
        t.SetData(pixels);
      }
    );
    return variantTexture;
  });

  private bool _rsvLoaded;
  private readonly Type _merchantType;

  private readonly ShakingSprite _bundleIcon;
  private string _hoverText;
  private string _hoverTextWithBundleInfo = "";
  private string _bundleText = "";

  public MerchantIcon(Type merchantType)
    : base(Game1.mouseCursors, new Rectangle(192, 1411, 20, 20), 40)
  {
    _merchantType = merchantType;

    _bundleIcon = new ShakingSprite(
      Game1.mouseCursors,
      new Rectangle(403, 496, 5, 14),
      finalSize: 8,
      PrimaryDimension.Width
    );

    switch (_merchantType)
    {
      case Type.Traveler:
        BaseTexture = _merchantIconTexture.Value.BaseTexture;
        SetSourceBounds(new Rectangle(0, 0, 20, 20));
        break;
      case Type.RsvTraveler:
        BaseTexture = _merchantIconTexture.Value.GetTexture(Type.RsvTraveler);
        SetSourceBounds(new Rectangle(0, 0, 20, 20));
        break;
      case Type.Bookseller:
        BaseTexture = Game1.mouseCursors_1_6;
        SetSourceBounds(new Rectangle(5, 471, 23, 22));
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(merchantType));
    }
    _hoverText = DefaultHoverText();
  }

  public override string HoverText
  {
    get => !ShouldShowBundleIcon() ? _hoverText : _hoverTextWithBundleInfo;
    set => _hoverText = FormatHoverText(value);
  }

  private string DefaultHoverText()
  {
    return _merchantType switch
    {
      Type.Traveler => I18n.TravelingMerchantIsInTown(),
      Type.RsvTraveler => I18n.RsvTravelingMerchantIsAtHike(),
      Type.Bookseller => I18n.BooksellerIsInTown(),
      _ => _hoverText,
    };
  }

  public bool IsTraveler => _merchantType is Type.Traveler or Type.RsvTraveler;

  public bool ConfigShowIcon()
  {
    return _merchantType switch
    {
      Type.Traveler => Config.ShowTravelingMerchantIcon,
      Type.RsvTraveler => _rsvLoaded && Config.ShowRsvTravelingMerchantIcon,
      Type.Bookseller => Config.ShowBooksellerIcon,
      _ => true,
    };
  }

  private bool ShouldShowBundleIcon()
  {
    return IsTraveler && Config.ShowMerchantBundleIcon && !string.IsNullOrWhiteSpace(_bundleText);
  }

  public bool VisitedMerchant { get; set; }

  public bool MerchantInTown { get; set; }

  public void SetBundleItems(List<string> bundleItems)
  {
    _hoverText = DefaultHoverText();
    _bundleText = string.Join(", ", bundleItems);
    if (bundleItems.Count == 0)
    {
      return;
    }
    _hoverTextWithBundleInfo = FormatHoverText(
      $"{_hoverText}\n{I18n.BundlesInStock()}\n---\n{_bundleText}"
    );
  }

  public void ResetMerchantAvailability(bool isInTown)
  {
    _rsvLoaded = ModEntry
      .GetSingleton<IModHelper>()
      .ModRegistry.IsLoaded(ModCompat.RidgesideVillage);
    VisitedMerchant = false;
    MerchantInTown = isInTown;
  }

  protected override bool _ShouldDraw()
  {
    if (!ConfigShowIcon())
    {
      return false;
    }

    if (VisitedMerchant && Config.HideMerchantIconWhenVisited)
    {
      return false;
    }

    return MerchantInTown && base._ShouldDraw();
  }

  public override void Draw(SpriteBatch batch)
  {
    base.Draw(batch);

    if (!ShouldShowBundleIcon())
    {
      return;
    }
    _bundleIcon.Update();
    _bundleIcon.Draw(batch, IconPosition + _bundleNotifyIconOffset);
  }
}
