using BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using VampireSurvivors.Data;
using VampireSurvivors.Objects.Characters;
using VampireSurvivors.Objects.Items;
using VampireSurvivors.Objects.Weapons;
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
    private RimuruPlayerRuntime EnsurePlayer(CharacterController controller)
    {
        var id = controller.GetInstanceID();
        if (_players.TryGetValue(id, out var existing))
        {
            return existing;
        }

        var initialForm = DetectInitialForm(controller);
        var player = new RimuruPlayerRuntime(controller, initialForm);
        _players[id] = player;
        EnsureRangaCount(player, initialForm == RimuruForm.DemonLord ? 3 : 1);
        _info($"Rimuru detectado em partida: forma inicial {initialForm}, nivel {controller.Level}.");
        return player;
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
            if (controller.Level >= 20 && predatorRank >= 8 && player.State.TryUnlockHumanoid(controller.Level))
            {
                player.ResetAbilityTimers();
                EnsureWeapon(controller, WeaponType.NIGHTSWORD);
                SpawnTransformation(player, new Color(0.25f, 0.85f, 1f, 1f));
                _info("Rimuru evoluiu de Slime para a forma Humanoide.");
            }
        }
        else if (player.Form == RimuruForm.Humanoid)
        {
            var swordRank = Math.Max(GetWeaponRank(controller, WeaponType.NIGHTSWORD), Math.Min(8, 1 + Math.Max(0, controller.Level - 20) / 3));
            var sageRank = Math.Max(GetWeaponRank(controller, WeaponType.COOLDOWN), Math.Min(5, 1 + Math.Max(0, controller.Level - 20) / 5));
            player.AbilityRank = swordRank;
            if (controller.Level >= 40 && swordRank >= 8 && sageRank >= 5 && player.TreasuresOpened > 0 &&
                player.State.TryEvolveDemonLord(swordRank, sageRank, treasureOpened: true))
            {
                player.ResetAbilityTimers();
                EnsureWeapon(controller, WeaponType.NIGHTSWORD2);
                EnsureRangaCount(player, 3);
                controller.SetHealthToMax();
                controller.SetInvulForMilliSecondsNonCumulativeIncludeParma(5000f);
                SpawnTransformation(player, new Color(0.65f, 0.08f, 0.9f, 1f));
                _info("Rimuru despertou como Lorde Demonio; Beelzebuth e a Barreira Multicamadas estao ativos.");
            }
        }
        else
        {
            player.AbilityRank = Math.Max(GetWeaponRank(controller, WeaponType.NIGHTSWORD2), Math.Min(8, 1 + Math.Max(0, controller.Level - 40) / 4));
            if (!player.HasAzathoth && player.State.IsCiel && player.TreasuresOpened >= 2 && controller.Level >= 60 &&
                player.State.TryEvolveAzathoth(player.AbilityRank, treasureOpened: true))
            {
                player.HasAzathoth = true;
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
            SpawnPulse(origin, GetSprite("predator-core"), new Color(0.2f, 0.9f, 1f, 0.9f), 2.1f + rank * 0.11f, (18f + rank * 5f) * might, 0.55f);
            player.PrimaryTimer = Mathf.Max(1.05f, 2.35f - rank * 0.12f);
            return;
        }

        var direction = target is null
            ? new Vector3(1f, 0f, 0f)
            : PredictDirection(origin, target, player.Form == RimuruForm.DemonLord ? 14f : 11f);
        var sprite = GetSprite(player.Form == RimuruForm.DemonLord ? "beelzebuth-blade" : "rimuru-katana-v2");
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
            SpawnBlade(player, origin, direction, GetSprite("rimuru-katana-v2"), (14f + rank * 3f) * might, 9f, 2 + rank / 4, waterBlade: true);
            player.SecondaryTimer = Mathf.Max(2.2f, 4.4f - rank * 0.2f);
            return;
        }

        if (player.Form == RimuruForm.Humanoid)
        {
            SpawnBlackLightning(origin, (26f + rank * 6f) * might, 3 + rank / 2);
            if (target is not null)
            {
                SpawnVortex(target.transform.position, GetSprite("predator-core"), (20f + rank * 4f) * might, 2.4f, 1.1f, false);
            }
            player.SecondaryTimer = Mathf.Max(2.4f, 5.2f - rank * 0.25f);
            return;
        }

        var vortexPosition = target?.transform.position ?? origin;
        SpawnVortex(vortexPosition, GetSprite("beelzebuth-blade"), (42f + rank * 8f) * might, 4.2f, 2.2f, player.HasAzathoth);
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
    private void SpawnPulse(Vector3 position, Sprite sprite, Color color, float radius, float damage, float lifetime)
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
            effect.Age += deltaTime;
            if (effect.Age >= effect.Lifetime || effect.Node is null)
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
        var characterRenderer = player.FormRenderer ?? player.Controller._CharacterRenderer;
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
            }

            ranga.Cooldown -= deltaTime;
            if (ranga.Target is null || !IsUsable(ranga.Target))
            {
                ranga.Target = null;
                if (ranga.Cooldown <= 0)
                {
                    ranga.Target = FindBestTarget(player.Controller.transform.position, 15f);
                    ranga.HasHit = false;
                }
            }

            if (ranga.Target is not null)
            {
                var targetPosition = ranga.Target.transform.position;
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
            var characterRenderer = player.FormRenderer ?? player.Controller._CharacterRenderer;
            if (characterRenderer is not null)
            {
                renderer.sortingLayerID = characterRenderer.sortingLayerID;
                renderer.sortingOrder = characterRenderer.sortingOrder + 8 + player.Rangas.Count;
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
        SpawnPulse(player.Controller.transform.position, GetSprite("beelzebuth-blade"), color, 4.5f, 70f + player.Controller.Level * 2f, 1.05f);
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
    }

    public int Id { get; }
    public CharacterController Controller { get; }
    public RimuruRunState State { get; } = new();
    public RimuruForm Form => State.Form;
    public List<RangaAvatar> Rangas { get; } = new();
    public GameObject FormNode { get; private set; }
    public SpriteRenderer FormRenderer { get; private set; }
    public EnemyController LastThreat { get; set; }
    public int AbilityRank { get; set; }
    public int TreasuresOpened { get; set; }
    public bool HasAzathoth { get; set; }
    public bool CielCounterReady { get; set; }
    public float PrimaryTimer { get; set; }
    public float SecondaryTimer { get; set; }
    public float RegenTimer { get; set; }

    public void ResetAbilityTimers()
    {
        PrimaryTimer = 0.2f;
        SecondaryTimer = 0.8f;
    }

    public void UpdateCharacterAnimation(IReadOnlyDictionary<string, Sprite> sprites, float time, Action<string> info)
    {
        try
        {
            var nativeRenderer = Controller._CharacterRenderer;
            if (FormNode is null || FormRenderer is null)
            {
                FormNode = new GameObject("Rimuru Form Visual");
                FormRenderer = FormNode.AddComponent<SpriteRenderer>();
                FormRenderer.enabled = true;
                FormRenderer.color = Color.white;
                if (nativeRenderer is not null)
                {
                    FormRenderer.sortingLayerID = nativeRenderer.sortingLayerID;
                    FormRenderer.sortingOrder = nativeRenderer.sortingOrder + 4;
                }
                else
                {
                    FormRenderer.sortingOrder = 234;
                }
                info($"Renderizador proprio do Rimuru criado para o jogador {Id}.");
            }

            var formId = Form switch
            {
                RimuruForm.Slime => "slime",
                RimuruForm.Humanoid => "humanoid",
                RimuruForm.DemonLord => "demon_lord",
                _ => "slime"
            };
            var frame = 1 + (int)(time * (Form == RimuruForm.Slime ? 9f : 8f)) % 4;
            if (sprites.TryGetValue($"form-{formId}-{frame:00}", out var sprite))
            {
                FormRenderer.sprite = sprite;
            }

            FormNode.SetActive(true);
            FormNode.transform.position = Controller.transform.position + new Vector3(0f, 0f, -0.05f);
            FormNode.transform.localScale = Vector3.one * (Form == RimuruForm.Slime ? 1.18f : 1f);
            FormRenderer.enabled = true;
            FormRenderer.color = Color.white;
            if (nativeRenderer is not null)
            {
                nativeRenderer.enabled = false;
                FormRenderer.flipX = nativeRenderer.flipX;
                FormRenderer.sortingLayerID = nativeRenderer.sortingLayerID;
                FormRenderer.sortingOrder = nativeRenderer.sortingOrder + 4;
            }
        }
        catch
        {
            // The controller can be torn down between scene transitions.
        }
    }

    public void Dispose()
    {
        if (FormNode is not null)
        {
            UObject.Destroy(FormNode);
        }
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

    public void Dispose() => UObject.Destroy(Node);
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
    public int Pierce { get; set; }
    public bool ReaperProtocol { get; set; }
    public HashSet<int> HitIds { get; } = new();

    public void Dispose() => UObject.Destroy(Node);
}
