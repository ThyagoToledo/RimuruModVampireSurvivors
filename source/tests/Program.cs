using System.Numerics;
using RimuruSurvivor;

if (RimuruPatchTargets.All.Count != 3 ||
    !RimuruPatchTargets.All.Any(target => target.MethodName == "HasPotentialEvolution") ||
    !RimuruPatchTargets.All.Any(target => target.TypeName == "LevelUpFactory") ||
    !RimuruPatchTargets.All.Any(target => target.TypeName == "SignalBus" && target.MethodName == "InternalFire"))
{
    throw new InvalidOperationException("Harmony patch probes are incomplete.");
}

var state = new RimuruRunState();
if (!state.TrySummonRanga(predatorLevel: 4))
{
    throw new InvalidOperationException("Ranga did not unlock from the Slime form.");
}

if (!state.TryUnlockHumanoid(characterLevel: 20))
{
    throw new InvalidOperationException("Humanoid form did not unlock.");
}

if (!state.TrySummonTempestCompanions(characterLevel: 35) || state.TempestCompanionCount != 2)
{
    throw new InvalidOperationException("Tempest companions did not unlock.");
}

if (!state.TryEvolveDemonLord(weaponLevel: 8, passiveLevel: 5, treasureOpened: true))
{
    throw new InvalidOperationException("Demon Lord evolution did not unlock.");
}

var analysis = state.AnalyzeRevival("fanged_bat", "sonic_bite");
if (!analysis.CielAwakened || !analysis.AbilityCopied || !state.IsImmuneTo("fanged_bat"))
{
    throw new InvalidOperationException("Revival analysis did not unlock immunity and Ciel.");
}

if (!state.TryEvolveAzathoth(beelzebuthLevel: 8, treasureOpened: true))
{
    throw new InvalidOperationException("Third weapon evolution did not unlock.");
}

if (!state.CanApplyReaperSeverance("red_reaper") || !state.CanExecuteDeath("red_reaper", 0.01f))
{
    throw new InvalidOperationException("Azathoth did not unlock the Death encounter rule.");
}

var target = GreatSageAimAssist.ChooseTarget(
    new[]
    {
        new TargetSnapshot("slow_elite", new Vector2(5, 0), Vector2.Zero, 0.8f, true, false),
        new TargetSnapshot("fast_common", new Vector2(3, 0), new Vector2(1, 0), 0.2f, false, false)
    },
    Vector2.Zero,
    maxDistance: 10);
if (target is null || target.EnemyFamilyId != "fast_common")
{
    throw new InvalidOperationException("Great Sage target selection did not prioritize the kill opportunity.");
}

var intercept = GreatSageAimAssist.PredictIntercept(target, Vector2.Zero, projectileSpeed: 10);
if (intercept.X <= target.Position.X)
{
    throw new InvalidOperationException("Great Sage did not lead the moving target.");
}

Console.WriteLine("Rimuru runtime rules: PASS");
