namespace RimuruSurvivor;

public sealed class PatchTargetProbe
{
    public PatchTargetProbe(string Id, string TypeName, string MethodName, string Purpose)
    {
        this.Id = Id;
        this.TypeName = TypeName;
        this.MethodName = MethodName;
        this.Purpose = Purpose;
    }

    public string Id { get; }
    public string TypeName { get; }
    public string MethodName { get; }
    public string Purpose { get; }
}

public static class RimuruPatchTargets
{
    public static readonly IReadOnlyList<PatchTargetProbe> All = new PatchTargetProbe[]
    {
        new(
            "weapon-evolution-check",
            null,
            "HasPotentialEvolution",
            "Adicionar Predador, Beelzebuth e Azathoth a verificacao nativa de evolucao."),
        new(
            "level-up-factory",
            "LevelUpFactory",
            null,
            "Descobrir as assinaturas que montam escolhas e concedem armas ou passivas."),
        new(
            "signal-bus",
            "SignalBus",
            "InternalFire",
            "Observar sinais de nivel, morte, revivificacao, bau e mudanca de personagem.")
    };
}
