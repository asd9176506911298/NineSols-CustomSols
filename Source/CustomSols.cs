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

    // 預先快取屬性 ID，提升效能
    private static readonly int TintColorID = Shader.PropertyToID("_TintColor");
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

    public static SpriteRenderer? CurrentDummyRenderer = null;

    public static List<SpriteRenderer> DummyRenderers = new List<SpriteRenderer>();

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
        if (currSkinFolder == "Default") return;
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
        AirParryColor();
    }

    private void LateUpdate() {
        if (!isAssetsLoaded) return;

        //Some same sprite name need execute first
        RendererReplace();

        PlayerSprite();
        AirParry();
        UCParryColor();
        PerfectParry();
        Dash();
        AirJump();
        UCAroundEffect();
        UCSuccess();
        UCCharging();
        TalismanBall();
        Foo();
        Sword();
        //YingZhao();
        
        //UI
        UpdateHeartSprite(); // Need
        UpdateArrowIcon(); // Need
        UpdateArrowColor(); //Need
        UpdateButterflySprite(); //Need

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
        AirParryColor();

        arrowInit = false;
        arrowInit2 = false;
    }

    private void SetupConfig() {
        openFolder = Config.Bind("Folder", "Open CustomSols Folder", false, "");
        isToastPlayerSprite = Config.Bind("", "Toast Player Sprite Name", false, "");
        isToastPlayerDummySprite = Config.Bind("", "Toast Player Dummy Sprite Name", false, "");
        spriteDelaySecond = Config.Bind("Sprite Delay Second", "PlayerSpriteAllUseThis Sprite Delay Second", 0.12f, "");
        //UCChargingColor = Config.Bind("Color", "UCCharging Color", new Color(1f, 0.837f, 0f, 1f), "");
        //UCSuccessColor = Config.Bind("Color", "UCSuccess Color", new Color(1f, 0.718f, 1f, 1f), "");
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

            // 如果這個路徑已經有東西了（重複了）
            if (cachedSpriteRenderers.ContainsKey(path)) {
                // 存成另一個 Key，例如 "路徑_2"
                cachedSpriteRenderers[path + "_2"] = renderer;
            } else {
                cachedSpriteRenderers[path] = renderer;
            }
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

    private void RendererReplace() {
        CustomSols.DummyRenderers.RemoveAll(r => r == null);

        foreach (var renderer in CustomSols.DummyRenderers) {
            if (renderer.sprite != null) {
                // 根據目前的 Sprite 名稱從快取中找尋對應的新 Sprite
                if (AssetLoader.all.TryGetValue(renderer.sprite.name, out var cachedSprite)) {
                    renderer.sprite = cachedSprite;
                }
            }
        }
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

        string basePath = "YeeParryEffectAccurate_Green(Clone)/ParrySparkAccurate0";

        // 1. 處理第一個原始路徑 (無序號)
        if (cachedSpriteRenderers.TryGetValue(basePath, out var renderer1)) {
            ApplyParrySprite(renderer1);
        }

        // 2. 自動處理後續所有編號 (_2, _3, _4...)
        int count = 2;
        while (cachedSpriteRenderers.TryGetValue(basePath + "_" + count, out var nextRenderer)) {
            ApplyParrySprite(nextRenderer);
            count++;
        }
    }

    // 提取共用邏輯，讓程式碼更乾淨
    private void ApplyParrySprite(SpriteRenderer renderer) {
        if (AssetLoader.cacheParrySprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
            // 根據玩家朝向翻轉特效
            float yRotation = (Player.i.Facing == Facings.Left) ? 180f : 0f;
            renderer.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }

    private void AirParry() {
        if (AssetLoader.cachePlayerSprites == null || AssetLoader.cachePlayerSprites.Count == 0) {
            return;
        }

        if (cachedSpriteRenderers.TryGetValue("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/AbilityChecker/Ability 識破/Effect_TAICHIParry_Air/Effect_JumpFeet", out var renderer) &&
            AssetLoader.cachePlayerSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
        }
    }

    private void Dash() {
        // 檢查總字典 all (包含所有圖片) 是否有資料
        if (AssetLoader.all == null || AssetLoader.all.Count == 0) return;

        string path = "Effect_Roll Dodge AfterImage(Clone)/Effect_HoHoYee_AirJump0";

        // 1. 處理第一個 (原始路徑)
        if (cachedSpriteRenderers.TryGetValue(path, out var renderer1)) {
            ApplyToRenderer(renderer1);
        }

        // 2. 自動處理後續所有編號 (_2, _3, _4...)
        int count = 2;
        while (cachedSpriteRenderers.TryGetValue(path + "_" + count, out var nextRenderer)) {
            ApplyToRenderer(nextRenderer);
            count++;
        }
    }

    // 寫個小方法避免重寫兩次邏輯
    private void ApplyToRenderer(SpriteRenderer renderer) {
        // 1. 關鍵修正：必須先檢查 renderer 本身是否為 null (包含已被銷毀的情況)
        if (renderer == null) return;

        // 2. 檢查是否有 Sprite（有些特效消失前會先清空 Sprite）
        if (renderer.sprite == null) return;

        // 3. 確保 AssetLoader.all 已經初始化
        if (AssetLoader.all == null) return;

        // 4. 進行替換
        if (AssetLoader.all.TryGetValue(renderer.sprite.name, out var customSprite)) {
            if (renderer.sprite != customSprite) {
                renderer.sprite = customSprite;
            }
        }
    }

    //Sometime will not work cause object dynamic create cache didn't cache it
    private void AirJump() {
        if (AssetLoader.cachePlayerSprites == null || AssetLoader.cachePlayerSprites.Count == 0) {
            return;
        }

        string basePath = "Effect_AirJump(Clone)/Effect_HoHoYee_AirJump0";

        // 1. 處理原始路徑
        if (cachedSpriteRenderers.TryGetValue(basePath, out var renderer1)) {
            ApplyAirJumpSprite(renderer1);
        }

        // 2. 自動處理後續所有編號 (_2, _3, _4...)
        int count = 2;
        while (cachedSpriteRenderers.TryGetValue(basePath + "_" + count, out var nextRenderer)) {
            ApplyAirJumpSprite(nextRenderer);
            count++;
        }
    }

    private void ApplyAirJumpSprite(SpriteRenderer renderer) {
        if (AssetLoader.cachePlayerSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
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
        UpdateParticleEffect(
            "GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging",
            ref _cachedUCSuccess,
            AssetLoader.UCSuccess1Color,
            AssetLoader.UCSuccess2Color,
            "UCSuccess",
            "UCSuccess2"
        );
    }

    private void UCCharging() {
        UpdateParticleEffect(
            "GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging C",
            ref _cachedUCCharging,
            AssetLoader.UCCharging1Color,
            AssetLoader.UCCharging2Color,
            "UCCharging",
            "UCCharging2"
        );
    }

    private void UpdateParticleEffect(string path, ref ParticleSystemRenderer cachedRenderer, Color? color1, Color? color2, string spriteKey1, string spriteKey2) {
        if (AssetLoader.cacheParrySprites == null) return;

        // 1. 初始化與快取尋找
        if (cachedRenderer == null) {
            var obj = GameObject.Find(path);
            if (obj == null) return;

            cachedRenderer = obj.GetComponent<ParticleSystemRenderer>();
            var ps = obj.GetComponent<ParticleSystem>();

            // 設定第一個顏色 (Particle Main 模組)
            if (color1.HasValue && ps != null) {
                var main = ps.main;
                main.startColor = color1.Value;
            }

            // 設定材質顏色 (注意：這裡會產生 Material Instance)
            if (color2.HasValue && cachedRenderer.sharedMaterials.Length > 1) {
                // 使用 materials[i] 會建立 Instance，我們只在必要時做一次
                cachedRenderer.materials[1].SetColor(TintColorID, color2.Value);
            }
        }

        // 2. 更新貼圖 (使用共用邏輯減少 GC)
        if (cachedRenderer != null) {
            // 一次性取得所有材質，避免反覆呼叫 .materials 產生副本
            Material[] currentMaterials = cachedRenderer.materials;
            bool changed = false;

            if (AssetLoader.cacheParrySprites.TryGetValue(spriteKey1, out var s1)) {
                currentMaterials[0].SetTexture(MainTexID, s1.texture);
                changed = true;
            }

            if (currentMaterials.Length > 1 && AssetLoader.cacheParrySprites.TryGetValue(spriteKey2, out var s2)) {
                currentMaterials[1].SetTexture(MainTexID, s2.texture);
                changed = true;
            }

            // 如果有變動，可以選擇不寫回，因為 currentMaterials[i] 已經是指向實例
            // 但為了確保引用正確，通常操作完畢後不需額外動作，除非是全新的陣列
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
        if (AssetLoader.cacheSwordSprites == null || AssetLoader.cacheSwordSprites.Count == 0) return;

        foreach (var path in swordSpritePaths) {
            // 1. 處理原始路徑
            if (cachedSpriteRenderers.TryGetValue(path, out var r1)) {
                ApplyToRenderer(r1);
            }

            // 2. 處理該路徑下所有編號實體 (_2, _3...)
            int count = 2;
            while (cachedSpriteRenderers.TryGetValue(path + "_" + count, out var nextR)) {
                ApplyToRenderer(nextR);
                count++;
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
            if(AssetLoader.ArrowGlowColor.HasValue)
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
                if (AssetLoader.RageBarFrameColor.HasValue)
                    renderer.color = AssetLoader.RageBarFrameColor.Value;
            }

            // RageBar
            if (cachedSpriteRenderers.TryGetValue(RageBarPath, out var renderer2)) {
                if (AssetLoader.RageBarColor.HasValue)
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
        if (AssetLoader.cacheUISprites == null) {
            return;
        }

        string expRingOuter = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreB(ExpUILogic)";
        if (AssetLoader.expRingOuterColor.HasValue && cachedSpriteRenderers.TryGetValue(expRingOuter, out var renderer)) {
            if (AssetLoader.expRingOuterColor.HasValue)
                renderer.color = AssetLoader.expRingOuterColor.Value;
        }

        string expRingInner = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreB(ExpUILogic)/BarFill";
        if (AssetLoader.expRingInnerColor.HasValue && cachedSpriteRenderers.TryGetValue(expRingInner, out var renderer2)) {
            if (AssetLoader.expRingInnerColor.HasValue)
                renderer2.color = AssetLoader.expRingInnerColor.Value;
        }
    }

    private void UpdateHpBar() {
        if (AssetLoader.cacheUISprites == null) {
            return;
        }

        string normalHp = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/HealthBarBase/HealthBar/BG renderer/Health";
        if (AssetLoader.normalHpColor.HasValue && cachedSpriteRenderers.TryGetValue(normalHp, out var renderer)) {
            ToastManager.Toast(AssetLoader.normalHpColor.Value);
            if (AssetLoader.normalHpColor.HasValue)
                renderer.color = AssetLoader.normalHpColor.Value;
        }


        string internalHp = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/HealthBarBase/HealthBar/BG renderer/RecoverableHealth";
        if (AssetLoader.internalHpColor.HasValue && cachedSpriteRenderers.TryGetValue(internalHp, out var renderer2)) {
            if (AssetLoader.internalHpColor.HasValue)
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

            if (AssetLoader.ChiBallLeftLineColor.HasValue)
                renderer.color = AssetLoader.ChiBallLeftLineColor.Value;
        }
    }

    //八卦
    private void UpdateEightGua() {
        string pathC = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreC";
        if (cachedSpriteRenderers.TryGetValue(pathC, out var rendererC)) {
            if (AssetLoader.cacheUISprites.TryGetValue("CoreC", out var sprite)) rendererC.sprite = sprite;
            if (AssetLoader.CoreCColor.HasValue) rendererC.color = AssetLoader.CoreCColor.Value;
        }

        string pathD = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreD";
        if (cachedSpriteRenderers.TryGetValue(pathD, out var rendererD)) {
            if (AssetLoader.cacheUISprites.TryGetValue("CoreD", out var sprite2)) rendererD.sprite = sprite2;
            if (AssetLoader.CoreDColor.HasValue) rendererD.color = AssetLoader.CoreDColor.Value;
        }
    }

    private void UpdateArrowLine() {
        string path = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftDown/Bow UI Area/RageUI renderer/ArrowLineB (1)";
        if (cachedSpriteRenderers.TryGetValue(path, out var renderer)) {
            if (AssetLoader.cacheUISprites.TryGetValue("ArrowLineA", out var sprite)) renderer.sprite = sprite;
            if (AssetLoader.ArrowLineBColor.HasValue) renderer.color = AssetLoader.ArrowLineBColor.Value;
        }
    }

    //Butterfly Right Line
    private void UpdateRightLine() {
        string path = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/RightDown/Butterfly_UIHintPanel/LineA";
        if (cachedSpriteRenderers.TryGetValue(path, out var renderer)) {
            if (AssetLoader.cacheUISprites.TryGetValue("ButterflyRightLine", out var sprite)) renderer.sprite = sprite;
            if (AssetLoader.ButterflyRightLineColor.HasValue) renderer.color = AssetLoader.ButterflyRightLineColor.Value;
        }
    }

    private void AirParryColor() {
        string airParry = "GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/AbilityChecker/Ability 識破/Effect_TAICHIParry_Air/YeeParryBlink/BlinkLight";
        if (AssetLoader.AirParryColor.HasValue && cachedSpriteRenderers.TryGetValue(airParry, out var renderer)) {
            if (AssetLoader.AirParryColor.HasValue)
                renderer.color = AssetLoader.AirParryColor.Value;
        }
    }

    private void UCParryColor() {
        string UCParry = "GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/YeeParryBlink/BlinkLight";
        if (AssetLoader.UCParryColor.HasValue && cachedSpriteRenderers.TryGetValue(UCParry, out var renderer)) {
            if (AssetLoader.UCParryColor.HasValue)
                renderer.color = AssetLoader.UCParryColor.Value;

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
        //YingZhaoOnce();
    }

    private void OnDestroy() {
        harmony.UnpatchSelf();
    }
}