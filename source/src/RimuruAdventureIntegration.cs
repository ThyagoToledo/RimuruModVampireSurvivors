using HarmonyLib;
using VampireSurvivors.App.Data.Adventures;
using VampireSurvivors.App.Scripts.Framework.Adventures;
using VampireSurvivors.Data;
using VampireSurvivors.Data.Characters;
using VampireSurvivors.UI;

namespace RimuruSurvivor;

internal static class RimuruAdventureIntegration
{
    private static CharacterType? _rimuruType;
    private static Action<string> _info = _ => { };
    private static Action<string> _warning = _ => { };

    public static void Install(Harmony harmony, Action<string> info, Action<string> warning)
    {
        _info = info;
        _warning = warning;

        TryPatch(
            harmony,
            AccessTools.Method(typeof(DataManager), nameof(DataManager.GenerateAdventureSpecificData)),
            nameof(GenerateAdventureSpecificDataPrefix),
            nameof(GenerateAdventureSpecificDataPostfix),
            "lista de personagens das Adventures");
        TryPatch(
            harmony,
            AccessTools.Method(typeof(AdventureManager), nameof(AdventureManager.IsAdventureCharacter)),
            null,
            nameof(IsAdventureCharacterPostfix),
            "filtro de personagens das Adventures");
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterItemUI), nameof(CharacterItemUI.IsCharUnlockable)),
            null,
            nameof(IsRimuruUnlockablePostfix),
            "desbloqueio visual do Rimuru");
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterItem), nameof(CharacterItem.IsCharacterBought)),
            null,
            nameof(IsRimuruCharacterItemUnlockablePostfix),
            "estado comprado do Rimuru");
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterItem), nameof(CharacterItem.IsCharacterUnlocked)),
            null,
            nameof(IsRimuruCharacterItemUnlockablePostfix),
            "estado desbloqueado do Rimuru");
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterItemUI), nameof(CharacterItemUI.IsCharAvailable)),
            null,
            nameof(IsRimuruUnlockablePostfix),
            "disponibilidade do Rimuru");
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterItemUI), nameof(CharacterItemUI.IsUnlockable)),
            null,
            nameof(IsRimuruUnlockablePostfix),
            "elegibilidade de selecao do Rimuru");
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterItemUI), nameof(CharacterItemUI.UpdateVisualState)),
            null,
            nameof(RimuruVisualStatePostfix),
            "estado disponivel do card do Rimuru");
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterSelectInfoPanel), nameof(CharacterSelectInfoPanel.SetUnlockableDescription)),
            nameof(RimuruUnlockDescriptionPrefix),
            null,
            "descricao de desbloqueio do Rimuru");
    }

    private static void TryPatch(
        Harmony harmony,
        System.Reflection.MethodInfo original,
        string prefixName,
        string postfixName,
        string purpose)
    {
        if (original is null)
        {
            _warning($"Harmony Adventures: alvo nao encontrado para {purpose}.");
            return;
        }

        try
        {
            var prefix = prefixName is null
                ? null
                : new HarmonyMethod(AccessTools.Method(typeof(RimuruAdventureIntegration), prefixName));
            var postfix = postfixName is null
                ? null
                : new HarmonyMethod(AccessTools.Method(typeof(RimuruAdventureIntegration), postfixName));
            harmony.Patch(original, prefix, postfix);
            _info($"Harmony Adventures: {purpose} conectado em {original.Name}.");
        }
        catch (Exception exception)
        {
            _warning($"Harmony Adventures: falha em {purpose}: {exception.Message}");
        }
    }

    private static void GenerateAdventureSpecificDataPrefix(DataManager __instance, AdventureData adventureData)
    {
        try
        {
            if (!TryResolveRimuru(__instance, out var rimuruType) || adventureData is null)
            {
                return;
            }

            var rimuruLevels = __instance._characterData[rimuruType];
            var characterTypes = adventureData.CharacterTypes;
            if (characterTypes is not null &&
                RimuruAdventureRules.ShouldAppend(characterTypes.Contains(rimuruType), rimuruLevels[0].charName, rimuruLevels[0].surname))
            {
                characterTypes.Add(rimuruType);
                _info($"Rimuru adicionado a Adventure {adventureData.ProgressKey} antes da geracao dos dados.");
            }
        }
        catch (Exception exception)
        {
            _warning($"Harmony Adventures: falha ao preparar o Rimuru: {exception.Message}");
        }
    }

    private static void GenerateAdventureSpecificDataPostfix(DataManager __instance, AdventureData adventureData)
    {
        try
        {
            if (!TryResolveRimuru(__instance, out var rimuruType))
            {
                return;
            }

            var rimuruLevels = __instance._characterData[rimuruType];
            foreach (var level in rimuruLevels)
            {
                level.hidden = false;
                level.alwaysHidden = false;
                level.price = 0f;
            }

            var adventureCharacters = __instance.AdventureCharacterData;
            if (adventureCharacters is not null && !adventureCharacters.ContainsKey(rimuruType))
            {
                adventureCharacters.Add(rimuruType, rimuruLevels);
                _info($"Rimuru registrado nos dados convertidos da Adventure {adventureData?.ProgressKey ?? "desconhecida"}.");
            }
        }
        catch (Exception exception)
        {
            _warning($"Harmony Adventures: falha ao finalizar o registro do Rimuru: {exception.Message}");
        }
    }

    private static void IsAdventureCharacterPostfix(CharacterType characterType, ref bool __result)
    {
        if (_rimuruType.HasValue && characterType.Equals(_rimuruType.Value))
        {
            __result = true;
        }
    }

    private static void IsRimuruUnlockablePostfix(CharacterItemUI __instance, ref bool __result)
    {
        if (IsRimuruCard(__instance))
        {
            __result = true;
        }
    }

    private static void IsRimuruCharacterItemUnlockablePostfix(CharacterItem __instance, ref bool __result)
    {
        if (IsRimuruItem(__instance))
        {
            __result = true;
        }
    }

    private static void RimuruVisualStatePostfix(CharacterItemUI __instance)
    {
        if (IsRimuruCard(__instance))
        {
            __instance.SetVisualStateAvailable();
        }
    }

    private static bool RimuruUnlockDescriptionPrefix(
        CharacterSelectInfoPanel __instance,
        CharacterItemUI selectedCharacterItemUi)
    {
        if (!IsRimuruCard(selectedCharacterItemUi))
        {
            return true;
        }

        _info("Rimuru: descricao de requisito ignorada; personagem liberado.");
        __instance.SetVisualStateAvailable();
        return false;
    }

    private static bool IsRimuruCard(CharacterItemUI item)
    {
        try
        {
            return item is not null && item.CharacterName.Contains("Rimuru", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsRimuruItem(CharacterItem item)
    {
        try
        {
            return item is not null &&
                   item.CharacterData is not null &&
                   item.CharacterData.charName.Contains("Rimuru", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryResolveRimuru(DataManager dataManager, out CharacterType rimuruType)
    {
        rimuruType = default;
        if (dataManager?._characterData is null)
        {
            return false;
        }

        if (_rimuruType.HasValue && dataManager._characterData.TryGetValue(_rimuruType.Value, out var cachedLevels))
        {
            rimuruType = _rimuruType.Value;
            return cachedLevels is not null && cachedLevels.Count > 0;
        }

        foreach (var entry in dataManager._characterData)
        {
            var levels = entry.Value;
            if (levels is null || levels.Count == 0)
            {
                continue;
            }

            var baseLevel = levels[0];
            if (!RimuruAdventureRules.IsRimuru(baseLevel.charName, baseLevel.surname))
            {
                continue;
            }

            _rimuruType = entry.Key;
            rimuruType = entry.Key;
            _info($"Rimuru CUSTOM identificado como CharacterType {(int)rimuruType}.");
            return true;
        }

        return false;
    }
}
