using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace RimuruSurvivor;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class RimuruPlugin : BasePlugin
{
    public const string PluginGuid = "ursoftware.vampiresurvivors.rimuru";
    public const string PluginName = "Rimuru Tempest";
    public const string PluginVersion = "0.3.0";

    public override void Load()
    {
        Log.LogInfo($"{PluginName} {PluginVersion} carregado.");
        Log.LogInfo(RimuruContent.RuntimeProbeMessage);
        Log.LogInfo($"Conteudo preparado: {RimuruContent.Character.DisplayName}, arma {RimuruContent.Weapon.DisplayName}, evolucoes {RimuruContent.Evolution.DisplayName} e {RimuruContent.ThirdEvolution.DisplayName}.");
        Log.LogInfo($"Passivas preparadas: {RimuruContent.GreatSage.DisplayName} -> {RimuruContent.Ciel.DisplayName}; regras de runtime carregadas para a futura integracao IL2CPP.");

        var diagnostics = new RimuruHarmonyDiagnostics(
            PluginGuid,
            message => Log.LogInfo(message),
            warning => Log.LogWarning(warning));
        diagnostics.Run();

        var harmony = new Harmony(PluginGuid);
        RimuruGameplayHooks.Install(
            harmony,
            message => Log.LogInfo(message),
            warning => Log.LogWarning(warning),
            includeAdaptiveHooks: false);
        RimuruGameplayBootstrap.Start(
            message => Log.LogInfo(message),
            warning => Log.LogWarning(warning));
    }
}
