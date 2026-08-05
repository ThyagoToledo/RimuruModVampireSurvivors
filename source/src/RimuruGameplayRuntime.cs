using BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;
using VampireSurvivors.Data;
using VampireSurvivors.Data.Characters;
using VampireSurvivors.Framework;
using VampireSurvivors.Objects.Characters;
using VampireSurvivors.Objects.Items;
using VampireSurvivors.Objects.Weapons;
using VampireSurvivors.UI;
using UObject = UnityEngine.Object;

namespace RimuruSurvivor;

internal static class RimuruGameplayBootstrap
{
    public static void Start(Action<string> info, Action<string> warning)
    {
        try
        {
            RimuruRuntimeBehaviour.Initialize(info, warning);
            info("Runtime jogavel iniciado: formas, Ranga, armas e resistencias ativos.");
        }
        catch (Exception exception)
        {
            warning($"Falha ao iniciar o runtime jogavel: {exception}");
        }
    }
}

internal static class RimuruGameplayHooks
{
    public static void Install(Harmony harmony, Action<string> info, Action<string> warning, bool includeAdaptiveHooks)
    {
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterController), nameof(CharacterController.OnUpdate), Type.EmptyTypes),
            null,
            nameof(PlayerUpdatePostfix),
            "ciclo das habilidades e Ranga",
            info,
            warning);
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterController), nameof(CharacterController.HandleLateUpdate), Type.EmptyTypes),
            null,
            nameof(PlayerLateUpdatePostfix),
            "animacao das formas",
            info,
            warning);
        TryPatch(
            harmony,
            AccessTools.Method(
                typeof(CharacterSelectInfoPanel),
                nameof(CharacterSelectInfoPanel.SetWeaponIconSprite),
                new[] { typeof(DataManager), typeof(CharacterData), typeof(Skin) }),
            nameof(SelectionWeaponIconSkinPrefix),
            null,
            "icone de arma por skin na selecao",
            info,
            warning);
        TryPatch(
            harmony,
            AccessTools.Method(
                typeof(CharacterSelectInfoPanel),
                nameof(CharacterSelectInfoPanel.SetWeaponIconSprite),
                new[] { typeof(DataManager), typeof(CharacterData) }),
            nameof(SelectionWeaponIconDefaultPrefix),
            null,
            "icone de arma padrao na selecao",
            info,
            warning);
        TryPatch(
            harmony,
            AccessTools.Method(
                typeof(GameEquipmentPanelItem),
                nameof(GameEquipmentPanelItem.Initialize),
                new[] { typeof(CharacterController), typeof(VampireSurvivors.Data.Weapons.WeaponData), typeof(WeaponType) }),
            null,
            nameof(EquipmentIconPostfix),
            "icone de arma durante a partida",
            info,
            warning);
        if (!includeAdaptiveHooks)
        {
            info("Harmony gameplay: ganchos adaptativos mantidos desativados neste teste de compatibilidade.");
            return;
        }

        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterController), nameof(CharacterController.GetDamaged), new[] { typeof(float) }),
            nameof(DamagePrefix),
            null,
            "resistencia adaptativa",
            info,
            warning);
        TryPatch(
            harmony,
            AccessTools.Method(
                typeof(CharacterController),
                nameof(CharacterController.GetDamaged),
                new[] { typeof(float), typeof(HitVfxType), typeof(float), typeof(WeaponType), typeof(bool) }),
            nameof(DamagePrefix),
            null,
            "resistencia por tipo de dano",
            info,
            warning);
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterController), nameof(CharacterController.OnDeath)),
            nameof(DeathPrefix),
            null,
            "analise do agressor",
            info,
            warning);
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterController), nameof(CharacterController.Revive), new[] { typeof(float), typeof(bool) }),
            null,
            nameof(RevivePostfix),
            "Grande Sabio e Ciel apos reviver",
            info,
            warning);
        TryPatch(
            harmony,
            AccessTools.Method(typeof(CharacterController), nameof(CharacterController.OnTreasureCollected), new[] { typeof(TreasureChest) }),
            null,
            nameof(TreasurePostfix),
            "evolucao por bau",
            info,
            warning);
    }

    private static void TryPatch(
        Harmony harmony,
        System.Reflection.MethodInfo original,
        string prefixName,
        string postfixName,
        string purpose,
        Action<string> info,
        Action<string> warning)
    {
        if (original is null)
        {
            warning($"Harmony gameplay: alvo nao encontrado para {purpose}.");
            return;
        }

        try
        {
            var prefix = prefixName is null ? null : new HarmonyMethod(AccessTools.Method(typeof(RimuruGameplayHooks), prefixName));
            var postfix = postfixName is null ? null : new HarmonyMethod(AccessTools.Method(typeof(RimuruGameplayHooks), postfixName));
            harmony.Patch(original, prefix, postfix);
            info($"Harmony gameplay: {purpose} conectado em {original.Name}.");
        }
        catch (Exception exception)
        {
            warning($"Harmony gameplay: falha em {purpose}: {exception.Message}");
        }
    }

    private static void DamagePrefix(CharacterController __instance, ref float __0)
    {
        __0 = RimuruRuntimeBehaviour.Instance?.ModifyIncomingDamage(__instance, __0) ?? __0;
    }

    private static void PlayerUpdatePostfix(CharacterController __instance)
    {
        RimuruRuntimeBehaviour.Instance?.Tick(__instance);
    }

    private static void PlayerLateUpdatePostfix(CharacterController __instance)
    {
        RimuruRuntimeBehaviour.Instance?.LateTick(__instance);
    }

    private static void DeathPrefix(CharacterController __instance)
    {
        RimuruRuntimeBehaviour.Instance?.CaptureFatalThreat(__instance);
    }

    private static void RevivePostfix(CharacterController __instance)
    {
        RimuruRuntimeBehaviour.Instance?.AnalyzeRevival(__instance);
    }

    private static void TreasurePostfix(CharacterController __instance)
    {
        RimuruRuntimeBehaviour.Instance?.RegisterTreasure(__instance);
    }

    private static bool SelectionWeaponIconSkinPrefix(
        CharacterSelectInfoPanel __instance,
        CharacterData __1,
        Skin __2)
    {
        return !(RimuruRuntimeBehaviour.Instance?.TrySetSelectionWeaponIcon(__instance, __1, __2) ?? false);
    }

    private static bool SelectionWeaponIconDefaultPrefix(CharacterSelectInfoPanel __instance, CharacterData __1)
    {
        return !(RimuruRuntimeBehaviour.Instance?.TrySetSelectionWeaponIcon(__instance, __1, null) ?? false);
    }

    private static void EquipmentIconPostfix(
        GameEquipmentPanelItem __instance,
        CharacterController __0,
        WeaponType __2)
    {
        RimuruRuntimeBehaviour.Instance?.TrySetEquipmentWeaponIcon(__instance, __0, __2);
    }
}

internal sealed class RimuruRuntimeBehaviour
{
    private const float EnemyRefreshInterval = 0.14f;
    private readonly Dictionary<int, RimuruPlayerRuntime> _players = new();
    private readonly List<EnemyController> _enemies = new();
    private readonly List<RimuruEffect> _effects = new();
    private readonly Dictionary<string, Sprite> _sprites = new(StringComparer.OrdinalIgnoreCase);
    private Action<string> _info = _ => { };
    private Action<string> _warning = _ => { };
    private float _nextEnemyRefresh;
    private int _lastEffectsFrame = -1;
    private bool _loggedFirstControllerTick;
    private string _assetRoot = string.Empty;
    private string _customRoot = string.Empty;

    public static RimuruRuntimeBehaviour Instance { get; private set; }

    private RimuruRuntimeBehaviour(Action<string> info, Action<string> warning)
    {
        _info = info;
        _warning = warning;
        _assetRoot = ResolveAssetRoot();
        _customRoot = ResolveCustomRoot();
        LoadAssets();
        _info($"Assets visuais carregados: {_sprites.Count}; plugin {_assetRoot}; CUSTOM {_customRoot}.");
    }

    public static void Initialize(Action<string> info, Action<string> warning)
    {
        Instance = new RimuruRuntimeBehaviour(info, warning);
    }

    public void Tick(CharacterController controller)
    {
        if (!_loggedFirstControllerTick)
        {
            _loggedFirstControllerTick = true;
            _info("Runtime recebeu o primeiro ciclo de CharacterController.");
        }

        if (!IsRimuru(controller))
        {
            return;
        }

        var now = Time.time;
        if (now >= _nextEnemyRefresh)
        {
            RefreshEnemies();
            _nextEnemyRefresh = now + EnemyRefreshInterval;
        }

        var player = EnsurePlayer(controller);
        var deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
        if (!IsUsable(player.Controller))
        {
            player.Dispose();
            _players.Remove(player.Id);
            return;
        }

        UpdateProgression(player);
        UpdateAbilities(player, deltaTime);
        UpdateRanga(player, deltaTime);
        if (_lastEffectsFrame != Time.frameCount)
        {
            UpdateEffects(deltaTime);
            _lastEffectsFrame = Time.frameCount;
        }
    }

    public void LateTick(CharacterController controller)
    {
        if (IsRimuru(controller) && _players.TryGetValue(controller.GetInstanceID(), out var player))
        {
            player.UpdateCharacterAnimation(_sprites, Time.time, _info);
        }
    }

    [HideFromIl2Cpp]
    public float ModifyIncomingDamage(CharacterController controller, float damage)
    {
        if (!TryGetPlayer(controller, out var player) || damage <= 0)
        {
            return damage;
        }

        var threat = FindNearestEnemy(controller.transform.position, 2.75f);
        if (threat is not null)
        {
            player.LastThreat = threat;
            var family = EnemyFamily(threat);
            if (player.State.IsImmuneTo(family))
            {
                SpawnPulse(controller.transform.position, GetSprite("predator-core"), new Color(0.3f, 0.95f, 1f, 0.85f), 1.4f, 0f, 0.28f);
                return 0f;
            }
        }

        var multiplier = player.Form switch
        {
            RimuruForm.Slime => 0.68f,
            RimuruForm.Humanoid => 0.78f,
            RimuruForm.DemonLord => player.State.IsCiel ? 0.38f : 0.50f,
            _ => 1f
        };
        return Mathf.Max(0.5f, damage * multiplier);
    }

    [HideFromIl2Cpp]
    public void CaptureFatalThreat(CharacterController controller)
    {
        if (!TryGetPlayer(controller, out var player))
        {
            return;
        }

        player.LastThreat = FindNearestEnemy(controller.transform.position, 8f) ?? player.LastThreat;
    }

    [HideFromIl2Cpp]
    public void AnalyzeRevival(CharacterController controller)
    {
        if (!TryGetPlayer(controller, out var player))
        {
            return;
        }

        var threat = player.LastThreat ?? FindNearestEnemy(controller.transform.position, 12f);
        var family = threat is null ? "unknown_threat" : EnemyFamily(threat);
        var ability = threat is null ? "adaptive_resistance" : $"copy_{family}";
        var result = player.State.AnalyzeRevival(family, ability);

        controller.SetInvulForMilliSecondsNonCumulativeIncludeParma(player.Form == RimuruForm.DemonLord ? 8000f : 3500f);
        controller.SetHealthToMax();
        SpawnPulse(controller.transform.position, GetSprite("predator-core"), new Color(0.25f, 0.9f, 1f, 0.95f), 3.2f, 60f, 0.7f);

        if (result.CielAwakened)
        {
            player.CielCounterReady = true;
            if (threat is not null && IsUsable(threat))
            {
                threat.Kill(WeaponType.NIGHTSWORD2);
            }
            _info($"Ciel despertou para o jogador {player.Id}; imunidade concedida contra {family}.");
        }
        else
        {
            _info($"Grande Sabio analisou {family}; resistencia permanente ativa nesta partida.");
        }
    }

    [HideFromIl2Cpp]
    public void RegisterTreasure(CharacterController controller)
    {
        if (TryGetPlayer(controller, out var player))
        {
            player.TreasuresOpened++;
        }
    }

    [HideFromIl2Cpp]
    public bool TrySetSelectionWeaponIcon(CharacterSelectInfoPanel panel, CharacterData character, Skin skin)
    {
        try
        {
            if (panel is null || character is null ||
                !character.charName.Contains("Rimuru", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var suffix = skin?.suffix ?? string.Empty;
            var spriteId = suffix.Contains("Demon", StringComparison.OrdinalIgnoreCase)
                ? "beelzebuth-blade"
                : suffix.Contains("Humanoid", StringComparison.OrdinalIgnoreCase)
                    ? "rimuru-katana-v2"
                    : "predator-core";
            return RimuruVisuals.SetImageSprite(panel._weaponIcon, GetSprite(spriteId));
        }
        catch
        {
            return false;
        }
    }

    [HideFromIl2Cpp]
    public bool TrySetEquipmentWeaponIcon(
        GameEquipmentPanelItem item,
        CharacterController owner,
        WeaponType weaponType)
    {
        if (!IsRimuru(owner))
        {
            return false;
        }

        var spriteId = weaponType switch
        {
            WeaponType.GARLIC => "predator-core",
            WeaponType.NIGHTSWORD => "rimuru-katana-v2",
            WeaponType.NIGHTSWORD2 => "beelzebuth-blade",
            _ => null
        };
        return spriteId is not null && RimuruVisuals.SetImageSprite(item?._icon, GetSprite(spriteId));
    }

    [HideFromIl2Cpp]
    private RimuruPlayerRuntime EnsurePlayer(CharacterController controller)
    {
        var id = controller.GetInstanceID();
        if (_players.TryGetValue(id, out var existing))
        {
            return existing;
        }

        PruneDestroyedRuntimeObjects();

        var initialForm = DetectInitialForm(controller);
        var player = new RimuruPlayerRuntime(controller, initialForm);
        _players[id] = player;
        if (initialForm != RimuruForm.Slime)
        {
            player.State.TrySummonRanga(8);
            SwapWeapon(
                controller,
                WeaponType.GARLIC,
                initialForm == RimuruForm.DemonLord ? WeaponType.NIGHTSWORD2 : WeaponType.NIGHTSWORD);
            EnsureRangaCount(player, initialForm == RimuruForm.DemonLord ? 3 : 1);
        }
        _info($"Rimuru detectado em partida: forma inicial {initialForm}, nivel {controller.Level}.");
        return player;
    }

    [HideFromIl2Cpp]
    private void PruneDestroyedRuntimeObjects()
    {
        foreach (var stale in _players.Values.Where(player => !IsUsable(player.Controller)).ToArray())
        {
            stale.Dispose();
            _players.Remove(stale.Id);
        }

        for (var index = _effects.Count - 1; index >= 0; index--)
        {
            if (!RimuruVisuals.IsUsable(_effects[index].Renderer))
            {
                _effects.RemoveAt(index);
            }
        }
    }

    [HideFromIl2Cpp]
    private void RefreshEnemies()
    {
        _enemies.Clear();
        foreach (var enemy in UObject.FindObjectsOfType<EnemyController>())
        {
            if (IsUsable(enemy) && enemy.Hp > 0)
            {
                _enemies.Add(enemy);
            }
        }
    }

    [HideFromIl2Cpp]
    private void UpdateProgression(RimuruPlayerRuntime player)
    {
        var controller = player.Controller;
        if (player.Form == RimuruForm.Slime)
        {
            var predatorRank = Math.Max(GetWeaponRank(controller, WeaponType.GARLIC), Math.Min(8, 1 + controller.Level / 3));
            player.AbilityRank = predatorRank;
            if (player.State.TrySummonRanga(predatorRank))
            {
                EnsureRangaCount(player, 1);
                SpawnPulse(controller.transform.position, GetSprite("predator-core"), new Color(0.3f, 0.45f, 1f, 0.9f), 2.2f, 0f, 0.6f);
                _info("Predador nivel 4: Ranga respondeu ao chamado de Rimuru.");
            }
            if (controller.Level >= 20 && predatorRank >= 8 && player.State.TryUnlockHumanoid(controller.Level))
            {
                player.ResetAbilityTimers();
                SwapWeapon(controller, WeaponType.GARLIC, WeaponType.NIGHTSWORD);
                player.BeginTransformation(RimuruForm.Slime, Time.time);
                SpawnTransformation(player, new Color(0.25f, 0.85f, 1f, 1f));
                _info("Rimuru evoluiu de Slime para a forma Humanoide.");
            }
        }
        else if (player.Form == RimuruForm.Humanoid)
        {
            var evolvedSwordRank = GetWeaponRank(controller, WeaponType.NIGHTSWORD2);
            var nativeEvolutionReady = evolvedSwordRank > 0;
            var swordRank = nativeEvolutionReady
                ? 8
                : Math.Max(GetWeaponRank(controller, WeaponType.NIGHTSWORD), Math.Min(8, 1 + Math.Max(0, controller.Level - 20) / 3));
            var sageRank = Math.Max(GetAccessoryRank(controller, WeaponType.COOLDOWN), Math.Min(5, 1 + Math.Max(0, controller.Level - 20) / 5));
            if (nativeEvolutionReady)
            {
                sageRank = 5;
            }
            player.AbilityRank = swordRank;
            if (player.State.TryEvolveDemonLordStable(nativeEvolutionReady ? 40 : controller.Level, swordRank, sageRank))
            {
                player.ResetAbilityTimers();
                if (!nativeEvolutionReady)
                {
                    SwapWeapon(controller, WeaponType.NIGHTSWORD, WeaponType.NIGHTSWORD2);
                }
                EnsureRangaCount(player, 3);
                controller.SetHealthToMax();
                controller.SetInvulForMilliSecondsNonCumulativeIncludeParma(5000f);
                player.BeginTransformation(RimuruForm.Humanoid, Time.time);
                SpawnTransformation(player, new Color(0.65f, 0.08f, 0.9f, 1f));
                _info("Rimuru despertou como Lorde Demonio; Beelzebuth e a Barreira Multicamadas estao ativos.");
            }
        }
        else
        {
            player.AbilityRank = Math.Max(GetWeaponRank(controller, WeaponType.NIGHTSWORD2), Math.Min(8, 1 + Math.Max(0, controller.Level - 40) / 4));
            if (player.State.TryAwakenCielFromCombatAnalysis(controller.Level, player.AbilityRank))
            {
                player.CielCounterReady = true;
                player.EvolutionLockUntil = Time.time + 1.25f;
                SpawnTransformation(player, new Color(0.2f, 0.95f, 1f, 1f));
                _info("A analise continua do Grande Sabio despertou Ciel.");
            }
            if (!player.HasAzathoth && Time.time >= player.EvolutionLockUntil &&
                player.State.TryEvolveAzathothStable(controller.Level, player.AbilityRank))
            {
                player.HasAzathoth = true;
                player.BeginWeaponEvolution(Time.time);
                SpawnTransformation(player, new Color(0.9f, 0.05f, 0.45f, 1f));
                _info("Azathoth, Deus do Vazio, despertou. O protocolo contra a Morte esta ativo.");
            }
        }
    }

    [HideFromIl2Cpp]
    private void UpdateAbilities(RimuruPlayerRuntime player, float deltaTime)
    {
        player.PrimaryTimer -= deltaTime;
        player.SecondaryTimer -= deltaTime;
        player.RegenTimer -= deltaTime;

        if (player.RegenTimer <= 0)
        {
            var heal = player.Form switch
            {
                RimuruForm.Slime => 1.8f + player.AbilityRank * 0.35f,
                RimuruForm.Humanoid => 1.2f + player.AbilityRank * 0.25f,
                RimuruForm.DemonLord => 3.5f + player.AbilityRank * 0.6f,
                _ => 1f
            };
            player.Controller.RecoverHp(heal, showRecovery: false);
            player.RegenTimer = player.Form == RimuruForm.DemonLord ? 0.65f : 1.1f;
        }

        if (player.PrimaryTimer <= 0)
        {
            FirePrimary(player);
        }

        if (player.SecondaryTimer <= 0)
        {
            FireSecondary(player);
        }
    }

    [HideFromIl2Cpp]
    private void FirePrimary(RimuruPlayerRuntime player)
    {
        var origin = player.Controller.transform.position;
        var target = FindBestTarget(origin, 18f);
        var might = Mathf.Max(1f, player.Controller.PPowerFinal());
        var rank = Math.Max(1, player.AbilityRank);

        if (player.Form == RimuruForm.Slime)
        {
            player.PlayAttackAnimation(Time.time, 0.34f);
            SpawnPulse(origin, GetSprite("predator-core"), new Color(0.2f, 0.9f, 1f, 0.9f), 2.1f + rank * 0.11f, (18f + rank * 5f) * might, 0.55f);
            player.PrimaryTimer = Mathf.Max(1.05f, 2.35f - rank * 0.12f);
            return;
        }

        var direction = target is null
            ? new Vector3(1f, 0f, 0f)
            : PredictDirection(origin, target, player.Form == RimuruForm.DemonLord ? 14f : 11f);
        player.PlayAttackAnimation(Time.time, player.Form == RimuruForm.DemonLord ? 0.28f : 0.34f);
        var sprite = GetSprite(player.HasAzathoth ? "azathoth-void-blade" : player.Form == RimuruForm.DemonLord ? "beelzebuth-blade" : "rimuru-katana-v2");
        var count = player.Form == RimuruForm.DemonLord ? 2 + rank / 3 : 1 + rank / 5;
        for (var i = 0; i < count; i++)
        {
            var spread = (i - (count - 1) * 0.5f) * 8f;
            SpawnBlade(
                player,
                origin,
                Quaternion.Euler(0, 0, spread) * direction,
                sprite,
                (player.Form == RimuruForm.DemonLord ? 38f : 25f) * might + rank * 4f,
                player.Form == RimuruForm.DemonLord ? 15f : 12f,
                player.Form == RimuruForm.DemonLord ? 6 : 3 + rank / 3);
        }
        player.PrimaryTimer = player.Form == RimuruForm.DemonLord
            ? Mathf.Max(0.32f, 0.76f - rank * 0.045f)
            : Mathf.Max(0.55f, 1.25f - rank * 0.065f);
    }

    [HideFromIl2Cpp]
    private void FireSecondary(RimuruPlayerRuntime player)
    {
        var origin = player.Controller.transform.position;
        var might = Mathf.Max(1f, player.Controller.PPowerFinal());
        var rank = Math.Max(1, player.AbilityRank);
        var target = FindBestTarget(origin, 22f);

        if (player.Form == RimuruForm.Slime)
        {
            var direction = target is null ? Vector3.right : PredictDirection(origin, target, 9f);
            player.PlayAttackAnimation(Time.time, 0.42f);
            SpawnBlade(player, origin, direction, GetSprite("predator-core"), (14f + rank * 3f) * might, 9f, 2 + rank / 4, waterBlade: true);
            player.SecondaryTimer = Mathf.Max(2.2f, 4.4f - rank * 0.2f);
            return;
        }

        if (player.Form == RimuruForm.Humanoid)
        {
            player.PlayAttackAnimation(Time.time, 0.38f);
            SpawnBlackLightning(origin, (26f + rank * 6f) * might, 3 + rank / 2);
            if (target is not null)
            {
                SpawnVortex(target.transform.position, GetSprite("predator-core"), (20f + rank * 4f) * might, 2.4f, 1.1f, false);
            }
            player.SecondaryTimer = Mathf.Max(2.4f, 5.2f - rank * 0.25f);
            return;
        }

        var vortexPosition = target?.transform.position ?? origin;
        player.PlayAttackAnimation(Time.time, 0.48f);
        SpawnVortex(vortexPosition, GetSprite(player.HasAzathoth ? "azathoth-void-blade" : "beelzebuth-blade"), (42f + rank * 8f) * might, 4.2f, 2.2f, player.HasAzathoth);
        if (player.State.IsCiel || player.CielCounterReady)
        {
            SpawnBlackLightning(origin, (55f + rank * 8f) * might, 6 + rank);
            player.CielCounterReady = false;
        }
        player.SecondaryTimer = player.HasAzathoth ? 2.6f : 3.8f;
    }

    [HideFromIl2Cpp]
    private void SpawnBlade(
        RimuruPlayerRuntime owner,
        Vector3 origin,
        Vector3 direction,
        Sprite sprite,
        float damage,
        float speed,
        int pierce,
        bool waterBlade = false)
    {
        if (sprite is null)
        {
            return;
        }

        var effect = CreateEffect("Rimuru Blade", sprite, origin, waterBlade ? new Color(0.2f, 0.9f, 1f, 0.95f) : Color.white);
        effect.Kind = RimuruEffectKind.Blade;
        effect.Owner = owner;
        effect.Velocity = direction.normalized * speed;
        effect.Damage = damage;
        effect.Lifetime = 1.8f;
        effect.Pierce = pierce;
        effect.Node.transform.localScale = Vector3.one * (waterBlade ? 0.65f : 0.82f);
        effect.Node.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 45f);
    }

    [HideFromIl2Cpp]
    private void SpawnPulse(Vector3 position, Sprite sprite, Color color, float radius, float damage, float lifetime, float delay = 0f)
    {
        if (sprite is null)
        {
            return;
        }

        var effect = CreateEffect("Predator Pulse", sprite, position, color);
        effect.Kind = RimuruEffectKind.Pulse;
        effect.Damage = damage;
        effect.Radius = radius;
        effect.Lifetime = lifetime;
        effect.Delay = delay;
        effect.Node.transform.localScale = Vector3.one * 0.25f;
    }

    [HideFromIl2Cpp]
    private void SpawnVortex(Vector3 position, Sprite sprite, float damage, float radius, float lifetime, bool reaperProtocol)
    {
        if (sprite is null)
        {
            return;
        }

        var effect = CreateEffect("Beelzebuth Vortex", sprite, position, new Color(0.9f, 0.25f, 1f, 0.92f));
        effect.Kind = RimuruEffectKind.Vortex;
        effect.Damage = damage;
        effect.Radius = radius;
        effect.Lifetime = lifetime;
        effect.ReaperProtocol = reaperProtocol;
        effect.Node.transform.localScale = Vector3.one * 0.6f;
    }

    [HideFromIl2Cpp]
    private void SpawnBlackLightning(Vector3 origin, float damage, int chains)
    {
        var current = origin;
        var used = new HashSet<int>();
        for (var i = 0; i < chains; i++)
        {
            var target = _enemies
                .Where(IsUsable)
                .Where(enemy => !used.Contains(enemy.GetInstanceID()))
                .OrderBy(enemy => Vector3.Distance(current, enemy.transform.position))
                .FirstOrDefault(enemy => Vector3.Distance(current, enemy.transform.position) <= 7f);
            if (target is null)
            {
                break;
            }

            used.Add(target.GetInstanceID());
            target.GetDamaged(damage, HitVfxType.Default, 0.2f, WeaponType.FIREBALL, hasKb: false);
            var midpoint = (current + target.transform.position) * 0.5f;
            SpawnPulse(midpoint, GetSprite("predator-core"), new Color(0.25f, 0.15f, 1f, 0.8f), 0.65f, 0f, 0.22f);
            current = target.transform.position;
        }
    }

    [HideFromIl2Cpp]
    private RimuruEffect CreateEffect(string name, Sprite sprite, Vector3 position, Color color)
    {
        var node = new GameObject(name);
        node.transform.position = position;
        var renderer = node.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 220;
        RimuruVisuals.Configure(renderer);
        var effect = new RimuruEffect(node, renderer);
        _effects.Add(effect);
        return effect;
    }

    [HideFromIl2Cpp]
    private void UpdateEffects(float deltaTime)
    {
        for (var index = _effects.Count - 1; index >= 0; index--)
        {
            var effect = _effects[index];
            if (!RimuruVisuals.IsUsable(effect.Renderer) || !RimuruVisuals.IsUsable(effect.Node))
            {
                _effects.RemoveAt(index);
                continue;
            }

            if (effect.Delay > 0f)
            {
                effect.Delay -= deltaTime;
                effect.Renderer.enabled = false;
                continue;
            }
            effect.Renderer.enabled = true;
            effect.Age += deltaTime;
            if (effect.Age >= effect.Lifetime)
            {
                effect.Dispose();
                _effects.RemoveAt(index);
                continue;
            }

            var progress = Mathf.Clamp01(effect.Age / effect.Lifetime);
            if (effect.Kind == RimuruEffectKind.Blade)
            {
                effect.Node.transform.position += effect.Velocity * deltaTime;
                effect.Node.transform.Rotate(0, 0, 240f * deltaTime);
                foreach (var enemy in _enemies)
                {
                    if (!IsUsable(enemy) || effect.HitIds.Contains(enemy.GetInstanceID()) ||
                        Vector3.Distance(effect.Node.transform.position, enemy.transform.position) > 0.85f)
                    {
                        continue;
                    }

                    effect.HitIds.Add(enemy.GetInstanceID());
                    enemy.GetDamaged(effect.Damage, HitVfxType.Default, 0.6f, WeaponType.NIGHTSWORD, hasKb: true);
                    if (effect.HitIds.Count >= effect.Pierce)
                    {
                        effect.Age = effect.Lifetime;
                        break;
                    }
                }
            }
            else if (effect.Kind == RimuruEffectKind.Pulse)
            {
                effect.Node.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, effect.Radius, progress);
                DamageInRadius(effect, effect.Node.transform.position, effect.Radius * progress, pull: false);
            }
            else
            {
                effect.Node.transform.Rotate(0, 0, 170f * deltaTime);
                effect.Node.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, effect.Radius * 0.72f, Mathf.Sin(progress * Mathf.PI));
                effect.PulseTimer -= deltaTime;
                if (effect.PulseTimer <= 0)
                {
                    DamageInRadius(effect, effect.Node.transform.position, effect.Radius, pull: true);
                    effect.PulseTimer = 0.34f;
                    effect.HitIds.Clear();
                }
            }

            var color = effect.Renderer.color;
            color.a = Mathf.Clamp01(1f - progress * progress);
            effect.Renderer.color = color;
        }
    }

    [HideFromIl2Cpp]
    private void DamageInRadius(RimuruEffect effect, Vector3 center, float radius, bool pull)
    {
        foreach (var enemy in _enemies)
        {
            if (!IsUsable(enemy))
            {
                continue;
            }

            var distance = Vector3.Distance(center, enemy.transform.position);
            if (distance > radius)
            {
                continue;
            }

            if (pull && distance > 0.2f && !enemy.IsBoss)
            {
                enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, center, 0.42f);
            }

            if (!effect.HitIds.Add(enemy.GetInstanceID()) || effect.Damage <= 0)
            {
                continue;
            }

            if (effect.ReaperProtocol && IsReaper(enemy))
            {
                var severance = Mathf.Max(effect.Damage, enemy._maxHp * 0.03f);
                enemy.GetDamaged(severance, HitVfxType.Default, 0f, WeaponType.NIGHTSWORD2, hasKb: false);
                if (enemy.NormalizedHp <= 0.01f)
                {
                    enemy.Kill(WeaponType.NIGHTSWORD2);
                }
            }
            else
            {
                enemy.GetDamaged(effect.Damage, HitVfxType.Default, pull ? 0.1f : 0.7f, WeaponType.NIGHTSWORD, hasKb: !pull);
            }
        }
    }

    [HideFromIl2Cpp]
    private void UpdateRanga(RimuruPlayerRuntime player, float deltaTime)
    {
        var characterRenderer = (Renderer)player.FormRenderer ?? player.Controller._CharacterRenderer;
        for (var index = 0; index < player.Rangas.Count; index++)
        {
            var ranga = player.Rangas[index];
            ranga.Renderer.enabled = true;
            ranga.Renderer.color = Color.white;
            if (characterRenderer is not null)
            {
                ranga.Renderer.sortingLayerID = characterRenderer.sortingLayerID;
                ranga.Renderer.sortingOrder = characterRenderer.sortingOrder + 8 + index;
            }
            ranga.FrameTimer += deltaTime * 9f;
            if (ranga.Frames.Length > 0)
            {
                ranga.Renderer.sprite = ranga.Frames[(int)ranga.FrameTimer % ranga.Frames.Length];
                RimuruVisuals.SyncTexture(ranga.Renderer);
            }

            ranga.Cooldown -= deltaTime;
            if (ranga.Target is null || !IsUsable(ranga.Target))
            {
                ranga.Target = null;
                if (ranga.Cooldown <= 0)
                {
                    ranga.Target = FindBestTarget(player.Controller.transform.position, 8f);
                    ranga.HasHit = false;
                }
            }

            if (ranga.Target is not null)
            {
                var targetPosition = ranga.Target.transform.position;
                if (Vector3.Distance(targetPosition, player.Controller.transform.position) > 7f ||
                    Vector3.Distance(ranga.Node.transform.position, player.Controller.transform.position) > 7f)
                {
                    ranga.Target = null;
                    ranga.Cooldown = 0.35f;
                    continue;
                }
                ranga.Node.transform.position = Vector3.MoveTowards(ranga.Node.transform.position, targetPosition, deltaTime * (10f + player.AbilityRank * 0.45f));
                ranga.Renderer.flipX = targetPosition.x < ranga.Node.transform.position.x;
                if (!ranga.HasHit && Vector3.Distance(ranga.Node.transform.position, targetPosition) <= 0.8f)
                {
                    ranga.Target.GetDamaged((22f + player.AbilityRank * 7f) * Mathf.Max(1f, player.Controller.PPowerFinal()), HitVfxType.Default, 1.1f, WeaponType.SUMMONNIGHT, hasKb: true);
                    SpawnPulse(targetPosition, GetSprite("predator-core"), new Color(0.3f, 0.35f, 1f, 0.8f), 0.9f, 0f, 0.24f);
                    ranga.HasHit = true;
                    ranga.Target = null;
                    ranga.Cooldown = Mathf.Max(2.3f, 5.8f - player.AbilityRank * 0.32f);
                }
            }
            else
            {
                ranga.OrbitAngle += deltaTime * (1.6f + index * 0.15f);
                var offset = new Vector3(Mathf.Cos(ranga.OrbitAngle), Mathf.Sin(ranga.OrbitAngle) * 0.55f, 0f) * (1.4f + index * 0.55f);
                ranga.Node.transform.position = Vector3.Lerp(ranga.Node.transform.position, player.Controller.transform.position + offset, deltaTime * 6f);
                ranga.Renderer.flipX = Mathf.Cos(ranga.OrbitAngle) < 0;
            }
        }
    }

    [HideFromIl2Cpp]
    private void EnsureRangaCount(RimuruPlayerRuntime player, int count)
    {
        var frames = Enumerable.Range(1, 4).Select(index => GetSprite($"ranga-{index:00}")).Where(sprite => sprite is not null).Cast<Sprite>().ToArray();
        if (frames.Length == 0)
        {
            _warning("Ranga nao foi criado porque nenhum frame foi carregado.");
            return;
        }

        while (player.Rangas.Count < count && frames.Length > 0)
        {
            var node = new GameObject($"Ranga Tempest {player.Rangas.Count + 1}");
            node.transform.position = player.Controller.transform.position;
            var renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.enabled = true;
            renderer.color = Color.white;
            RimuruVisuals.Configure(renderer);
            var characterRenderer = (Renderer)player.FormRenderer ?? player.Controller._CharacterRenderer;
            if (RimuruVisuals.IsUsable(characterRenderer))
            {
                try
                {
                    renderer.sortingLayerID = characterRenderer.sortingLayerID;
                    renderer.sortingOrder = characterRenderer.sortingOrder + 8 + player.Rangas.Count;
                }
                catch
                {
                    renderer.sortingOrder = 238 + player.Rangas.Count;
                }
            }
            else
            {
                renderer.sortingOrder = 238 + player.Rangas.Count;
            }
            node.transform.localScale = Vector3.one * (player.Rangas.Count == 0 ? 1.45f : 1.15f);
            player.Rangas.Add(new RangaAvatar(node, renderer, frames, player.Rangas.Count * 2.1f));
            _info($"Ranga criado para o jogador {player.Id}: {player.Rangas.Count}/{count}, {frames.Length} frames.");
        }
    }

    [HideFromIl2Cpp]
    private void SpawnTransformation(RimuruPlayerRuntime player, Color color)
    {
        var sprite = GetSprite(player.HasAzathoth ? "azathoth-void-blade" : "predator-core") ?? GetSprite("beelzebuth-blade");
        SpawnPulse(player.Controller.transform.position, sprite, color, 2.5f, 0f, 0.55f);
        SpawnPulse(player.Controller.transform.position, sprite, color, 4.2f, 0f, 0.72f, 0.18f);
        SpawnPulse(player.Controller.transform.position, sprite, color, 5.4f, 70f + player.Controller.Level * 2f, 0.9f, 0.36f);
    }

    [HideFromIl2Cpp]
    private void SwapWeapon(CharacterController controller, WeaponType oldType, WeaponType newType)
    {
        try
        {
            var facade = GM.Core?.WeaponsFacade;
            if (facade is not null)
            {
                if (controller.WeaponsManager?.GetWeaponByTypeFromAnyCollection(oldType) is not null)
                {
                    facade.RemoveWeapon(oldType, controller, notifyRemove: true);
                }
                if (controller.WeaponsManager?.GetWeaponByTypeFromAnyCollection(newType) is null)
                {
                    facade.AddWeapon(newType, controller, removeFromStore: false, skipFire: false);
                }
                _info($"Arma proxy evoluida: {oldType} -> {newType}.");
                return;
            }
        }
        catch (Exception exception)
        {
            _warning($"Troca nativa {oldType} -> {newType} falhou; usando concessao compatível: {exception.Message}");
        }

        EnsureWeapon(controller, newType);
    }

    [HideFromIl2Cpp]
    private void EnsureWeapon(CharacterController controller, WeaponType type)
    {
        try
        {
            if (controller.WeaponsManager?.GetWeaponByTypeFromAnyCollection(type) is null)
            {
                controller.ApplyWeaponLevelUp(type);
            }
        }
        catch (Exception exception)
        {
            _warning($"Nao foi possivel conceder a arma proxy {type}: {exception.Message}");
        }
    }

    [HideFromIl2Cpp]
    private static int GetWeaponRank(CharacterController controller, WeaponType type)
    {
        try
        {
            return controller.WeaponsManager?.GetWeaponByTypeFromAnyCollection(type)?.Level ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    [HideFromIl2Cpp]
    private static int GetAccessoryRank(CharacterController controller, WeaponType type)
    {
        try
        {
            return controller.AccessoriesManager?.GetAccessoryByType(type, searchHidden: true)?.Level ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    [HideFromIl2Cpp]
    private EnemyController FindBestTarget(Vector3 origin, float maxDistance)
    {
        return _enemies
            .Where(IsUsable)
            .Select(enemy => new { Enemy = enemy, Distance = Vector3.Distance(origin, enemy.transform.position) })
            .Where(candidate => candidate.Distance <= maxDistance)
            .OrderBy(candidate => candidate.Enemy.NormalizedHp)
            .ThenByDescending(candidate => candidate.Enemy.IsBoss)
            .ThenBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Enemy)
            .FirstOrDefault();
    }

    [HideFromIl2Cpp]
    private EnemyController FindNearestEnemy(Vector3 origin, float maxDistance)
    {
        return _enemies
            .Where(IsUsable)
            .Select(enemy => new { Enemy = enemy, Distance = Vector3.Distance(origin, enemy.transform.position) })
            .Where(candidate => candidate.Distance <= maxDistance)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Enemy)
            .FirstOrDefault();
    }

    [HideFromIl2Cpp]
    private static Vector3 PredictDirection(Vector3 origin, EnemyController target, float projectileSpeed)
    {
        var position = target.transform.position;
        var lead = Mathf.Clamp(Vector3.Distance(origin, position) / Mathf.Max(1f, projectileSpeed), 0f, 0.32f);
        var velocity = new Vector3(target._currentDirection.x, target._currentDirection.y, 0f) * target._defaultSpeed;
        return (position + velocity * lead - origin).normalized;
    }

    [HideFromIl2Cpp]
    private bool TryGetPlayer(CharacterController controller, out RimuruPlayerRuntime player)
    {
        return _players.TryGetValue(controller.GetInstanceID(), out player!);
    }

    [HideFromIl2Cpp]
    private static bool IsRimuru(CharacterController controller)
    {
        try
        {
            var data = controller?.CurrentCharacterData;
            return data is not null && data.charName.Contains("Rimuru", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    [HideFromIl2Cpp]
    private static RimuruForm DetectInitialForm(CharacterController controller)
    {
        var suffix = controller.CurrentSkinData?.suffix ?? string.Empty;
        if (suffix.Contains("Demon", StringComparison.OrdinalIgnoreCase) || controller.StartingWeaponType == WeaponType.NIGHTSWORD2)
        {
            return RimuruForm.DemonLord;
        }

        if (suffix.Contains("Humanoid", StringComparison.OrdinalIgnoreCase) || controller.StartingWeaponType == WeaponType.NIGHTSWORD)
        {
            return RimuruForm.Humanoid;
        }

        return RimuruForm.Slime;
    }

    [HideFromIl2Cpp]
    private static string EnemyFamily(EnemyController enemy) => enemy.EnemyType.ToString().ToLowerInvariant();

    [HideFromIl2Cpp]
    private static bool IsReaper(EnemyController enemy)
    {
        var family = EnemyFamily(enemy);
        return family.Contains("reaper") || family.Contains("death");
    }

    [HideFromIl2Cpp]
    private static bool IsUsable(Component component)
    {
        try
        {
            return component is not null && component.gameObject is not null && component.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    [HideFromIl2Cpp]
    private void LoadAssets()
    {
        LoadSprite("predator-core", Path.Combine(_assetRoot, "weapons", "predator-core.png"), 32f);
        LoadSprite("rimuru-katana-v2", Path.Combine(_assetRoot, "weapons", "rimuru-katana-v2.png"), 32f);
        LoadSprite("beelzebuth-blade", Path.Combine(_assetRoot, "weapons", "beelzebuth-blade.png"), 32f);
        LoadSprite("azathoth-void-blade", Path.Combine(_assetRoot, "weapons", "azathoth-void-blade.png"), 32f);
        for (var index = 1; index <= 4; index++)
        {
            LoadSprite($"ranga-{index:00}", Path.Combine(_assetRoot, "summons", "ranga", $"ranga_{index:00}.png"), 32f);
            foreach (var form in new[] { "slime", "humanoid", "demon_lord" })
            {
                LoadSprite($"form-{form}-{index:00}", Path.Combine(_customRoot, "skins", form, "sprites", $"rimuru_{index:00}.png"), 32f);
            }
        }
    }

    [HideFromIl2Cpp]
    private void LoadSprite(string id, string path, float pixelsPerUnit)
    {
        if (!File.Exists(path))
        {
            _warning($"Asset ausente: {path}");
            return;
        }

        try
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var bytes = new Il2CppStructArray<byte>(File.ReadAllBytes(path));
            if (!ImageConversion.LoadImage(texture, bytes, markNonReadable: false))
            {
                throw new InvalidOperationException("Unity nao conseguiu decodificar o PNG.");
            }

            texture.name = $"Rimuru/{id}";
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.name = id;
            _sprites[id] = sprite;
        }
        catch (Exception exception)
        {
            _warning($"Falha ao carregar {id}: {exception.Message}");
        }
    }

    [HideFromIl2Cpp]
    private Sprite GetSprite(string id) => _sprites.TryGetValue(id, out var sprite) ? sprite : null;

    [HideFromIl2Cpp]
    private static string ResolveAssetRoot()
    {
        var candidates = new[]
        {
            Path.Combine(Paths.PluginPath, "RimuruSurvivor", "assets"),
            Path.Combine(Paths.PluginPath, "assets"),
            Path.Combine(AppContext.BaseDirectory, "BepInEx", "plugins", "RimuruSurvivor", "assets")
        };
        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }

    [HideFromIl2Cpp]
    private static string ResolveCustomRoot()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.GetFullPath(Path.Combine(localAppData, "..", "LocalLow", "poncle", "Vampire Survivors", "CustomCharacters", "RIMURU"));
    }
}

internal static class RimuruVisuals
{
    public static bool IsUsable(Component component)
    {
        try
        {
            return component is not null && component.gameObject is not null && component.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsUsable(GameObject node)
    {
        try
        {
            return node is not null && node.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    public static void Configure(SpriteRenderer renderer)
    {
        if (!IsUsable(renderer))
        {
            return;
        }

        renderer.color = Color.white;
        try
        {
            var shader = Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Transparent") ??
                         Shader.Find("Unlit/Texture");
            if (shader is not null)
            {
                var material = new Material(shader) { name = $"Rimuru Unlit/{renderer.gameObject.name}" };
                material.color = Color.white;
                material.renderQueue = 3000;
                renderer.material = material;
                renderer.forceRenderingOff = false;
                SyncTexture(renderer);
            }
        }
        catch
        {
            // Unity can invalidate material wrappers while changing scenes.
        }
    }

    public static void SyncTexture(SpriteRenderer renderer)
    {
        try
        {
            if (!IsUsable(renderer) || renderer.sprite is null || renderer.material is null)
            {
                return;
            }

            renderer.material.mainTexture = renderer.sprite.texture;
            renderer.material.color = Color.white;
        }
        catch
        {
            // Scene transitions can release the sprite or its material first.
        }
    }

    public static void Configure(MeshRenderer renderer)
    {
        if (!IsUsable(renderer))
        {
            return;
        }

        try
        {
            var shader = Shader.Find("Unlit/Transparent") ??
                         Shader.Find("Unlit/Texture") ??
                         Shader.Find("Sprites/Default");
            if (shader is null)
            {
                return;
            }

            var material = new Material(shader) { name = $"Rimuru Quad/{renderer.gameObject.name}" };
            SetMaterialColor(material, Color.white);
            material.renderQueue = 3000;
            renderer.material = material;
            renderer.forceRenderingOff = false;
        }
        catch
        {
            // Unity can invalidate renderer wrappers while changing scenes.
        }
    }

    public static bool SetTexture(MeshRenderer renderer, Sprite sprite, Color color)
    {
        try
        {
            if (!IsUsable(renderer) || sprite is null || renderer.sharedMaterial is null)
            {
                return false;
            }

            var material = renderer.sharedMaterial;
            var texture = sprite.texture;
            material.mainTexture = texture;
            SetTextureIfPresent(material, "_MainTex", texture);
            SetTextureIfPresent(material, "_BaseMap", texture);
            SetTextureIfPresent(material, "_BaseColorMap", texture);
            SetMaterialColor(material, color);
            renderer.forceRenderingOff = false;
            return material.mainTexture is not null;
        }
        catch
        {
            // Scene transitions can release the texture or its material first.
            return false;
        }
    }

    private static void SetTextureIfPresent(Material material, string propertyName, Texture texture)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, texture);
        }
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        foreach (var propertyName in new[] { "_Color", "_BaseColor", "_TintColor" })
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }
    }

    public static bool SetImageSprite(Image image, Sprite sprite)
    {
        try
        {
            if (image is null || image.gameObject is null || sprite is null)
            {
                return false;
            }

            var rawImage = image.gameObject.GetComponent<RawImage>() ?? image.gameObject.AddComponent<RawImage>();
            rawImage.texture = sprite.texture;
            rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            rawImage.material = null;
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;
            rawImage.enabled = true;
            image.enabled = false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void SafeDestroy(GameObject node)
    {
        try
        {
            if (node is not null)
            {
                UObject.Destroy(node);
            }
        }
        catch
        {
            // The native object may already have been released by Unity.
        }
    }
}

internal sealed class RimuruPlayerRuntime
{
    public RimuruPlayerRuntime(CharacterController controller, RimuruForm initialForm)
    {
        Controller = controller;
        Id = controller.GetInstanceID();
        State.InitializeForm(initialForm);
        AbilityRank = 1;
        PrimaryTimer = 0.25f;
        SecondaryTimer = 1.2f;
        RegenTimer = 0.5f;
        PreviousForm = initialForm;
    }

    public int Id { get; }
    public CharacterController Controller { get; }
    public RimuruRunState State { get; } = new();
    public RimuruForm Form => State.Form;
    public List<RangaAvatar> Rangas { get; } = new();
    public GameObject FormNode { get; private set; }
    public MeshRenderer FormRenderer { get; private set; }
    public GameObject WeaponNode { get; private set; }
    public MeshRenderer WeaponRenderer { get; private set; }
    public EnemyController LastThreat { get; set; }
    public int AbilityRank { get; set; }
    public int TreasuresOpened { get; set; }
    public bool HasAzathoth { get; set; }
    public bool CielCounterReady { get; set; }
    public float PrimaryTimer { get; set; }
    public float SecondaryTimer { get; set; }
    public float RegenTimer { get; set; }
    public RimuruForm PreviousForm { get; private set; }
    public float TransformationStartedAt { get; private set; } = -10f;
    public float WeaponEvolutionStartedAt { get; private set; } = -10f;
    public float AttackAnimationStartedAt { get; private set; } = -10f;
    public float AttackAnimationUntil { get; private set; } = -10f;
    public float EvolutionLockUntil { get; set; } = -10f;
    private bool _nativeRenderersLogged;
    private bool _meshBindingLogged;
    private bool _animationFailureLogged;
    private bool _missingFormSpriteLogged;
    private bool _lastFlipX;

    public void ResetAbilityTimers()
    {
        PrimaryTimer = 0.2f;
        SecondaryTimer = 0.8f;
    }

    public void BeginTransformation(RimuruForm previousForm, float time)
    {
        PreviousForm = previousForm;
        TransformationStartedAt = time;
    }

    public void BeginWeaponEvolution(float time)
    {
        WeaponEvolutionStartedAt = time;
    }

    public void PlayAttackAnimation(float time, float duration)
    {
        AttackAnimationStartedAt = time;
        AttackAnimationUntil = time + duration;
    }

    private void SuppressNativeRenderers(SpriteRenderer primaryRenderer, Action<string> info)
    {
        var rendererCount = 0;
        var primaryId = -1;
        try
        {
            if (RimuruVisuals.IsUsable(primaryRenderer))
            {
                primaryId = primaryRenderer.GetInstanceID();
                _lastFlipX = primaryRenderer.flipX;
            }

            var renderers = Controller.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            foreach (var renderer in renderers)
            {
                if (!RimuruVisuals.IsUsable(renderer))
                {
                    continue;
                }

                try
                {
                    rendererCount++;
                    var rendererId = renderer.GetInstanceID();
                    if (rendererId == primaryId)
                    {
                        _lastFlipX = renderer.flipX;
                    }

                    if (!_nativeRenderersLogged)
                    {
                        var spriteName = renderer.sprite?.name ?? "sem sprite";
                        var shaderName = renderer.sharedMaterial?.shader?.name ?? "sem shader";
                        info($"Render nativo do jogador: {renderer.gameObject.name}; sprite {spriteName}; shader {shaderName}; cor {renderer.color}.");
                    }

                    renderer.enabled = false;
                    renderer.forceRenderingOff = true;
                }
                catch
                {
                    // Individual native renderers can expire during a scene transition.
                }
            }
        }
        catch
        {
            // The controller hierarchy can be rebuilt while loading a stage.
        }

        if (!_nativeRenderersLogged)
        {
            info($"Supressao visual nativa do jogador {Id}: {rendererCount} renderizador(es) encontrado(s).");
            _nativeRenderersLogged = true;
        }
    }

    public void UpdateCharacterAnimation(IReadOnlyDictionary<string, Sprite> sprites, float time, Action<string> info)
    {
        try
        {
            SpriteRenderer nativeRenderer = null;
            try
            {
                nativeRenderer = Controller._CharacterRenderer;
            }
            catch
            {
                // The replacement render remains independent from the native sprite.
            }

            SuppressNativeRenderers(nativeRenderer, info);

            if (!RimuruVisuals.IsUsable(FormRenderer) || !RimuruVisuals.IsUsable(FormNode))
            {
                FormNode = GameObject.CreatePrimitive(PrimitiveType.Quad);
                FormNode.name = "Rimuru Form Visual";
                FormRenderer = FormNode.GetComponent<MeshRenderer>();
                FormRenderer.enabled = true;
                FormRenderer.sortingOrder = 234;
                RimuruVisuals.Configure(FormRenderer);
                if (RimuruVisuals.IsUsable(nativeRenderer))
                {
                    try
                    {
                        FormRenderer.sortingLayerID = nativeRenderer.sortingLayerID;
                        FormRenderer.sortingOrder = nativeRenderer.sortingOrder + 64;
                    }
                    catch
                    {
                        FormRenderer.sortingOrder = 234;
                    }
                }
                var shaderName = FormRenderer.sharedMaterial?.shader?.name ?? "sem shader";
                info($"Renderizador proprio do Rimuru criado para o jogador {Id}; shader {shaderName}.");
            }

            if (!RimuruVisuals.IsUsable(WeaponRenderer) || !RimuruVisuals.IsUsable(WeaponNode))
            {
                WeaponNode = GameObject.CreatePrimitive(PrimitiveType.Quad);
                WeaponNode.name = "Rimuru Weapon Visual";
                WeaponRenderer = WeaponNode.GetComponent<MeshRenderer>();
                WeaponRenderer.enabled = false;
                WeaponRenderer.sortingOrder = 236;
                RimuruVisuals.Configure(WeaponRenderer);
            }

            var transformationProgress = Mathf.Clamp01((time - TransformationStartedAt) / 1.05f);
            var transforming = transformationProgress < 1f;
            var displayedForm = transforming && transformationProgress < 0.38f ? PreviousForm : Form;
            var formId = displayedForm switch
            {
                RimuruForm.Slime => "slime",
                RimuruForm.Humanoid => "humanoid",
                RimuruForm.DemonLord => "demon_lord",
                _ => "slime"
            };
            var frame = 1 + (int)(time * (displayedForm == RimuruForm.Slime ? 9f : 8f)) % 4;
            var formSpriteId = $"form-{formId}-{frame:00}";
            if (sprites.TryGetValue(formSpriteId, out var sprite))
            {
                var formColor = transforming
                    ? Color.Lerp(Color.white, new Color(0.35f, 0.95f, 1f, 1f), Mathf.Sin(transformationProgress * Mathf.PI))
                    : Color.white;
                var textureApplied = RimuruVisuals.SetTexture(FormRenderer, sprite, formColor);
                if (!_meshBindingLogged)
                {
                    var sourceTexture = sprite.texture;
                    var shaderName = FormRenderer.sharedMaterial?.shader?.name ?? "sem shader";
                    info($"Textura da malha do Rimuru: sprite {sprite.name}, origem {sourceTexture?.name ?? "ausente"} {sourceTexture?.width ?? 0}x{sourceTexture?.height ?? 0}, shader {shaderName}, aplicada {textureApplied}.");
                    _meshBindingLogged = true;
                }
            }
            else if (!_missingFormSpriteLogged)
            {
                info($"Sprite de forma nao encontrado: {formSpriteId}; dicionario com {sprites.Count} item(ns).");
                _missingFormSpriteLogged = true;
            }

            FormNode.SetActive(true);
            var bob = Mathf.Sin(time * (displayedForm == RimuruForm.Slime ? 9f : 7f));
            var baseScale = displayedForm switch
            {
                RimuruForm.Slime => 1.18f,
                RimuruForm.Humanoid => 1.45f,
                RimuruForm.DemonLord => 1.35f,
                _ => 1f
            };
            var squashX = baseScale * (1f + bob * (displayedForm == RimuruForm.Slime ? 0.045f : 0.018f));
            var squashY = baseScale * (1f - bob * (displayedForm == RimuruForm.Slime ? 0.035f : 0.012f));
            if (transforming)
            {
                var surge = 0.72f + Mathf.Sin(transformationProgress * Mathf.PI) * 0.62f;
                squashX *= surge;
                squashY *= surge;
            }
            FormNode.transform.position = Controller.transform.position + new Vector3(0f, bob * 0.025f, -0.05f);
            FormNode.transform.localScale = new Vector3(_lastFlipX ? -squashX : squashX, squashY, 1f);
            FormRenderer.enabled = true;
            FormRenderer.forceRenderingOff = false;
            if (RimuruVisuals.IsUsable(nativeRenderer))
            {
                try
                {
                    _lastFlipX = nativeRenderer.flipX;
                }
                catch
                {
                    // Continue rendering our form and weapon even if the native wrapper expired.
                }
            }


            var showWeapon = Form != RimuruForm.Slime;
            WeaponNode.SetActive(showWeapon);
            WeaponRenderer.enabled = showWeapon;
            WeaponRenderer.forceRenderingOff = false;
            if (showWeapon)
            {
                var weaponId = HasAzathoth ? "azathoth-void-blade" : Form == RimuruForm.DemonLord ? "beelzebuth-blade" : "rimuru-katana-v2";
                sprites.TryGetValue(weaponId, out var weaponSprite);
                var facingLeft = _lastFlipX;
                var attackDuration = Mathf.Max(0.01f, AttackAnimationUntil - AttackAnimationStartedAt);
                var attackProgress = Mathf.Clamp01((time - AttackAnimationStartedAt) / attackDuration);
                var attacking = time < AttackAnimationUntil;
                var direction = facingLeft ? -1f : 1f;
                var idleAngle = facingLeft ? 48f : -48f;
                var slashAngle = Mathf.Lerp(facingLeft ? -95f : 95f, facingLeft ? 70f : -70f, attackProgress);
                WeaponNode.transform.position = Controller.transform.position + new Vector3(0.38f * direction, -0.04f, -0.08f);
                WeaponNode.transform.rotation = Quaternion.Euler(0f, 0f, attacking ? slashAngle : idleAngle + bob * 3f);
                var evolutionGlow = Mathf.Clamp01(1f - (time - WeaponEvolutionStartedAt) / 1.2f);
                var weaponScale = (attacking ? 1.2f : 0.92f) * (1f + evolutionGlow * 0.45f);
                WeaponNode.transform.localScale = new Vector3(facingLeft ? -weaponScale : weaponScale, weaponScale, 1f);
                var weaponColor = evolutionGlow > 0f
                    ? Color.Lerp(Color.white, new Color(0.35f, 0.9f, 1f, 1f), evolutionGlow)
                    : Color.white;
                RimuruVisuals.SetTexture(WeaponRenderer, weaponSprite, weaponColor);
                WeaponRenderer.sortingLayerID = FormRenderer.sortingLayerID;
                WeaponRenderer.sortingOrder = FormRenderer.sortingOrder + 2;
            }
        }
        catch (Exception exception)
        {
            if (!_animationFailureLogged)
            {
                info($"Falha no render proprio do Rimuru: {exception.GetType().Name}: {exception.Message}");
                _animationFailureLogged = true;
            }
        }
    }

    public void Dispose()
    {
        RimuruVisuals.SafeDestroy(FormNode);
        RimuruVisuals.SafeDestroy(WeaponNode);
        foreach (var ranga in Rangas)
        {
            ranga.Dispose();
        }
        Rangas.Clear();
    }
}

internal sealed class RangaAvatar
{
    public RangaAvatar(GameObject node, SpriteRenderer renderer, Sprite[] frames, float orbitAngle)
    {
        Node = node;
        Renderer = renderer;
        Frames = frames;
        OrbitAngle = orbitAngle;
    }

    public GameObject Node { get; }
    public SpriteRenderer Renderer { get; }
    public Sprite[] Frames { get; }
    public EnemyController Target { get; set; }
    public float OrbitAngle { get; set; }
    public float FrameTimer { get; set; }
    public float Cooldown { get; set; } = 0.5f;
    public bool HasHit { get; set; }

    public void Dispose() => RimuruVisuals.SafeDestroy(Node);
}

internal enum RimuruEffectKind
{
    Blade,
    Pulse,
    Vortex
}

internal sealed class RimuruEffect
{
    public RimuruEffect(GameObject node, SpriteRenderer renderer)
    {
        Node = node;
        Renderer = renderer;
    }

    public GameObject Node { get; }
    public SpriteRenderer Renderer { get; }
    public RimuruEffectKind Kind { get; set; }
    public RimuruPlayerRuntime Owner { get; set; }
    public Vector3 Velocity { get; set; }
    public float Damage { get; set; }
    public float Radius { get; set; }
    public float Lifetime { get; set; }
    public float Age { get; set; }
    public float PulseTimer { get; set; }
    public float Delay { get; set; }
    public int Pierce { get; set; }
    public bool ReaperProtocol { get; set; }
    public HashSet<int> HitIds { get; } = new();

    public void Dispose() => RimuruVisuals.SafeDestroy(Node);
}
