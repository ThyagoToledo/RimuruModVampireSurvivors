namespace RimuruSurvivor;

public static class RimuruContent
{
    public const string RuntimeProbeMessage = "Modo jogavel: o pacote CUSTOM fornece selecao e skins; o plugin IL2CPP controla formas, Ranga, habilidades e resistencias.";

    public static readonly CharacterDefinition Character = new(
        Id: "rimuru_tempest",
        DisplayName: "Rimuru Tempest",
        StartingWeaponId: "GARLIC",
        PassiveName: "Grande Sabio",
        PassiveSummary: "+20% Area, +15% Experiencia e -10% Recarga.");

    public static readonly WeaponDefinition Weapon = new(
        Id: "predator_katana",
        DisplayName: "Predador: Lamina de Rimuru",
        Summary: "Katana de energia azul que encadeia cortes e acumula analise contra o alvo.",
        MaxLevel: 8,
        EvolutionPassiveId: "great_sage");

    public static readonly WeaponDefinition Evolution = new(
        Id: "beelzebuth_demon_lord",
        DisplayName: "Rei Glutao Beelzebuth",
        Summary: "A forma Lorde Demonio abre cortes dimensionais, puxa inimigos e aplica dano continuo.",
        MaxLevel: 8,
        EvolutionPassiveId: "great_sage");

    public static readonly WeaponDefinition ThirdEvolution = new(
        Id: "azathoth_reaper_severance",
        DisplayName: "Azathoth, Void God",
        Summary: "Ciel guia cortes do vazio e abre uma janela real para derrotar a Morte.",
        MaxLevel: 8,
        EvolutionPassiveId: "ciel");

    public static readonly PassiveDefinition GreatSage = new(
        Id: "great_sage",
        DisplayName: "Grande Sabio",
        MaxLevel: 5,
        RevivalBehavior: "analyze_killer_family_and_grant_run_immunity");

    public static readonly PassiveDefinition Ciel = new(
        Id: "ciel",
        DisplayName: "Ciel",
        MaxLevel: 1,
        RevivalBehavior: "copy_ability_and_counterstrike_analyzed_target");
}

public sealed class CharacterDefinition
{
    public CharacterDefinition(string Id, string DisplayName, string StartingWeaponId, string PassiveName, string PassiveSummary)
    {
        this.Id = Id;
        this.DisplayName = DisplayName;
        this.StartingWeaponId = StartingWeaponId;
        this.PassiveName = PassiveName;
        this.PassiveSummary = PassiveSummary;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string StartingWeaponId { get; }
    public string PassiveName { get; }
    public string PassiveSummary { get; }
}

public sealed class WeaponDefinition
{
    public WeaponDefinition(string Id, string DisplayName, string Summary, int MaxLevel, string EvolutionPassiveId)
    {
        this.Id = Id;
        this.DisplayName = DisplayName;
        this.Summary = Summary;
        this.MaxLevel = MaxLevel;
        this.EvolutionPassiveId = EvolutionPassiveId;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Summary { get; }
    public int MaxLevel { get; }
    public string EvolutionPassiveId { get; }
}

public sealed class PassiveDefinition
{
    public PassiveDefinition(string Id, string DisplayName, int MaxLevel, string RevivalBehavior)
    {
        this.Id = Id;
        this.DisplayName = DisplayName;
        this.MaxLevel = MaxLevel;
        this.RevivalBehavior = RevivalBehavior;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public int MaxLevel { get; }
    public string RevivalBehavior { get; }
}
