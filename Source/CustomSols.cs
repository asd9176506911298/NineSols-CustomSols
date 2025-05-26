using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NineSolsAPI;
using NineSolsAPI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomSols;

[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CustomSols : BaseUnityPlugin {
    public static CustomSols instance { get; private set; } = null!;
    private Harmony harmony = null!;
    private Dictionary<string, SpriteRenderer> cachedSpriteRenderers = new Dictionary<string, SpriteRenderer>();
    private static bool isAssetsLoaded = false;

    public ConfigEntry<bool> isEnablePlayer = null!;
    public ConfigEntry<bool> isEnableMenuLogo = null!;
    public ConfigEntry<bool> isEnableUIChiBall = null!;
    public ConfigEntry<bool> isEnableTalismanBall = null!;
    public ConfigEntry<bool> isEnableDash = null!;
    public ConfigEntry<bool> isEnableAirJump = null!;
    public ConfigEntry<bool> isEnableImPerfectParry = null!;
    public ConfigEntry<bool> isEnablePerfectParry = null!;
    public ConfigEntry<bool> isEnableUCSuccess = null!;
    public ConfigEntry<bool> isEnableUCCharging = null!;
    public ConfigEntry<bool> isEnableUCAroundEffect = null!;
    public ConfigEntry<bool> isEnableBow = null!;
    public ConfigEntry<bool> isEnableSword = null!;
    public ConfigEntry<bool> isEnableFoo = null!;
    public static ConfigEntry<bool> isUseExample = null!;
    public ConfigEntry<bool> isToastPlayerSprite = null!;
    private ConfigEntry<Color> UCChargingColor = null!;
    private ConfigEntry<Color> UCSuccessColor = null!;
    private ConfigEntry<KeyboardShortcut> reloadShortcut = null!;

    public static readonly HashSet<string> bowSpritePaths = new HashSet<string> {
        "GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Yee_Skill/HoHoYee_Archery/Bow",
        "GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Yee_Skill/HoHoYee_Archery/Bow/Bow_A",
    };

    public static readonly HashSet<string> swordSpritePaths = new HashSet<string> {
        "HoHoYee_AttackA_PoolObject_Variant(Clone)/Sprite",
        "HoHoYee_AttackB_PoolObject_Variant(Clone)/Sprite",
        "HoHoYee_AttackC ThirdAttack Effect(Clone)/Sprite",
        "Yee 氣刃 chi blade(Clone)/Projectile FSM/FSM Animator/View/Sprite",
        "HoHoYee_AttackC ThirdAttack 劍氣玉 Effect(Clone)/Sprite",
        "GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/Effect_Attack/ChargeAttack/ChargeAttackSprite",
        "HoHoYee_Charging 蓄力攻擊特效(Clone)/ChargeAttackSprite",
        "HoHoYee_Charging 蓄力攻擊特效(Clone)/Super Charge Ability/childNode/ChargeAttackSprite"
    };

    private void Awake() {
        instance = this;
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        harmony = Harmony.CreateAndPatchAll(typeof(CustomSols).Assembly);
        SetupConfig();
        KeybindManager.Add(this, Reload, () => reloadShortcut.Value);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Logger.LogInfo($"isUseExample: {isUseExample.Value}");
    }

    private void Start() {
        InitializeAssets();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void InitializeAssets() {
        isAssetsLoaded = false;
        AssetLoader.Init();
        isAssetsLoaded = true;
        CacheSpriteRenderers();
        if (isEnableMenuLogo.Value) ChangeMenuLogo(); // 立即應用 Logo
    }

    private void LateUpdate() {
        if (!isAssetsLoaded) return;

        if (isEnablePlayer.Value) PlayerSprite();
        if (isEnablePerfectParry.Value) PerfectParry();
        if (isEnableDash.Value) Dash();
        if (isEnableAirJump.Value) AirJump();
        if (isEnableUCAroundEffect.Value) UCAroundEffect();
        if (isEnableUCSuccess.Value) UCSuccess();
        if (isEnableUCCharging.Value) UCCharging();
        if (isEnableTalismanBall.Value) TalismanBall();
        if (isEnableFoo.Value) Foo();
        if (isToastPlayerSprite.Value && Player.i?.PlayerSprite != null)
            ToastManager.Toast(Player.i.PlayerSprite.sprite.name);
    }

    private void ChangeMenuLogo() {
        if (!isAssetsLoaded) {
            ToastManager.Toast("Assets not loaded for MenuLogo");
            return;
        }
        var logoObject = GameObject.Find("MenuLogic/MainMenuLogic/Providers/MenuUIPanel/Logo");
        if (logoObject != null &&
            logoObject.GetComponent<UnityEngine.UI.Image>() is { } image &&
            AssetLoader.cacheMenuLogoSprites.TryGetValue("9sLOGO_1", out var sprite)) {
            image.sprite = sprite;
            ToastManager.Toast("MenuLogo changed successfully");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        CacheSpriteRenderers();
        if (isEnableMenuLogo.Value) ChangeMenuLogo();
        if (isEnableUIChiBall.Value) ChangeUIChiBall();
        if (isEnableImPerfectParry.Value) ImPerfectParry();
        if (isEnableSword.Value) SwordOnce();
        if (isEnableBow.Value) InitializeBowSprites();
    }

    private void SetupConfig() {
        isEnablePlayer = Config.Bind("", "Player Sprite", true, "");
        isEnableMenuLogo = Config.Bind("", "Menu Logo Sprite", true, "");
        isEnableUIChiBall = Config.Bind("", "UI Chi Ball", true, "");
        isEnableTalismanBall = Config.Bind("", "EnableTalisman Ball Sprite", true, "");
        isEnableDash = Config.Bind("", "Dash Sprite", true, "");
        isEnableAirJump = Config.Bind("", "AirJump Sprite", true, "");
        isEnableImPerfectParry = Config.Bind("", "imPerfectParry Sprite", true, "");
        isEnablePerfectParry = Config.Bind("", "PerfectParry Sprite", true, "");
        isEnableUCSuccess = Config.Bind("", "UCSuccess Sprite", true, "");
        isEnableUCCharging = Config.Bind("", "UCCharging Sprite", true, "");
        isEnableUCAroundEffect = Config.Bind("", "UCAroundEffect Sprite", true, "");
        isEnableBow = Config.Bind("", "Bow Sprite", true, "");
        isEnableSword = Config.Bind("", "Sword Sprite", true, "");
        isEnableFoo = Config.Bind("", "Foo Sprite", true, "");
        isUseExample = Config.Bind("", "Use Example Sprite", true, "");
        isToastPlayerSprite = Config.Bind("", "Toast Player Sprite Name", false, "");
        UCChargingColor = Config.Bind("Color", "UCCharging Color", new Color(1f, 0.837f, 0f, 1f), "");
        UCSuccessColor = Config.Bind("Color", "UCSuccess Color", new Color(1f, 0.718f, 1f, 1f), "");
        reloadShortcut = Config.Bind("Shortcut", "Reload Shortcut", new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl), "");

        isUseExample.SettingChanged += (sender, args) => InitializeAssets();
    }

    private void CacheSpriteRenderers() {
        cachedSpriteRenderers.Clear();
        var renderers = FindObjectsOfType<SpriteRenderer>(true);
        foreach (var renderer in renderers) {
            var path = GetGameObjectPath(renderer.gameObject);
            cachedSpriteRenderers[path] = renderer;
        }
    }

    public static string GetGameObjectPath(GameObject obj) {
        var path = obj.name;
        var current = obj.transform;
        while (current.parent != null) {
            current = current.parent;
            path = current.name + "/" + path;
        }
        return path;
    }

    private void ChangeUIChiBall() {
        var paths = new[] {
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint/BG/Rotate/Fill",
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (5)/BG/Rotate/Fill",
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (6)/BG/Rotate/Fill",
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (7)/BG/Rotate/Fill",
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (8)/BG/Rotate/Fill"
        };

        if (AssetLoader.cacheUIChiBallSprites.TryGetValue("ParryBalls", out var sprite)) {
            foreach (var path in paths) {
                if (cachedSpriteRenderers.TryGetValue(path, out var renderer)) {
                    renderer.sprite = sprite;
                }
            }
        }
    }

    private void ImPerfectParry() {
        foreach (var renderer in FindObjectsOfType<ParticleSystemRenderer>(true)) {
            if (renderer.transform.parent.name == "YeeParryEffect_Not Accurate(Clone)") {
                if (AssetLoader.cacheParrySprites.TryGetValue("imPerfect", out var sprite)) {
                    renderer.materials[1].SetTexture("_MainTex", sprite.texture);
                }
            }
        }
    }

    private void PerfectParry() {
        if (cachedSpriteRenderers.TryGetValue("YeeParryEffectAccurate_Green(Clone)/ParrySparkAccurate0", out var renderer) &&
            AssetLoader.cacheParrySprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
            renderer.transform.rotation = Quaternion.Euler(0f, Player.i.Facing == Facings.Left ? 180f : 0f, 0f);
        }
    }

    private void Dash() {
        if (cachedSpriteRenderers.TryGetValue("Effect_Roll Dodge AfterImage(Clone)/Effect_HoHoYee_AirJump0", out var renderer) &&
            AssetLoader.cachePlayerSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
        }
    }

    private void AirJump() {
        if (cachedSpriteRenderers.TryGetValue("Effect_AirJump(Clone)/Effect_HoHoYee_AirJump0", out var renderer) &&
            AssetLoader.cachePlayerSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
        }
    }

    private void UCAroundEffect() {
        if (cachedSpriteRenderers.TryGetValue("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/Effect_ParryCounterAttack0", out var renderer) &&
            AssetLoader.cacheParrySprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
        }
    }

    private void UCSuccess() {
        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging") is { } obj) {
            if (AssetLoader.cacheParrySprites.TryGetValue("UCSuccess", out var sprite)) {
                var particleRenderer = obj.GetComponent<ParticleSystemRenderer>();
                particleRenderer.materials[0].SetTexture("_MainTex", sprite.texture);
                obj.GetComponent<ParticleSystem>().startColor = UCSuccessColor.Value;
            } else {
                ToastManager.Toast("UCSuccess sprite not found in cacheParrySprites");
            }
        }
    }

    private void UCCharging() {
        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging C") is { } obj) {
            if (AssetLoader.cacheParrySprites.TryGetValue("UCCharging", out var sprite)) {
                var particleRenderer = obj.GetComponent<ParticleSystemRenderer>();
                particleRenderer.materials[0].SetTexture("_MainTex", sprite.texture);
                obj.GetComponent<ParticleSystem>().startColor = UCChargingColor.Value;
            } else {
                ToastManager.Toast("UCCharging sprite not found in cacheParrySprites");
            }
        }
    }

    private void TalismanBall() {
        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo") is { activeSelf: true } foo) {
            for (int i = 1; i <= 5; i++) {
                var ball = foo.transform.Find($"FooDots/D{i}/FooDot ({i})/JENG/Ball")?.GetComponent<SpriteRenderer>();
                if (ball != null && !string.IsNullOrEmpty(ball.sprite.name) &&
                    AssetLoader.cacheTalismanBallSprites.TryGetValue(ball.sprite.name, out var sprite)) {
                    ball.sprite = sprite;
                }
            }
        }
    }

    private void PlayerSprite() {
        if (Player.i?.PlayerSprite?.sprite != null &&
            AssetLoader.cachePlayerSprites.TryGetValue(Player.i.PlayerSprite.sprite.name, out var sprite)) {
            Player.i.PlayerSprite.sprite = sprite;
        }
    }

    private void InitializeBowSprites() {
        foreach (var path in bowSpritePaths) {
            if (cachedSpriteRenderers.TryGetValue(path, out var renderer) &&
                AssetLoader.cacheBowSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
                renderer.sprite = sprite;
            }
        }

        foreach (var go in FindObjectsOfType<GameObject>(true)) {
            if (go.name.StartsWith("NormalArrow Shoot 穿雲 Lv")) {
                UpdateBowSprite(go, "光束");
                UpdateBowSprite(go, "NormalArrow");
                UpdateBowSprite(go, "NormalArrow/ChasingArrowLight");
            } else if (go.name.StartsWith("ExplodingArrow Shooter 爆破發射器 Lv")) {
                UpdateBowSprite(go, "Exploding Arrow/ExplodingArrow/ExplodingArrow");
                UpdateBowSprite(go, "Exploding Arrow/ExplodingArrow/ChasingArrowLight");
                UpdateBowSprite(go, "Exploding Arrow/EnergyBall/Core");
            } else if (go.name.StartsWith("Explosion Damage 爆破箭 閃電 lv")) {
                UpdateBowSprite(go, "ATTACK/Core");
            } else if (go.name.StartsWith("Chasing Arrow Shooter 飛天御劍 lv")) {
                for (int i = 1; i <= 2; i++) {
                    UpdateBowSprite(go, $"Circle Shooter/Arrow ({i})/ChasingArrow /ChasingArrowLight");
                    UpdateBowSprite(go, $"Circle Shooter/Arrow ({i})/ChasingArrow /Parent 刺/刺/刺");
                    UpdateBowSprite(go, $"Circle Shooter/Arrow ({i})/ChasingArrow /Parent 刺/刺 (1)/刺");
                }
            }
        }
    }

    private void UpdateBowSprite(GameObject parent, string childPath) {
        if (parent == null) return;

        var renderer = parent.transform.Find(childPath)?.GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null && AssetLoader.cacheBowSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
        }
    }

    private void SwordOnce() {
        var chargePaths = new[] { "F1", "F2", "F3", "F4", "F5" };
        foreach (var path in chargePaths) {
            if (GameObject.Find($"GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharged/{path}") != null) {
                GameObject.Find($"GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharged/{path}").GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", AssetLoader.cacheSwordSprites["FooSmokeGlow"].texture);
            }
        }

        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharging/P_hit") != null) {
            GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharging/P_hit").GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", AssetLoader.cacheSwordSprites["bubbletrail"].texture);
        }
    }

    private void Foo() {
        var fooPaths = new Dictionary<string, string> {
            { "FooPrefab Deposit(Clone)/Foo Charm Deposit/Animator(StartShouldDisable)/Effect_Foo/FOO", "Effect_Foo4" },
            { "FooPrefab Deposit(Clone)/Foo Charm Deposit/Animator(StartShouldDisable)/流派/一氣貫通/Effect_一氣貫通/FOO", "Effect_Foo3" },
            { "FooPrefab Deposit(Clone)/Foo Charm Deposit/Animator(StartShouldDisable)/流派/行雲流水/Effect_行雲流水/FOO", "Effect_Foo3" },
            { "FooPrefab Deposit(Clone)/Foo Charm Deposit/Animator(StartShouldDisable)/流派/收放自如/Effect_收放自如/FOO", "Effect_Foo3" }
        };

        foreach (var (path, spriteKey) in fooPaths) {
            if (cachedSpriteRenderers.TryGetValue(path, out var renderer) &&
                AssetLoader.cacheFooSprites.TryGetValue(spriteKey, out var sprite)) {
                renderer.sprite = sprite;
            }
        }
    }

    private void Reload() {
        InitializeAssets();
        if (isEnableMenuLogo.Value) ChangeMenuLogo();
        if (isEnableUIChiBall.Value) ChangeUIChiBall();
        if (isEnableImPerfectParry.Value) ImPerfectParry();
        if (isEnableSword.Value) SwordOnce();
        if (isEnableBow.Value) InitializeBowSprites();
    }

    private void OnDestroy() {
        harmony.UnpatchSelf();
    }
}