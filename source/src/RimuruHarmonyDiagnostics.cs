using System.Reflection;
using HarmonyLib;

namespace RimuruSurvivor;

internal sealed class RimuruHarmonyDiagnostics
{
    private static Action<string> _log;
    private readonly Harmony _harmony;
    private readonly Action<string> _info;
    private readonly Action<string> _warning;

    public RimuruHarmonyDiagnostics(string harmonyId, Action<string> info, Action<string> warning)
    {
        _harmony = new Harmony(harmonyId);
        _info = info;
        _warning = warning;
    }

    public void Run()
    {
        _log = _info;
        RunHarmonySmokeTest();
        ProbeGameTargets();
    }

    private void RunHarmonySmokeTest()
    {
        var original = AccessTools.Method(typeof(HarmonySmokeTarget), nameof(HarmonySmokeTarget.Ping));
        var prefix = AccessTools.Method(typeof(RimuruHarmonyDiagnostics), nameof(SmokePrefix));

        if (original is null || prefix is null)
        {
            _warning("Harmony smoke test: metodos de teste nao encontrados.");
            return;
        }

        _harmony.Patch(original, prefix: new HarmonyMethod(prefix));
        HarmonySmokeTarget.Ping();
    }

    private void ProbeGameTargets()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .ToArray();

        foreach (var target in RimuruPatchTargets.All)
        {
            var matches = types
                .Where(type => target.TypeName is null || type.Name.Equals(target.TypeName, StringComparison.Ordinal))
                .SelectMany(type => GetMethods(type)
                    .Where(method => target.MethodName is null || method.Name.Equals(target.MethodName, StringComparison.Ordinal)))
                .Take(20)
                .ToArray();

            if (matches.Length == 0)
            {
                _warning($"Harmony probe [{target.Id}]: alvo nao encontrado. {target.Purpose}");
                continue;
            }

            _info($"Harmony probe [{target.Id}]: {matches.Length} candidato(s) encontrado(s).");
            foreach (var method in matches)
            {
                _info($"Harmony target: {FormatMethod(method)}");
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static IEnumerable<MethodInfo> GetMethods(Type type)
    {
        try
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        catch
        {
            return Array.Empty<MethodInfo>();
        }
    }

    private static string FormatMethod(MethodInfo method)
    {
        var parameters = string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name));
        return $"{method.DeclaringType?.FullName}.{method.Name}({parameters}) -> {method.ReturnType.Name}";
    }

    private static bool SmokePrefix()
    {
        _log?.Invoke("Harmony smoke test: PASS.");
        return true;
    }

    private static class HarmonySmokeTarget
    {
        public static void Ping()
        {
        }
    }
}
