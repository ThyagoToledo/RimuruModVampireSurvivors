using System.Numerics;

namespace RimuruSurvivor;

public enum RimuruForm
{
    Slime,
    Humanoid,
    DemonLord
}

public sealed class RimuruRunState
{
    private static readonly HashSet<string> ReaperFamilies = new(StringComparer.OrdinalIgnoreCase)
    {
        "red_reaper",
        "death"
    };

    private readonly HashSet<string> immuneEnemyFamilies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CopiedEnemyAbility> copiedAbilities = new(StringComparer.OrdinalIgnoreCase);

    public RimuruForm Form { get; private set; } = RimuruForm.Slime;
    public bool IsDemonLord => Form == RimuruForm.DemonLord;
    public bool IsCiel { get; private set; }
    public bool RangaSummoned { get; private set; }
    public int TempestCompanionCount { get; private set; }
    public bool HasAzathoth { get; private set; }
    public int RevivalsAnalyzed { get; private set; }
    public IReadOnlySet<string> ImmuneEnemyFamilies => immuneEnemyFamilies;
    public IReadOnlyDictionary<string, CopiedEnemyAbility> CopiedAbilities => copiedAbilities;

    public void InitializeForm(RimuruForm form)
    {
        Form = form;
    }

    public bool TryUnlockHumanoid(int characterLevel)
    {
        if (Form != RimuruForm.Slime || characterLevel < 20)
        {
            return false;
        }

        Form = RimuruForm.Humanoid;
        return true;
    }

    public bool TrySummonRanga(int predatorLevel)
    {
        if (RangaSummoned || predatorLevel < 4)
        {
            return false;
        }

        RangaSummoned = true;
        return true;
    }

    public bool TrySummonTempestCompanions(int characterLevel)
    {
        if (Form == RimuruForm.Slime || TempestCompanionCount > 0 || characterLevel < 35)
        {
            return false;
        }

        TempestCompanionCount = 2;
        return true;
    }

    public bool TryEvolveDemonLord(int weaponLevel, int passiveLevel, bool treasureOpened)
    {
        if (IsDemonLord || Form == RimuruForm.Slime || weaponLevel < 8 || passiveLevel < 5 || !treasureOpened)
        {
            return false;
        }

        Form = RimuruForm.DemonLord;
        return true;
    }

    public bool TryEvolveDemonLordStable(int characterLevel, int weaponLevel, int passiveLevel)
    {
        if (characterLevel < 40)
        {
            return false;
        }

        return TryEvolveDemonLord(weaponLevel, passiveLevel, treasureOpened: true);
    }

    public bool TryAwakenCielFromCombatAnalysis(int characterLevel, int beelzebuthLevel)
    {
        if (!IsDemonLord || IsCiel || characterLevel < 55 || beelzebuthLevel < 6)
        {
            return false;
        }

        IsCiel = true;
        return true;
    }

    public bool TryEvolveAzathoth(int beelzebuthLevel, bool treasureOpened)
    {
        if (!IsDemonLord || !IsCiel || !RangaSummoned || HasAzathoth || beelzebuthLevel < 8 || !treasureOpened)
        {
            return false;
        }

        HasAzathoth = true;
        return true;
    }

    public bool TryEvolveAzathothStable(int characterLevel, int beelzebuthLevel)
    {
        if (characterLevel < 60)
        {
            return false;
        }

        return TryEvolveAzathoth(beelzebuthLevel, treasureOpened: true);
    }

    public RevivalAnalysisResult AnalyzeRevival(string enemyFamilyId, string enemyAbilityId)
    {
        if (string.IsNullOrWhiteSpace(enemyFamilyId))
        {
            throw new ArgumentException("Enemy family id is required.", nameof(enemyFamilyId));
        }

        if (string.IsNullOrWhiteSpace(enemyAbilityId))
        {
            throw new ArgumentException("Enemy ability id is required.", nameof(enemyAbilityId));
        }

        RevivalsAnalyzed++;
        var firstAnalysis = immuneEnemyFamilies.Add(enemyFamilyId);
        if (IsDemonLord)
        {
            IsCiel = true;
            copiedAbilities.TryAdd(
                enemyFamilyId,
                new CopiedEnemyAbility(enemyFamilyId, enemyAbilityId, CanExecute: true));
        }

        return new RevivalAnalysisResult(
            EnemyFamilyId: enemyFamilyId,
            IsNewEnemyAnalysis: firstAnalysis,
            ImmunityGranted: true,
            CielAwakened: IsCiel,
            AbilityCopied: IsCiel);
    }

    public bool IsImmuneTo(string enemyFamilyId) => immuneEnemyFamilies.Contains(enemyFamilyId);

    public bool CanCounterstrike(string enemyFamilyId) => IsCiel && copiedAbilities.ContainsKey(enemyFamilyId);

    public bool CanApplyReaperSeverance(string enemyFamilyId) =>
        HasAzathoth && ReaperFamilies.Contains(enemyFamilyId);

    public bool CanExecuteDeath(string enemyFamilyId, float healthRatio) =>
        CanApplyReaperSeverance(enemyFamilyId) && healthRatio is > 0 and <= 0.01f;
}

public sealed class RevivalAnalysisResult
{
    public RevivalAnalysisResult(string EnemyFamilyId, bool IsNewEnemyAnalysis, bool ImmunityGranted, bool CielAwakened, bool AbilityCopied)
    {
        this.EnemyFamilyId = EnemyFamilyId;
        this.IsNewEnemyAnalysis = IsNewEnemyAnalysis;
        this.ImmunityGranted = ImmunityGranted;
        this.CielAwakened = CielAwakened;
        this.AbilityCopied = AbilityCopied;
    }

    public string EnemyFamilyId { get; }
    public bool IsNewEnemyAnalysis { get; }
    public bool ImmunityGranted { get; }
    public bool CielAwakened { get; }
    public bool AbilityCopied { get; }
}

public sealed class CopiedEnemyAbility
{
    public CopiedEnemyAbility(string EnemyFamilyId, string AbilityId, bool CanExecute)
    {
        this.EnemyFamilyId = EnemyFamilyId;
        this.AbilityId = AbilityId;
        this.CanExecute = CanExecute;
    }

    public string EnemyFamilyId { get; }
    public string AbilityId { get; }
    public bool CanExecute { get; }
}

public sealed class TargetSnapshot
{
    public TargetSnapshot(string EnemyFamilyId, Vector2 Position, Vector2 Velocity, float HealthRatio, bool IsThreat, bool IsBoss)
    {
        this.EnemyFamilyId = EnemyFamilyId;
        this.Position = Position;
        this.Velocity = Velocity;
        this.HealthRatio = HealthRatio;
        this.IsThreat = IsThreat;
        this.IsBoss = IsBoss;
    }

    public string EnemyFamilyId { get; }
    public Vector2 Position { get; }
    public Vector2 Velocity { get; }
    public float HealthRatio { get; }
    public bool IsThreat { get; }
    public bool IsBoss { get; }
}

public static class GreatSageAimAssist
{
    public static TargetSnapshot ChooseTarget(
        IEnumerable<TargetSnapshot> targets,
        Vector2 origin,
        float maxDistance,
        bool includeBosses = false)
    {
        return targets
            .Where(target => (includeBosses || !target.IsBoss) && target.HealthRatio > 0)
            .Select(target => (target, distance: Vector2.Distance(origin, target.Position)))
            .Where(candidate => candidate.distance <= maxDistance)
            .OrderBy(candidate => candidate.target.HealthRatio)
            .ThenByDescending(candidate => candidate.target.IsThreat)
            .ThenBy(candidate => candidate.distance)
            .Select(candidate => candidate.target)
            .FirstOrDefault();
    }

    public static Vector2 PredictIntercept(TargetSnapshot target, Vector2 origin, float projectileSpeed)
    {
        if (projectileSpeed <= 0)
        {
            return target.Position;
        }

        var distance = Vector2.Distance(origin, target.Position);
        var lead = Math.Clamp(distance / projectileSpeed, 0f, 0.45f);
        return target.Position + target.Velocity * lead;
    }
}
