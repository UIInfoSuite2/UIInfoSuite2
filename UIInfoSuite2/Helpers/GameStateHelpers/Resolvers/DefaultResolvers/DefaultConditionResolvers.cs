using UIInfoSuite2.Extensions;

namespace UIInfoSuite2.Helpers.GameStateHelpers.Resolvers.DefaultResolvers;

internal static partial class DefaultConditionResolvers
{
  public static ConditionResolver UnsupportedConditionResolver(string conditionKey)
  {
    return new ConditionResolver(
      conditionKey,
      (joinedQueryString, _) => I18n.GSQ_Requirements_Unsupported().Format(joinedQueryString, conditionKey),
      true
    );
  }
}
