using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace UIInfoSuite2.Models.Icons;

internal class QueenOfSauceIcon()
  : ClickableIcon(Game1.mouseCursors, new Rectangle(609, 361, 28, 28), 40)
{
  private static readonly Rectangle _tvSourceRect = new(609, 361, 28, 28);

  private CraftingRecipe? _recipe;

  public CraftingRecipe? Recipe
  {
    get => _recipe;
    set
    {
      _recipe = value;
      if (_recipe is null)
      {
        return;
      }

      UpdateKnowsRecipeCheck();
      HoverText = I18n.TodaysRecipe() + _recipe.DisplayName;
    }
  }

  private bool KnowsRecipe { get; set; } = true;

  public void UpdateKnowsRecipeCheck()
  {
    bool knowsSinceLastCheck = Recipe is null || Game1.player.knowsRecipe(Recipe.name);
    if (KnowsRecipe == knowsSinceLastCheck)
    {
      return;
    }

    KnowsRecipe = knowsSinceLastCheck;
    ModEntry.DebugLog(
      $"Player {Game1.player.Name} recipe knowledge has changed. Knows Recipe: {knowsSinceLastCheck}"
    );
  }

  protected override bool _ShouldDraw()
  {
    bool recipeAvailable =
      (Game1.dayOfMonth % 7 == 0 || (Game1.dayOfMonth - 3) % 7 == 0)
      && Game1.stats.DaysPlayed > 5
      && Recipe is not null
      && !KnowsRecipe;
    return base._ShouldDraw() && recipeAvailable;
  }

  public override void Draw(SpriteBatch batch)
  {
    if (!ShouldDraw() || Recipe is null)
    {
      return;
    }

    if (!Config.ShowRecipeItemAsIcon)
    {
      Icon.draw(batch);
      return;
    }

    ParsedItemData itemData = Recipe.GetItemData(useFirst: true);
    batch.Draw(
      itemData.GetTexture(),
      new Vector2(IconPosition.X, IconPosition.Y),
      itemData.GetSourceRect(),
      Color.White,
      0f,
      Vector2.Zero,
      2.5f,
      SpriteEffects.None,
      1f
    );

    batch.Draw(
      Game1.mouseCursors,
      new Vector2(IconPosition.X + 18, IconPosition.Y + 18),
      _tvSourceRect,
      Color.White,
      0f,
      Vector2.Zero,
      0.8f,
      SpriteEffects.None,
      1f
    );
  }
}
