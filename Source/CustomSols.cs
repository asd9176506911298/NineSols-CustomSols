using Battlehub.RTHandles;
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
    public ConfigEntry<bool> isToastPlayerDummySprite = null!;
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

    private static string playerSpriteName = "";
    private static string playerDummySpriteName = "";

    public static SpriteRenderer? CurrentDummyRenderer = null;
    public static SpriteRenderer? CurrentRootDummyRenderer = null;
    public static SpriteRenderer? CurrentElevatorDummyRenderer = null;

    private ParticleSystemRenderer? _cachedUCSuccess;
    private ParticleSystemRenderer? _cachedUCCharging;
    private List<SpriteRenderer> _cachedTalismanBalls = new List<SpriteRenderer>();
    private bool _talismanSearched = false;

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
        ChangeUIChiBall();
        ImPerfectParry();
        SwordOnce();
        InitializeBowSprites();
        UpdateExpRing();
        UpdateHpBar();
        UpdatePotion();
        UpdateLineA();
        UpdateEightGua();
        UpdateArrowLine();
        UpdateRightLine();
        UpdateArrowBullet();
        YingZhaoOnce();
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
        YingZhao();

        //UI
        UpdateHeartSprite();
        UpdateArrowIcon();
        UpdateArrowColor();
        UpdateButterflySprite();

        // 玩家部分
        if (isToastPlayerSprite.Value) {
            var s = Player.i?.PlayerSprite?.sprite;
            if (s != null) CheckAndToast(s, ref playerSpriteName);
        }

        // Dummy 部分
        if (isToastPlayerDummySprite.Value) {
            var s = CurrentDummyRenderer?.sprite;
            if (s != null) CheckAndToast(s, ref playerDummySpriteName, "Dummy: ");
        }
    }

    private void CheckAndToast(Sprite sprite, ref string cachedName, string prefix = "") {
        // 1. 安全檢查：如果 sprite 為空則不執行
        if (sprite == null) return;

        // 2. 獲取名稱（避免重複存取 .name 屬性）
        string currentName = sprite.name;

        // 3. 只有在名稱真正改變時才執行邏輯
        if (cachedName != currentName) {
            cachedName = currentName;
            ToastManager.Toast($"{prefix}{currentName}");
        }
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
        _cachedUCSuccess = null;
        _cachedUCCharging = null;
        _cachedTalismanBalls.Clear();
        _talismanSearched = false;

        CacheSpriteRenderers();
        ChangeMenuLogo();
        ChangeUIChiBall();
        ImPerfectParry();
        SwordOnce();
        InitializeBowSprites();
        UpdateExpRing();
        UpdateHpBar();
        UpdatePotion();
        UpdateLineA();
        UpdateEightGua();
        UpdateArrowLine();
        UpdateRightLine();
        UpdateArrowBullet();
        YingZhaoOnce();

        arrowInit = false;
        arrowInit2 = false;
    }

    private void SetupConfig() {
        openFolder = Config.Bind("Folder", "Open CustomSols Folder", false, "");
        isToastPlayerSprite = Config.Bind("", "Toast Player Sprite Name", false, "");
        isToastPlayerDummySprite = Config.Bind("", "Toast Player Dummy Sprite Name", false, "");
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
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (6)/BG/Rotate/Fill",
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
        if (AssetLoader.cacheParrySprites == null) return;

        // 如果變數是空的，才去找一次
        if (_cachedUCSuccess == null) {
            var obj = GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging");
            if (obj != null) {
                _cachedUCSuccess = obj.GetComponent<ParticleSystemRenderer>();
                obj.GetComponent<ParticleSystem>().startColor = UCSuccessColor.Value;
            }
        }

        // 之後直接用變數
        if (_cachedUCSuccess != null && AssetLoader.cacheParrySprites.TryGetValue("UCSuccess", out var sprite)) {
            _cachedUCSuccess.materials[0].SetTexture("_MainTex", sprite.texture);
        }
    }

    private void UCCharging() {
        if (AssetLoader.cacheParrySprites == null) return;

        // 如果變數是空的，才去找一次
        if (_cachedUCCharging == null) {
            var obj = GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging C");
            if (obj != null) {
                _cachedUCCharging = obj.GetComponent<ParticleSystemRenderer>();
                obj.GetComponent<ParticleSystem>().startColor = UCChargingColor.Value;
            }
        }

        // 之後直接用變數
        if (_cachedUCCharging != null && AssetLoader.cacheParrySprites.TryGetValue("UCCharging", out var sprite)) {
            _cachedUCCharging.materials[0].SetTexture("_MainTex", sprite.texture);
        }
    }

    private void TalismanBall() {
        if (AssetLoader.cacheTalismanBallSprites == null) return;

        // 只找一次並存到 List 裡
        if (!_talismanSearched) {
            var foo = GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo");
            if (foo != null && foo.activeSelf) {
                for (int i = 1; i <= 5; i++) {
                    var ball = foo.transform.Find($"FooDots/D{i}/FooDot ({i})/JENG/Ball")?.GetComponent<SpriteRenderer>();
                    if (ball != null) _cachedTalismanBalls.Add(ball);
                }
                _talismanSearched = true;
            }
        }

        // 之後迴圈只跑已經找到的 List
        foreach (var ball in _cachedTalismanBalls) {
            if (ball != null && ball.sprite != null &&
                AssetLoader.cacheTalismanBallSprites.TryGetValue(ball.sprite.name, out var sprite)) {
                ball.sprite = sprite;
            }
        }
    }

    private void PlayerSprite() {
        // 1. 檢查 Cache 是否存在
        var cacheOnly = AssetLoader.cacheOnlyOneSprites;
        var cachePlayer = AssetLoader.cachePlayerSprites;

        if (cacheOnly is { Count: > 0 } && Player.i?.PlayerSprite is not null) {
            spriteChangeTimer += Time.deltaTime;

            if (cacheOnly.TryGetValue(currentSpriteIndex.ToString(), out var sprite)) {
                Player.i.PlayerSprite.sprite = sprite;

                if (spriteChangeTimer >= spriteDelaySecond.Value) {
                    currentSpriteIndex++;
                    if (!cacheOnly.ContainsKey(currentSpriteIndex.ToString())) {
                        currentSpriteIndex = 1;
                    }
                    spriteChangeTimer = 0f;
                }
            }
        } else if (cachePlayer is { Count: > 0 } && Player.i?.PlayerSprite?.sprite is not null) {
            // 安全獲取玩家當前 Sprite 名稱
            string currentName = Player.i.PlayerSprite.sprite.name;
            if (cachePlayer.TryGetValue(currentName, out var sprite)) {
                Player.i.PlayerSprite.sprite = sprite;
            }

            // --- 修正重點：使用 s?.name 並檢查 null ---

            // Dummy 渲染器處理
            if (CurrentDummyRenderer != null && CurrentDummyRenderer.sprite != null) {
                if (cachePlayer.TryGetValue(CurrentDummyRenderer.sprite.name, out var cachedSprite)) {
                    CurrentDummyRenderer.sprite = cachedSprite;
                }
            }

            // RootDummy 渲染器處理
            if (CurrentRootDummyRenderer != null && CurrentRootDummyRenderer.sprite != null) {
                if (cachePlayer.TryGetValue(CurrentRootDummyRenderer.sprite.name, out var cachedSprite2)) {
                    CurrentRootDummyRenderer.sprite = cachedSprite2;
                }
            }

            // ElevatorDummy 渲染器處理
            if (CurrentElevatorDummyRenderer != null && CurrentElevatorDummyRenderer.sprite != null) {
                if (cachePlayer.TryGetValue(CurrentElevatorDummyRenderer.sprite.name, out var cachedSprite3)) {
                    CurrentElevatorDummyRenderer.sprite = cachedSprite3;
                }
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
                if(go.name.StartsWith("NormalArrow Shoot 穿雲 Lv1"))
                    go.transform.Find("光束").localPosition = AssetLoader.NormalArrowLv1Pos.Value;
                if (go.name.StartsWith("NormalArrow Shoot 穿雲 Lv2"))
                    go.transform.Find("光束").localPosition = AssetLoader.NormalArrowLv2Pos.Value;
                if (go.name.StartsWith("NormalArrow Shoot 穿雲 Lv3"))
                    go.transform.Find("光束").localPosition = AssetLoader.NormalArrowLv3Pos.Value;

                UpdateBowSprite(go, "光束");
                UpdateBowSprite(go, "NormalArrow");
                UpdateBowSprite(go, "NormalArrow/ChasingArrowLight");
            } else if (go.name.StartsWith("ExplodingArrow Shooter 爆破發射器 Lv")) {
                UpdateBowSprite(go, "Exploding Arrow/ExplodingArrow/ExplodingArrow");
                UpdateBowSprite(go, "Exploding Arrow/ExplodingArrow/ChasingArrowLight");
                UpdateBowSprite(go, "Exploding Arrow/EnergyBall/Core");
            } else if (go.name.StartsWith("Explosion Damage 爆破箭 閃電 lv")) {
                //UpdateBowSprite(go, "ATTACK/Core");
                var renderer = go.transform.Find("ATTACK/Core")?.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null && AssetLoader.cacheBowSprites.TryGetValue("ExplosionCenter", out var sprite)) {
                    renderer.sprite = sprite;
                }
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
            if (GameObject.Find($"GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharged/{path}") != null &&
                AssetLoader.cacheSwordSprites.TryGetValue("FooSmokeGlow", out var sprite)) {
                GameObject.Find($"GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharged/{path}").GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", sprite.texture);
            }
        }

        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharging/P_hit") != null &&
            AssetLoader.cacheSwordSprites.TryGetValue("bubbletrail", out var sprite2)) {
            GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharging/P_hit").GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", sprite2.texture);
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

    private void YingZhaoOnce() {
        if (cachedSpriteRenderers.TryGetValue("A2_S5_ BossHorseman_GameLevel/Room/StealthGameMonster_SpearHorseMan/MonsterCore/Animator(Proxy)/Animator/SpearHorseMan/DDS/SpearHorseMan_HorseBodyD/SpearHorseMan_HorseBody_A", out var renderer3) &&
           AssetLoader.cacheYingZhaoSprites.TryGetValue(renderer3.sprite.name, out var sprite3)) {
            renderer3.sprite = sprite3;
        }

        if (cachedSpriteRenderers.TryGetValue("A2_S5_ BossHorseman_GameLevel/Room/StealthGameMonster_SpearHorseMan/MonsterCore/Animator(Proxy)/Animator/SpearHorseMan/DDS/SpearHorseMan_HorseBodyD/SpearHorseMan_HorseBody_A/bone_1/bone_2/SpearHorseMan_HorseBody_B", out var renderer4) &&

           AssetLoader.cacheYingZhaoSprites.TryGetValue(renderer4.sprite.name, out var sprite4)) {
            renderer4.sprite = sprite4;
        }
    }

    private void YingZhao() {
        
        if (AssetLoader.cacheYingZhaoSprites == null || AssetLoader.cacheYingZhaoSprites.Count == 0) {
            return;
        }

        //ToastManager.Toast(GameObject.Find("A2_S5_ BossHorseman_GameLevel/Room/StealthGameMonster_SpearHorseMan/MonsterCore/Animator(Proxy)/Animator/SpearHorseMan/DDS/Body_Attack_D (F)").GetComponent<SpriteRenderer>().sprite.name);
        if (cachedSpriteRenderers.TryGetValue("A2_S5_ BossHorseman_GameLevel/Room/StealthGameMonster_SpearHorseMan/MonsterCore/Animator(Proxy)/Animator/SpearHorseMan/DDS/Body_Attack_D (F)", out var renderer) &&

           AssetLoader.cacheYingZhaoSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
                renderer.sprite = sprite;
        }

        if (cachedSpriteRenderers.TryGetValue("A2_S5_ BossHorseman_GameLevel/Room/StealthGameMonster_SpearHorseMan/MonsterCore/Animator(Proxy)/Animator/SpearHorseMan/DDS/SpearHorseMan_HorseBodyD", out var renderer2) &&

           AssetLoader.cacheYingZhaoSprites.TryGetValue(renderer2.sprite.name, out var sprite2)) {
            renderer2.sprite = sprite2;
        }

        if (cachedSpriteRenderers.TryGetValue("A2_S5_ BossHorseman_GameLevel/Room/StealthGameMonster_SpearHorseMan/MonsterCore/Animator(Proxy)/Animator/SpearHorseMan/DDS/EFFECT/SlashA", out var renderer3) &&

           AssetLoader.cacheYingZhaoSprites.TryGetValue(renderer3.sprite.name, out var sprite3)) {
            renderer3.sprite = sprite3;
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

    private void UpdateArrowColor() {
        if (AssetLoader.cacheUISprites == null || AssetLoader.cacheUISprites.Count == 0) {
            return;
        }
   
        if (cachedSpriteRenderers.TryGetValue("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftDown/Bow UI Area/ItemSelection/CurrentItemPanel spr/Glow", out var renderer3)) {
            if(AssetLoader.ArrowGlowColor != null)
                renderer3.color = AssetLoader.ArrowGlowColor.Value;
        }

        const string basePath = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftDown/Bow UI Area/RageUI renderer/slots/";

        Sprite RageBarFrameSprite = AssetLoader.cacheUISprites.TryGetValue("BlockOutline", out var RageBarFrame) ? RageBarFrame : null;
        Sprite RageBarSprite = AssetLoader.cacheUISprites.TryGetValue("Block", out var RageBar) ? RageBar : null;

        if (RageBarFrame == null && RageBar == null) {
            return;
        }


        for (int i = 0; i <= 7; i++) {
            // Construct path once per iteration
            string RagePath = i == 0 ? $"{basePath}RagePart_spr" : $"{basePath}RagePart_spr ({i})";
            string RageBarFramePath = $"{RagePath}/RageBar Frame";
            string RageBarPath = $"{RagePath}/RageBar";
            //ToastManager.Toast(AssetLoader.RageBarFrameColor.Value);
            // RageBarFrame
            if (cachedSpriteRenderers.TryGetValue(RageBarFramePath, out var renderer)) {
                if (AssetLoader.RageBarFrameColor != null)
                    renderer.color = AssetLoader.RageBarFrameColor.Value;
            }

            // RageBar
            if (cachedSpriteRenderers.TryGetValue(RageBarPath, out var renderer2)) {
                if (AssetLoader.RageBarColor != null)
                    renderer2.color = AssetLoader.RageBarColor.Value;
            }
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
        if (AssetLoader.expRingOuterColor.HasValue && cachedSpriteRenderers.TryGetValue(expRingOuter, out var renderer)) {
            if (AssetLoader.expRingOuterColor != null)
                renderer.color = AssetLoader.expRingOuterColor.Value;
        }

        string expRingInner = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreB(ExpUILogic)/BarFill";
        if (AssetLoader.expRingInnerColor.HasValue && cachedSpriteRenderers.TryGetValue(expRingInner, out var renderer2)) {
            if (AssetLoader.expRingInnerColor != null)
                renderer2.color = AssetLoader.expRingInnerColor.Value;
        }
    }

    private void UpdateHpBar() {
        if (AssetLoader.cacheUISprites == null || AssetLoader.cacheUISprites.Count == 0) {
            return;
        }

        string normalHp = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/HealthBarBase/HealthBar/BG renderer/Health";
        if (AssetLoader.normalHpColor.HasValue && cachedSpriteRenderers.TryGetValue(normalHp, out var renderer)) {
            if (AssetLoader.normalHpColor != null)
                renderer.color = AssetLoader.normalHpColor.Value;
        }


        string internalHp = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/HealthBarBase/HealthBar/BG renderer/RecoverableHealth";
        if (AssetLoader.internalHpColor.HasValue && cachedSpriteRenderers.TryGetValue(internalHp, out var renderer2)) {
            if (AssetLoader.internalHpColor != null)
                renderer2.color = AssetLoader.internalHpColor.Value;
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

    //Chi Ball Left Line
    private void UpdateLineA() {
        string path = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/LineA";

        if (cachedSpriteRenderers.TryGetValue(path, out var renderer)) {
            if (AssetLoader.cacheUISprites.TryGetValue("ChiBallLeftLine", out var sprite))
                renderer.sprite = sprite;

            if (AssetLoader.ChiBallLeftLineColor != null)
                renderer.color = AssetLoader.ChiBallLeftLineColor.Value;
        }
    }

    //八卦
    private void UpdateEightGua() {
        string pathC = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreC";
        if (cachedSpriteRenderers.TryGetValue(pathC, out var rendererC)) {
            if (AssetLoader.cacheUISprites.TryGetValue("CoreC", out var sprite)) rendererC.sprite = sprite;
            if (AssetLoader.CoreCColor != null) rendererC.color = AssetLoader.CoreCColor.Value;
        }

        string pathD = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreD";
        if (cachedSpriteRenderers.TryGetValue(pathD, out var rendererD)) {
            if (AssetLoader.cacheUISprites.TryGetValue("CoreD", out var sprite2)) rendererD.sprite = sprite2;
            if (AssetLoader.CoreDColor != null) rendererD.color = AssetLoader.CoreDColor.Value;
        }
    }

    private void UpdateArrowLine() {
        string path = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftDown/Bow UI Area/RageUI renderer/ArrowLineB (1)";
        if (cachedSpriteRenderers.TryGetValue(path, out var renderer)) {
            if (AssetLoader.cacheUISprites.TryGetValue("ArrowLineA", out var sprite)) renderer.sprite = sprite;
            if (AssetLoader.ArrowLineBColor != null) renderer.color = AssetLoader.ArrowLineBColor.Value;
        }
    }

    //Butterfly Right Line
    private void UpdateRightLine() {
        string path = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/RightDown/Butterfly_UIHintPanel/LineA";
        if (cachedSpriteRenderers.TryGetValue(path, out var renderer)) {
            if (AssetLoader.cacheUISprites.TryGetValue("ButterflyRightLine", out var sprite)) renderer.sprite = sprite;
            if (AssetLoader.ButterflyRightLineColor != null) renderer.color = AssetLoader.ButterflyRightLineColor.Value;
        }
    }

    private void UpdateArrowBullet() {
        // 取得 Sprite 資源
        Sprite RageBarFrameSprite = AssetLoader.cacheUISprites.TryGetValue("BlockOutline", out var f) ? f : null;
        Sprite RageBarSprite = AssetLoader.cacheUISprites.TryGetValue("Block", out var b) ? b : null;

        // 如果找不到圖就不做了，省效能
        if (RageBarFrameSprite == null && RageBarSprite == null) return;

        // 定義基本路徑
        const string basePath = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftDown/Bow UI Area/RageUI renderer/slots/";

        for (int i = 0; i <= 7; i++) {
            // 拼湊路徑字串
            string RagePath = i == 0 ? $"{basePath}RagePart_spr" : $"{basePath}RagePart_spr ({i})";
            string RageBarFramePath = $"{RagePath}/RageBar Frame";
            string RageBarPath = $"{RagePath}/RageBar";

            // 直接從字典拿，不要 Find
            if (cachedSpriteRenderers.TryGetValue(RageBarFramePath, out var r1)) r1.sprite = RageBarFrameSprite;
            if (cachedSpriteRenderers.TryGetValue(RageBarPath, out var r2)) r2.sprite = RageBarSprite;
        }
    }

    private void Reload() {
        _cachedUCSuccess = null;
        _cachedUCCharging = null;
        _cachedTalismanBalls.Clear();
        _talismanSearched = false;

        InitializeAssets();
        ChangeMenuLogo();
        ChangeUIChiBall();
        ImPerfectParry();
        SwordOnce();
        InitializeBowSprites();
        UpdateExpRing();
        UpdateHpBar();
        UpdatePotion();
        UpdateLineA();
        UpdateEightGua();
        UpdateArrowLine();
        UpdateRightLine();
        UpdateArrowBullet();
        YingZhaoOnce();
    }

    private void OnDestroy() {
        harmony.UnpatchSelf();
    }
}