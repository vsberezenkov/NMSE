using System.Xml.Linq;

namespace NMSE.Extractor.Data;

public static class ProductLookup
{
    public static Dictionary<string, object?>? ParseProductElement(
        Dictionary<string, string> localisation,
        XElement item,
        bool includeRequirements,
        bool includeRawKeys,
        bool requireIcon,
        string fallbackId = "",
        string nameDefault = "",
        string groupDefault = "",
        string descriptionDefault = "")
    {
        string itemId = MxmlParser.GetPropertyValue(item, "ID", fallbackId);
        if (string.IsNullOrEmpty(itemId)) return null;

        string nameKey = MxmlParser.GetPropertyValue(item, "Name");
        string subtitleKey = MxmlParser.GetPropertyValue(item, "Subtitle");
        string descriptionKey = MxmlParser.GetPropertyValue(item, "Description");
        string nameLowerKey = MxmlParser.GetPropertyValue(item, "NameLower");
        string altDescriptionKey = MxmlParser.GetPropertyValue(item, "AltDescription");
        string hintKey = MxmlParser.GetPropertyValue(item, "Hint");

        if (MxmlParser.UnresolvedLocalisationKeyCount(localisation, nameKey, subtitleKey, descriptionKey) >= 2)
            return null;

        object baseValue = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "BaseValue", "0"));
        object stackMult = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "StackMultiplier", "1"));
        object level = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "Level", "0"));
        object chargeValue = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "ChargeValue", "0"));
        object defaultCraftAmount = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "DefaultCraftAmount", "1"));
        object craftAmountStepSize = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "CraftAmountStepSize", "1"));
        object craftAmountMultiplier = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "CraftAmountMultiplier", "1"));
        object recipeCost = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "RecipeCost", "0"));
        object cookingValue = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "CookingValue", "0"));
        object specificChargeOnly = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "SpecificChargeOnly", "false"));
        object normalizedValueOnWorld = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "NormalisedValueOnWorld", "0"));
        object normalizedValueOffWorld = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "NormalisedValueOffWorld", "0"));
        object economyInfluenceMultiplier = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "EconomyInfluenceMultiplier", "0"));

        var requiredItems = new List<Dictionary<string, object>>();
        if (includeRequirements)
        {
            var reqProp = item.Descendants("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Requirements");
            if (reqProp != null)
            {
                foreach (var reqElem in reqProp.Elements("Property"))
                {
                    string reqId = MxmlParser.GetPropertyValue(reqElem, "ID");
                    if (!string.IsNullOrEmpty(reqId))
                    {
                        requiredItems.Add(new Dictionary<string, object>
                        {
                            ["Id"] = reqId,
                            ["Quantity"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(reqElem, "Amount", "1"))
                        });
                    }
                }
            }
        }

        bool isCraftableBool = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "IsCraftable", "false")) is true;
        bool isCookingBool = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "CookingIngredient", "false")) is true;
        bool eggModifierBool = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "EggModifierIngredient", "false")) is true;
        bool goodForSellingBool = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "GoodForSelling", "false")) is true;

        var usages = new List<string>();
        if (isCraftableBool) usages.Add("HasUsedToCraft");
        if (isCookingBool) usages.Add("HasCookingProperties");
        if (eggModifierBool) usages.Add("IsEggIngredient");
        if (goodForSellingBool) usages.Add("HasDevProperties");

        string rarity = MxmlParser.GetNestedEnum(item, "Rarity", "Rarity");
        string legality = MxmlParser.GetNestedEnum(item, "Legality", "Legality");
        string tradeCategory = MxmlParser.GetNestedEnum(item, "TradeCategory", "TradeCategory");
        string productCategory = MxmlParser.GetNestedEnum(item, "Type", "ProductCategory");
        string substanceCategory = MxmlParser.GetNestedEnum(item, "Category", "SubstanceCategory");

        // Icon
        var iconProp = item.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Icon");
        string iconFilename = iconProp != null ? MxmlParser.GetPropertyValue(iconProp, "Filename") : "";
        string iconPath = !string.IsNullOrEmpty(iconFilename) ? MxmlParser.NormalizeGameIconPath(iconFilename) : "";

        var heroIconProp = item.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "HeroIcon");
        string heroIconFilename = heroIconProp != null ? MxmlParser.GetPropertyValue(heroIconProp, "Filename") : "";
        string heroIconPath = !string.IsNullOrEmpty(heroIconFilename) ? MxmlParser.NormalizeGameIconPath(heroIconFilename) : "";

        if (requireIcon && string.IsNullOrEmpty(iconPath)) return null;

        // Colour
        var colourElem = item.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Colour");
        string colour = MxmlParser.ParseColour(colourElem);
        var worldColourElem = item.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "WorldColour");
        string? worldColour = worldColourElem != null ? MxmlParser.ParseColour(worldColourElem) : null;

        // Cost/PriceModifiers
        var costProp = item.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Cost");
        Dictionary<string, object>? priceModifiers = null;
        if (costProp != null)
        {
            priceModifiers = new Dictionary<string, object>
            {
                ["SpaceStationMarkup"] = AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "SpaceStationMarkup", "0"))),
                ["LowPriceMod"] = AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "LowPriceMod", "0"))),
                ["HighPriceMod"] = AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "HighPriceMod", "0"))),
                ["BuyBaseMarkup"] = AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "BuyBaseMarkup", "0"))),
                ["BuyMarkupMod"] = AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "BuyMarkupMod", "0"))),
            };
        }

        bool isProceduralBool = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "Procedural", "false")) is true;

        var row = new Dictionary<string, object?>
        {
            ["Id"] = itemId,
            ["IconPath"] = iconPath,
            ["Name"] = MxmlParser.Translate(nameKey, nameDefault),
            ["Name_LocStr"] = NullIfEmpty(nameKey),
            ["NameLower"] = !string.IsNullOrEmpty(nameLowerKey) ? MxmlParser.Translate(nameLowerKey, nameLowerKey) : null,
            ["NameLower_LocStr"] = NullIfEmpty(nameLowerKey),
            ["Group"] = MxmlParser.Translate(subtitleKey, groupDefault),
            ["Subtitle_LocStr"] = NullIfEmpty(subtitleKey),
            ["Description"] = MxmlParser.Translate(descriptionKey, descriptionDefault),
            ["Description_LocStr"] = NullIfEmpty(descriptionKey),
            ["AltDescription"] = !string.IsNullOrEmpty(altDescriptionKey) ? MxmlParser.Translate(altDescriptionKey, altDescriptionKey) : null,
            ["Hint"] = !string.IsNullOrEmpty(hintKey) ? MxmlParser.Translate(hintKey, hintKey) : null,
            ["BaseValueUnits"] = baseValue,
            ["Level"] = level,
            ["ChargeValue"] = chargeValue,
            ["MaxStackSize"] = stackMult,
            ["DefaultCraftAmount"] = defaultCraftAmount,
            ["CraftAmountStepSize"] = craftAmountStepSize,
            ["CraftAmountMultiplier"] = craftAmountMultiplier,
            ["Colour"] = colour,
            ["WorldColour"] = worldColour,
            ["Usages"] = usages,
            ["BlueprintCost"] = recipeCost,
            ["CookingValue"] = cookingValue,
            ["RequiredItems"] = requiredItems,
            ["HeroIconPath"] = !string.IsNullOrEmpty(heroIconPath) ? heroIconPath : null,
            ["PriceModifiers"] = priceModifiers,
            ["SpecificChargeOnly"] = specificChargeOnly,
            ["NormalisedValueOnWorld"] = AsDouble(normalizedValueOnWorld),
            ["NormalisedValueOffWorld"] = AsDouble(normalizedValueOffWorld),
            ["EconomyInfluenceMultiplier"] = AsDouble(economyInfluenceMultiplier),
            ["Rarity"] = !string.IsNullOrEmpty(rarity) ? rarity : null,
            ["Legality"] = !string.IsNullOrEmpty(legality) ? legality : null,
            ["TradeCategory"] = !string.IsNullOrEmpty(tradeCategory) ? tradeCategory : null,
            ["ProductCategory"] = !string.IsNullOrEmpty(productCategory) ? productCategory : null,
            ["SubstanceCategory"] = !string.IsNullOrEmpty(substanceCategory) ? substanceCategory : null,
            ["IsCraftable"] = isCraftableBool,
            ["Procedural"] = isProceduralBool,
            ["WikiCategory"] = NullIfEmpty(MxmlParser.GetPropertyValue(item, "WikiCategory")),
            ["WikiEnabled"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "WikiEnabled", "false")),
            ["FossilCategory"] = NullIfEmpty(MxmlParser.GetPropertyValue(item, "FossilCategory")),
            ["CorvettePartCategory"] = NullIfEmpty(MxmlParser.GetNestedEnum(item, "CorvettePartCategory", "CorvettePartCategory")),
            ["CorvetteRewardFrequency"] = AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "CorvetteRewardFrequency", "0"))),
            ["Consumable"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "Consumable", "false")),
            ["CookingIngredient"] = isCookingBool,
            ["GoodForSelling"] = goodForSellingBool,
            ["EggModifierIngredient"] = eggModifierBool,
            ["DeploysInto"] = NullIfEmpty(MxmlParser.GetPropertyValue(item, "DeploysInto")),
            ["BuildableShipTechID"] = NullIfEmpty(MxmlParser.GetPropertyValue(item, "BuildableShipTechID")),
            ["GroupID"] = NullIfEmpty(MxmlParser.GetPropertyValue(item, "GroupID")),
            ["PinObjective"] = NullIfEmpty(MxmlParser.GetPropertyValue(item, "PinObjective")),
            ["PinObjectiveTip"] = NullIfEmpty(MxmlParser.GetPropertyValue(item, "PinObjectiveTip")),
            ["PinObjectiveMessage"] = NullIfEmpty(MxmlParser.GetPropertyValue(item, "PinObjectiveMessage")),
            ["PinObjectiveScannableType"] = NullIfEmpty(MxmlParser.GetNestedEnum(item, "PinObjectiveScannableType", "ScanIconType")),
            ["PinObjectiveEasyToRefine"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "PinObjectiveEasyToRefine", "false")),
            ["NeverPinnable"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "NeverPinnable", "false")),
            ["GiveRewardOnSpecialPurchase"] = NullIfEmpty(MxmlParser.GetPropertyValue(item, "GiveRewardOnSpecialPurchase")),
            ["FoodBonusStat"] = NullIfEmpty(MxmlParser.GetNestedEnum(item, "FoodBonusStat", "ConsumableBonusStat")),
            ["FoodBonusStatAmount"] = AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "FoodBonusStatAmount", "0"))),
            ["IsTechbox"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "IsTechbox", "false")),
            ["CanSendToOtherPlayers"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(item, "CanSendToOtherPlayers", "true")),
        };

        if (includeRawKeys)
        {
            row["SubtitleKey"] = subtitleKey;
            row["NameKey"] = nameKey;
            row["DescriptionKey"] = descriptionKey;
        }

        return row;
    }

    /// <summary>
    /// Ensures a parsed value is always a double. Used for fields that are
    /// floating-point in the game data (e.g., NormalisedValue, PriceModifiers)
    /// even when their MXML value is "0" (which ParseValue returns as int).
    /// </summary>
    internal static double AsDouble(object val) => val is int i ? (double)i : (double)val;

    private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

    public static Dictionary<string, Dictionary<string, object?>> LoadProductLookup(
        Dictionary<string, string> localisation,
        string productsMxmlPath,
        bool includeRequirements = true,
        bool includeRawKeys = false)
    {
        var lookup = new Dictionary<string, Dictionary<string, object?>>();
        if (!File.Exists(productsMxmlPath)) return lookup;

        var root = MxmlParser.LoadXml(productsMxmlPath);
        var tableProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null) return lookup;

        foreach (var item in tableProp.Elements("Property")
            .Where(e => e.Attribute("name")?.Value == "Table"))
        {
            var row = ParseProductElement(localisation, item, includeRequirements, includeRawKeys,
                requireIcon: false, nameDefault: MxmlParser.GetPropertyValue(item, "ID"));
            if (row == null) continue;
            string id = row["Id"]?.ToString() ?? "";
            if (!string.IsNullOrEmpty(id))
                lookup[id] = row;
        }
        return lookup;
    }
}
