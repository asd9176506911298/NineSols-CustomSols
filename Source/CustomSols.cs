using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NineSolsAPI;
using NineSolsAPI.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

    public ConfigEntry<bool> openFolder = null!;
    public ConfigEntry<bool> isToastPlayerSprite = null!;
    private ConfigEntry<float> spriteDelaySecond= null!;
    private ConfigEntry<Color> UCChargingColor = null!;
    private ConfigEntry<Color> UCSuccessColor = null!;
    private ConfigEntry<KeyboardShortcut> reloadShortcut = null!;

    private ConfigEntry<string?> skins = null!;
    public static string currSkinFolder = "";
    private string basePath = "";

    private int currentSpriteIndex = 1;
    private float spriteChangeTimer;

    public static bool arrowInit = false;
    public static bool arrowInit2 = false;

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
        ChangeMenuLogo(); // 立即應用 Logo
    }

    private void LateUpdate() {
        if (!isAssetsLoaded) return;

        PlayerSprite();
        PerfectParry();
        Dash();
        AirJump();
        UCAroundEffect();
        UCSuccess();
        UCCharging();
        TalismanBall();
        Foo();
        Sword();

        //UI
        UpdateHeartSprite();
        UpdateArrowIcon();
        UpdateButterflySprite();
        UpdateExpRing();

        if (isToastPlayerSprite.Value && Player.i?.PlayerSprite != null)
            ToastManager.Toast(Player.i.PlayerSprite.sprite.name);
    }

    private void ChangeMenuLogo() {
        if (!isAssetsLoaded) {
            return;
        }

        if (AssetLoader.cacheMenuLogoSprites == null || AssetLoader.cacheMenuLogoSprites.Count == 0) {
            return;
        }

        var logoObject = GameObject.Find("MenuLogic/MainMenuLogic/Providers/MenuUIPanel/Logo");
        if (logoObject != null &&
            logoObject.GetComponent<UnityEngine.UI.Image>() is { } image &&
            AssetLoader.cacheMenuLogoSprites.TryGetValue("9sLOGO_1", out var sprite)) {
            image.sprite = sprite;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        CacheSpriteRenderers();
        ChangeMenuLogo();
        ChangeUIChiBall();
        ImPerfectParry();
        SwordOnce();
        InitializeBowSprites();
        UpdateExpRing();
        UpdateHpBar();
        UpdatePotion();

        arrowInit = false;
        arrowInit2 = false;
    }

    private void SetupConfig() {
        openFolder = Config.Bind("Folder", "Open CustomSols Folder", false, "");
        isToastPlayerSprite = Config.Bind("", "Toast Player Sprite Name", false, "");
        spriteDelaySecond = Config.Bind("Sprite Delay Second", "PlayerSpriteAllUseThis Sprite Delay Second", 0.12f, "");
        UCChargingColor = Config.Bind("Color", "UCCharging Color", new Color(1f, 0.837f, 0f, 1f), "");
        UCSuccessColor = Config.Bind("Color", "UCSuccess Color", new Color(1f, 0.718f, 1f, 1f), "");
        reloadShortcut = Config.Bind("Shortcut", "Reload Shortcut", new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl), "");

        basePath =
#if DEBUG
            @"E:\Games\Nine Sols1030\BepInEx\plugins\CustomSols\CustomSols";
#else
        Path.Combine(Paths.ConfigPath, "CustomSols");
#endif

        skins = Config.Bind<string?>("Skin List", "Select Skin", null, new ConfigDescription("", new AcceptableValueList<string?>(AssetLoader.GetAllDirectories(basePath).Select(Path.GetFileName).ToArray())));
        currSkinFolder = skins.Value ?? "Default";
        skins.SettingChanged += (sender, args) => {
#if DEBUG
            ToastManager.Toast(skins.Value);
#endif
            currSkinFolder = skins.Value;
            InitializeAssets();
        };
        openFolder.SettingChanged += (sender, args) => {
            Process.Start(basePath);
        };
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
        if (AssetLoader.cacheUISprites == null || AssetLoader.cacheUISprites.Count == 0) {
            return;
        }

        var paths = new[] {
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint/BG/Rotate/Fill",
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (5)/BG/Rotate/Fill",
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (6)/BG/Rotate/Fill",
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (7)/BG/Rotate/Fill",
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (8)/BG/Rotate/Fill"
        };

        if (AssetLoader.cacheUISprites.TryGetValue("ParryBalls", out var sprite)) {
            foreach (var path in paths) {
                if (cachedSpriteRenderers.TryGetValue(path, out var renderer)) {
                    renderer.sprite = sprite;   
                }
            }
        }
    }

    private void ImPerfectParry() {
        if (AssetLoader.cacheParrySprites == null || AssetLoader.cacheParrySprites.Count == 0) {
            return;
        }

        foreach (var renderer in FindObjectsOfType<ParticleSystemRenderer>(true)) {
            if (renderer.transform.parent.name == "YeeParryEffect_Not Accurate(Clone)") {
                if (AssetLoader.cacheParrySprites.TryGetValue("imPerfect", out var sprite)) {
                    renderer.materials[1].SetTexture("_MainTex", sprite.texture);
                }
            }
        }
    }

    private void PerfectParry() {
        if (AssetLoader.cacheParrySprites == null || AssetLoader.cacheParrySprites.Count == 0) {
            return;
        }

        if (cachedSpriteRenderers.TryGetValue("YeeParryEffectAccurate_Green(Clone)/ParrySparkAccurate0", out var renderer) &&
            AssetLoader.cacheParrySprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
            renderer.transform.rotation = Quaternion.Euler(0f, Player.i.Facing == Facings.Left ? 180f : 0f, 0f);
        }
    }

    private void Dash() {
        if (AssetLoader.cachePlayerSprites == null || AssetLoader.cachePlayerSprites.Count == 0) {
            return;
        }

        if (cachedSpriteRenderers.TryGetValue("Effect_Roll Dodge AfterImage(Clone)/Effect_HoHoYee_AirJump0", out var renderer) &&
            AssetLoader.cachePlayerSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
        }
    }

    private void AirJump() {
        if (AssetLoader.cachePlayerSprites == null || AssetLoader.cachePlayerSprites.Count == 0) {
            return;
        }

        if (cachedSpriteRenderers.TryGetValue("Effect_AirJump(Clone)/Effect_HoHoYee_AirJump0", out var renderer) &&
            AssetLoader.cachePlayerSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
        }
    }

    private void UCAroundEffect() {
        if (AssetLoader.cacheParrySprites == null || AssetLoader.cacheParrySprites.Count == 0) {
            return;
        }

        if (cachedSpriteRenderers.TryGetValue("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/Effect_ParryCounterAttack0", out var renderer) &&
            AssetLoader.cacheParrySprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
        }
    }

    private void UCSuccess() {
        if (AssetLoader.cacheParrySprites == null || AssetLoader.cacheParrySprites.Count == 0) {
            return;
        }

        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging") is { } obj) {
            if (AssetLoader.cacheParrySprites.TryGetValue("UCSuccess", out var sprite)) {
                var particleRenderer = obj.GetComponent<ParticleSystemRenderer>();
                particleRenderer.materials[0].SetTexture("_MainTex", sprite.texture);
                obj.GetComponent<ParticleSystem>().startColor = UCSuccessColor.Value;
            }
        }
    }

    private void UCCharging() {
        if (AssetLoader.cacheParrySprites == null || AssetLoader.cacheParrySprites.Count == 0) {
            return;
        }

        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging C") is { } obj) {
            if (AssetLoader.cacheParrySprites.TryGetValue("UCCharging", out var sprite)) {
                var particleRenderer = obj.GetComponent<ParticleSystemRenderer>();
                particleRenderer.materials[0].SetTexture("_MainTex", sprite.texture);
                obj.GetComponent<ParticleSystem>().startColor = UCChargingColor.Value;
            }
        }
    }

    private void TalismanBall() {
        if (AssetLoader.cacheTalismanBallSprites == null || AssetLoader.cacheTalismanBallSprites.Count == 0) {
            return;
        }

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
        if (AssetLoader.cacheOnlyOneSprites is { Count: > 0 } && Player.i?.PlayerSprite is not null) {
            spriteChangeTimer += Time.deltaTime;

            if (AssetLoader.cacheOnlyOneSprites.TryGetValue(currentSpriteIndex.ToString(), out var sprite)) {
                Player.i.PlayerSprite.sprite = sprite;

                if (spriteChangeTimer >= spriteDelaySecond.Value) {
                    currentSpriteIndex++;
                    if (!AssetLoader.cacheOnlyOneSprites.ContainsKey(currentSpriteIndex.ToString())) {
                        currentSpriteIndex = 1;
                    }
                    spriteChangeTimer = 0f;
                }
            }
        } else if (AssetLoader.cachePlayerSprites is { Count: > 0 } && Player.i?.PlayerSprite?.sprite is not null) {
            if (AssetLoader.cachePlayerSprites.TryGetValue(Player.i.PlayerSprite.sprite.name, out var sprite)) {
                Player.i.PlayerSprite.sprite = sprite;
            }
        }
    }

    private void InitializeBowSprites() {
        if (AssetLoader.cacheBowSprites == null || AssetLoader.cacheBowSprites.Count == 0) {
            return;
        }

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

    public static void UpdateBowSprite(GameObject parent, string childPath) {
        if (parent == null) return;

        var renderer = parent.transform.Find(childPath)?.GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null && AssetLoader.cacheBowSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
        }
    }

    private void Sword() {
        if (AssetLoader.cacheSwordSprites == null || AssetLoader.cacheSwordSprites.Count == 0) {
            return;
        }

        foreach (var path in swordSpritePaths) {
            if (cachedSpriteRenderers.TryGetValue(path, out var renderer) &&
                renderer != null &&
                renderer.sprite != null &&
                AssetLoader.cacheSwordSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
                renderer.sprite = sprite;
            }
        }
    }

    private void SwordOnce() {
        if (AssetLoader.cacheSwordSprites == null || AssetLoader.cacheSwordSprites.Count == 0) {
            return;
        }

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
        if (AssetLoader.cacheFooSprites == null || AssetLoader.cacheFooSprites.Count == 0) {
            return;
        }

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

    private void UpdateHeartSprite() {
        if (AssetLoader.cacheUISprites == null || AssetLoader.cacheUISprites.Count == 0) {
            return;
        }

        string heartPath = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/HUD_Heart/Heart";
        if (cachedSpriteRenderers.TryGetValue(heartPath, out var renderer) && renderer.sprite != null
            && AssetLoader.cacheUISprites.TryGetValue(renderer.sprite.name, out var sprtie)) {
            renderer.sprite = sprtie;
        }
    }

    private void UpdateArrowIcon() {
        if (AssetLoader.cacheUISprites == null || AssetLoader.cacheUISprites.Count == 0) {
            return;
        }

        string arrowIconPath = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftDown/Bow UI Area/ItemSelection/CurrentItemPanel spr/ItemPic";
        if (cachedSpriteRenderers.TryGetValue(arrowIconPath, out var renderer) && renderer.sprite != null
            && AssetLoader.cacheUISprites.TryGetValue(renderer.sprite.name, out var sprtie)) {
            renderer.sprite = sprtie;
        }
    }

    private void UpdateButterflySprite() {
        if (AssetLoader.cacheUISprites == null || AssetLoader.cacheUISprites.Count == 0) {
            return;
        }

        var butterflyPaths = new[] {
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/RightDown/Butterfly_UIHintPanel/TESLA BUTTERFlY/Butterfly/Butterfly",
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/RightDown/Butterfly_UIHintPanel/TESLA BUTTERFlY/Butterfly/ButterflyIcon Color",
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/RightDown/Butterfly_UIHintPanel/TESLA BUTTERFlY/Butterfly/ButterflyIcon"
        };

        foreach (var path in butterflyPaths) {
            if (cachedSpriteRenderers.TryGetValue(path, out var renderer) && renderer.sprite != null && renderer.sprite != null) {
                if (AssetLoader.cacheUISprites.TryGetValue(renderer.sprite.name, out var sprite) && renderer.sprite != null
                    && AssetLoader.cacheUISprites.TryGetValue(renderer.sprite.name, out var sprtie)) {
                    renderer.sprite = sprite;
                }
            }
        }
    }

    private void UpdateExpRing() {
        if (AssetLoader.cacheUISprites == null || AssetLoader.cacheUISprites.Count == 0) {
            return;
        }

        string expRingOuter = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreB(ExpUILogic)";
        if (cachedSpriteRenderers.TryGetValue(expRingOuter, out var renderer)) {
            renderer.color = new Color(1f, 0.4634513f, 0.6132076f, 0.5f);
        }

        string expRingInner = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreB(ExpUILogic)/BarFill";
        if (cachedSpriteRenderers.TryGetValue(expRingInner, out var renderer2)) {
            renderer2.color = new Color(0.5f, 1f, 0.5f, 0.5f);
        }
    }

    private void UpdateHpBar() {
        if (AssetLoader.cacheUISprites == null || AssetLoader.cacheUISprites.Count == 0) {
            return;
        }

        string normalHp = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/HealthBarBase/HealthBar/BG renderer/Health";
        if (cachedSpriteRenderers.TryGetValue(normalHp, out var renderer)) {
            renderer.color = new Color(0.75f, 0.5f, 0.75f, 1f);
        }

        string internalHp = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/HealthBarBase/HealthBar/BG renderer/RecoverableHealth";
        if (cachedSpriteRenderers.TryGetValue(internalHp, out var renderer2)) {
            renderer2.color = new Color(0.1f, 0.1f, 0.8f, 1f);
        }
    }

    private void UpdatePotion() {
        if (AssetLoader.cacheUISprites == null || AssetLoader.cacheUISprites.Count == 0) {
            return;
        }

        const string basePath = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/Potion/";

        // Cache sprites to avoid repeated dictionary lookups
        Sprite bloodEmptySprite = AssetLoader.cacheUISprites.TryGetValue("Icon_BloodEmpty", out var emptySprite) ? emptySprite : null;
        Sprite bloodSprite = AssetLoader.cacheUISprites.TryGetValue("Icon_Blood", out var sprite) ? sprite : null;

        if (bloodEmptySprite == null && bloodSprite == null) {
            return;
        }

        for (int i = 0; i <= 7; i++) {
            // Construct path once per iteration
            string potionPath = i == 0 ? $"{basePath}PotionIMG" : $"{basePath}PotionIMG ({i})";
            string potionChildPath = $"{potionPath}/GameObject";

            // Find parent GameObject and update its SpriteRenderer
            GameObject potionParent = GameObject.Find(potionPath);
            if (potionParent != null) {
                SpriteRenderer parentRenderer = potionParent.GetComponent<SpriteRenderer>();
                if (parentRenderer != null) {
                    parentRenderer.sprite = bloodEmptySprite;
                }
            } else {
                continue;
            }

            // Find child GameObject and update its SpriteRenderer
            GameObject potionChild = GameObject.Find(potionChildPath);
            if (potionChild != null) {
                SpriteRenderer childRenderer = potionChild.GetComponent<SpriteRenderer>();
                if (childRenderer != null) {
                    childRenderer.sprite = bloodSprite;
                }
            }
        }
    }

    private void Reload() {
        InitializeAssets();
        ChangeMenuLogo();
        ChangeUIChiBall();
        ImPerfectParry();
        SwordOnce();
        InitializeBowSprites();
        UpdateExpRing();
        UpdateHpBar();
        UpdatePotion();
    }

    private void OnDestroy() {
        harmony.UnpatchSelf();
    }
}