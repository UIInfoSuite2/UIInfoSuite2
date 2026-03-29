using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using UIInfoSuite2.Layout.Measurement;
using UIInfoSuite2.Models.Sprite;

namespace UIInfoSuite2.Models.Icons;

internal class MerchantIcon : ClickableIcon
{
  private static readonly Vector2 _bundleNotifyIconOffset = new(27, 11);

  // Keep the merchant icon texture alive for the entirety of the game so we don't have to worry about disposing
  private static readonly Lazy<Texture2D> _merchantIconTexture = new(() =>
    ModEntry.GetSingleton<IModHelper>().ModContent.Load<Texture2D>("assets/merchant.png")
  );

  public enum Type
  {
    Traveler,
    Bookseller,
  }

  private readonly Type _merchantType;

  private bool _merchantInTown;
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
        _hoverText = I18n.TravelingMerchantIsInTown();
        BaseTexture.Value = _merchantIconTexture.Value;
        SetSourceBounds(new Rectangle(0, 0, 20, 20));
        break;
      case Type.Bookseller:
        _hoverText = I18n.BooksellerIsInTown();
        BaseTexture.Value = Game1.mouseCursors_1_6;
        SetSourceBounds(new Rectangle(5, 471, 23, 22));
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(merchantType));
    }

    UpdateMerchantIcon();
  }

  public override string HoverText
  {
    get
    {
      if (
        _merchantType != Type.Traveler
        || !Config.ShowMerchantBundleItems
        || string.IsNullOrWhiteSpace(_bundleText)
      )
      {
        return _hoverText;
      }
      return _hoverTextWithBundleInfo;
    }
    set => _hoverText = FormatHoverText(value);
  }

  private string DefaultHoverText()
  {
    return _merchantType switch
    {
      Type.Traveler => I18n.TravelingMerchantIsInTown(),
      Type.Bookseller => I18n.BooksellerIsInTown(),
      _ => _hoverText,
    };
  }

  public bool VisitedMerchant { get; set; }

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

  public void UpdateMerchantIcon()
  {
    switch (_merchantType)
    {
      case Type.Traveler:
        _merchantInTown = (
          (Forest)Game1.getLocationFromName(nameof(Forest))
        ).ShouldTravelingMerchantVisitToday();
        break;
      case Type.Bookseller:
        // Town location has the bookseller bool private, so we'll just do it like they do it.
        // No need to reflect if we don't have to.
        _merchantInTown = Utility.getDaysOfBooksellerThisSeason().Contains(Game1.dayOfMonth);
        break;
    }

    VisitedMerchant = false;
  }

  protected override bool _ShouldDraw()
  {
    if (
      (_merchantType == Type.Bookseller && !Config.ShowBooksellerIcon)
      || (_merchantType == Type.Traveler && !Config.ShowTravelingMerchantIcon)
    )
    {
      return false;
    }

    if (VisitedMerchant && Config.HideMerchantIconWhenVisited)
    {
      return false;
    }

    return _merchantInTown && base._ShouldDraw();
  }

  public override void Draw(SpriteBatch batch)
  {
    base.Draw(batch);

    if (
      _merchantType != Type.Traveler
      || !Config.ShowMerchantBundleIcon
      || string.IsNullOrWhiteSpace(_bundleText)
    )
    {
      return;
    }
    _bundleIcon.Update();
    _bundleIcon.Draw(batch, IconPosition + _bundleNotifyIconOffset);
  }
}
