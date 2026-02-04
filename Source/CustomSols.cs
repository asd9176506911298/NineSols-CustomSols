using Battlehub.RTHandles;
using BepInEx;
using BepInEx.Configuration;
using Com.LuisPedroFonseca.ProCamera2D;
using HarmonyLib;
using NineSolsAPI;
using NineSolsAPI.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

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
    public ConfigEntry<bool> isToastDialogue = null!;
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

    private Dictionary<string, List<SpriteRenderer>> groupedRenderers = new Dictionary<string, List<SpriteRenderer>>();

    private ParticleSystemRenderer? _cachedUCSuccess;
    private ParticleSystemRenderer? _cachedUCCharging;
    private List<SpriteRenderer> _cachedTalismanBalls = new List<SpriteRenderer>();
    private bool _talismanSearched = false;

    private Dictionary<Sprite, Sprite> _spriteMappingCache = new Dictionary<Sprite, Sprite>();
    private int _cleanupCounter = 0;

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

        _spriteMappingCache.Clear();
        _cachedUCSuccess = null;
        _cachedUCCharging = null;
        _cachedTalismanBalls.Clear();
        _talismanSearched = false;

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
        FooColorOnce();
    }

    private void FooColorOnce() {
        //if (!Input.GetKeyDown(KeyCode.F5)) return;
        string FooLight = "GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo/FOO/Foo1/Light";
        if (cachedSpriteRenderers.TryGetValue(FooLight, out var renderer)) {
            if (AssetLoader.FooLightColor.HasValue)
                renderer.color = AssetLoader.FooLightColor.Value;
        }

        string DrawFooLight = "GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo/DrawFoo/Light";
        if (cachedSpriteRenderers.TryGetValue(DrawFooLight, out var renderer2)) {
            if (AssetLoader.DrawFooLightColor.HasValue)
                renderer2.color = AssetLoader.DrawFooLightColor.Value;
        }

        if (AssetLoader.ParticlesFooColor.HasValue) {
            foreach (var PSR in FindObjectsOfType<ParticleSystemRenderer>(true)) {
                if (PSR.name == "Particles_Foo" && PSR.material.name == "YeeDrone (Instance)") {
                    PSR.material.color = AssetLoader.ParticlesFooColor.Value;
                }
            }
        }


    }

    private void LateUpdate() {
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
        FooColor();
        //YingZhao();

        //UI
        UpdateHeartSprite(); // Need
        UpdateArrowIcon(); // Need
        UpdateArrowColor(); //Need
        UpdateButterflySprite(); //Need
    }

    private void FooColor() {
        string DrawFooBottomLight = "GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo/DrawFoo/BottomLight";
        if (cachedSpriteRenderers.TryGetValue(DrawFooBottomLight, out var renderer)) {
            if (AssetLoader.DrawFooBottomLightColor.HasValue)
                renderer.color = AssetLoader.DrawFooBottomLightColor.Value;
        }

        for (int i = 1; i <= 5; i++) {
            // 1. 動態生成路徑字串
            string path = $"GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo/FooDots/D{i}/FooDot ({i})/JENG/Ball";

            // 2. 檢查快取中是否存在該路徑
            if (cachedSpriteRenderers.TryGetValue(path, out var renderer2)) {
                // 3. 取得對應的顏色
                Color? targetColor = GetColorByIndex(i);

                // 4. 只有在顏色有值（不為 null）時才賦值
                if (targetColor.HasValue) {
                    renderer2.color = targetColor.Value;
                }
            }
        }
    }

    private Color? GetColorByIndex(int index) {
        return index switch {
            1 => AssetLoader.DrawFooBallColor1,
            2 => AssetLoader.DrawFooBallColor2,
            3 => AssetLoader.DrawFooBallColor3,
            4 => AssetLoader.DrawFooBallColor4,
            5 => AssetLoader.DrawFooBallColor5,
            _ => null // 索引超出範圍時回傳 null
        };
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
        _spriteMappingCache.Clear();
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
        FooColorOnce();

        arrowInit = false;
        arrowInit2 = false;
    }

    private void SetupConfig() {
        openFolder = Config.Bind("Folder", "Open CustomSols Folder", false, "");
        isToastPlayerSprite = Config.Bind("", "Toast Player Sprite Name", false, "");
        isToastPlayerDummySprite = Config.Bind("", "Toast Player Dummy Sprite Name", false, "");
        isToastDialogue = Config.Bind("", "Toast Dialogue Character", false, "");
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
        groupedRenderers.Clear();
        cachedSpriteRenderers.Clear(); // 記得也要清空這個
        var renderers = FindObjectsOfType<SpriteRenderer>(true);

        foreach (var renderer in renderers) {
            if (renderer == null) continue;

            // 維持 DummyRenderers 邏輯
            if (!CustomSols.DummyRenderers.Contains(renderer)) {
                CustomSols.DummyRenderers.Add(renderer);
            }

            string path = GetGameObjectPath(renderer.gameObject);

            // 如果路徑還沒存在，建立新清單
            if (!groupedRenderers.ContainsKey(path)) {
                groupedRenderers[path] = new List<SpriteRenderer>();
            }
            groupedRenderers[path].Add(renderer);

            // 2. 填充單一快取 (用於 UI 函數，確保功能不失效)
            string finalPath = path;
            int suffix = 2;
            while (cachedSpriteRenderers.ContainsKey(finalPath)) {
                finalPath = $"{path}_{suffix}";
                suffix++;
            }
            cachedSpriteRenderers[finalPath] = renderer;
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
        // 優化 1：不要每幀都 RemoveAll。每 100 幀清理一次無效引用即可
        _cleanupCounter++;
        if (_cleanupCounter > 100) {
            CustomSols.DummyRenderers.RemoveAll(r => r == null);
            _cleanupCounter = 0;
        }

        foreach (var renderer in CustomSols.DummyRenderers) {
            // 快速檢查
            if (renderer == null) continue;

            Sprite currentSprite = renderer.sprite;
            if (currentSprite == null) continue;

            // 優化 2：使用 Sprite 引用作為 Key，避免存取 .name (產生成對的字串垃圾)
            // 如果這個 Sprite 已經在我們的「已處理緩存」中
            if (_spriteMappingCache.TryGetValue(currentSprite, out var cachedSprite)) {
                // 如果當前 renderer 的 sprite 不是目標 sprite，才賦值
                if (currentSprite != cachedSprite) {
                    renderer.sprite = cachedSprite;
                }
                continue;
            }

            // 優化 3：如果緩存裡沒有，才進行耗時的字串比對與字典查尋
            string spriteName = currentSprite.name; // 這裡才會產生一次字串分配
            if (AssetLoader.all.TryGetValue(spriteName, out var newSprite)) {
                // 存入引用緩存，下次同一個 Sprite 就不會再觸發 .name 分配
                _spriteMappingCache[currentSprite] = newSprite;
                _spriteMappingCache[newSprite] = newSprite; // 防止重複處理
                renderer.sprite = newSprite;
            } else {
                // 如果沒找到對應的替換，也存入緩存（指向自己），避免下次重複查尋字典
                _spriteMappingCache[currentSprite] = currentSprite;
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
        bool hasSprites = AssetLoader.cacheParrySprites != null && AssetLoader.cacheParrySprites.Count > 0;
        if (!hasSprites &&
            !AssetLoader.ImperfectParryColor.HasValue) {
            return;
        }

        // 事先準備好要替換的 Shader (Alpha 混合模式，不會顏色相加)
        Shader alphaBlendShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        foreach (var renderer in FindObjectsOfType<ParticleSystemRenderer>(true)) {
            if (renderer.transform.parent.name == "YeeParryEffect_Not Accurate(Clone)") {

                var mats = renderer.materials;
                if (mats.Length > 1) {
                    var targetMat = mats[1];

                    if (alphaBlendShader != null) {
                        targetMat.shader = alphaBlendShader;
                    }
                    if (AssetLoader.cacheParrySprites.TryGetValue("imPerfect", out var sprite)) {
                        targetMat.SetTexture("_MainTex", sprite.texture);
                    }
                    if (AssetLoader.ImperfectParryColor.HasValue) {
                        targetMat.SetColor(TintColorID, AssetLoader.ImperfectParryColor.Value);
                    }
                    renderer.materials = mats;
                }
            }
        }
    }

    private void PerfectParry() {
        bool hasSprites = AssetLoader.cacheParrySprites != null && AssetLoader.cacheParrySprites.Count > 0;
        if (!hasSprites &&
            !AssetLoader.PerfectParryColor.HasValue) {
            return;
        }

        string basePath = "YeeParryEffectAccurate_Green(Clone)/ParrySparkAccurate0";
        if (groupedRenderers.TryGetValue(basePath, out var list)) {
            foreach (var r in list) {
                ApplyParrySprite(r);
                if (AssetLoader.PerfectParryColor.HasValue) {
                    r.color = AssetLoader.PerfectParryColor.Value;
                }
            }
        }
    }

    // 提取共用邏輯，讓程式碼更乾淨
    private void ApplyParrySprite(SpriteRenderer renderer) {
        if (renderer == null || renderer.sprite == null) return;

        // 1. 先找引用快取
        if (_spriteMappingCache.TryGetValue(renderer.sprite, out var cachedSprite)) {
            if (renderer.sprite != cachedSprite) renderer.sprite = cachedSprite;
        }
        // 2. 找不到才讀取 .name (僅此一次)
        else if (AssetLoader.cacheParrySprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            _spriteMappingCache[renderer.sprite] = sprite;
            renderer.sprite = sprite;
        } else {
            _spriteMappingCache[renderer.sprite] = renderer.sprite;
        }

        // 原有的翻轉邏輯保持不變
        float yRotation = (Player.i.Facing == Facings.Left) ? 180f : 0f;
        renderer.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
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
        if (AssetLoader.all == null || AssetLoader.all.Count == 0) return;

        string path = "Effect_Roll Dodge AfterImage(Clone)/Effect_HoHoYee_AirJump0";
        if (groupedRenderers.TryGetValue(path, out var list)) {
            foreach (var r in list) {
                ApplyToRenderer(r);
            }
        }
    }

    // 寫個小方法避免重寫兩次邏輯
    private void ApplyToRenderer(SpriteRenderer renderer) {
        if (renderer == null || renderer.sprite == null) return;

        // 第一步：用「引用(記憶體地址)」去查快取 (極快，零垃圾產生)
        if (_spriteMappingCache.TryGetValue(renderer.sprite, out var cachedSprite)) {
            if (renderer.sprite != cachedSprite) renderer.sprite = cachedSprite;
            return; // <--- 只要快取有中，後面的 .name 永遠不會被執行到！！
        }

        // 第二步：如果快取找不到 (代表這是這一幀剛出現的新 Sprite，或是還沒處理過)
        // 只有在這個「第一次碰面」的瞬間，才會執行昂貴的 .name
        string spriteName = renderer.sprite.name;

        if (AssetLoader.all.TryGetValue(spriteName, out var customSprite)) {
            // 找到替換後，立刻存入快取
            _spriteMappingCache[renderer.sprite] = customSprite;
            _spriteMappingCache[customSprite] = customSprite;
            renderer.sprite = customSprite;
        } else {
            // 沒找到替換，也存入快取(指向自己)，確保「下一幀」同一個 Sprite 進來時，在第一步就會被 return
            _spriteMappingCache[renderer.sprite] = renderer.sprite;
        }
    }

    //Sometime will not work cause object dynamic create cache didn't cache it
    private void AirJump() {
        if (AssetLoader.cachePlayerSprites == null || AssetLoader.cachePlayerSprites.Count == 0) return;

        string basePath = "Effect_AirJump(Clone)/Effect_HoHoYee_AirJump0";
        if (groupedRenderers.TryGetValue(basePath, out var list)) {
            foreach (var r in list) {
                ApplyAirJumpSprite(r);
            }
        }
    }

    private void ApplyAirJumpSprite(SpriteRenderer renderer) {
        if (renderer == null || renderer.sprite == null) return;

        if (_spriteMappingCache.TryGetValue(renderer.sprite, out var cachedSprite)) {
            if (renderer.sprite != cachedSprite) renderer.sprite = cachedSprite;
        } else if (AssetLoader.cachePlayerSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            _spriteMappingCache[renderer.sprite] = sprite;
            renderer.sprite = sprite;
        } else {
            _spriteMappingCache[renderer.sprite] = renderer.sprite;
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
        var cacheOnly = AssetLoader.cacheOnlyOneSprites;
        var cachePlayer = AssetLoader.cachePlayerSprites;

        // 安全檢查：確保玩家和 Sprite 存在
        if (Player.i?.PlayerSprite == null || Player.i.PlayerSprite.sprite == null) return;

        // 拿到當前遊戲原本想顯示的 Sprite 引用
        Sprite originalSprite = Player.i.PlayerSprite.sprite;

        // 情況 A：強制序列幀動畫 (這裡通常是特殊用途，保持原本邏輯即可)
        if (cacheOnly is { Count: > 0 }) {
            spriteChangeTimer += Time.deltaTime;

            if (cacheOnly.TryGetValue(currentSpriteIndex.ToString(), out var seqSprite)) {
                Player.i.PlayerSprite.sprite = seqSprite;

                if (spriteChangeTimer >= spriteDelaySecond.Value) {
                    currentSpriteIndex++;
                    if (!cacheOnly.ContainsKey(currentSpriteIndex.ToString())) {
                        currentSpriteIndex = 1;
                    }
                    spriteChangeTimer = 0f;
                }
            }
        }
        // 情況 B：根據 Sprite 名稱替換 (這是最常用、也是最吃效能的地方)
        else if (cachePlayer is { Count: > 0 }) {

            // --- 優化重點：開始使用引用快取 ---

            // 1. 先用 Sprite 引用在 _spriteMappingCache 找
            if (_spriteMappingCache.TryGetValue(originalSprite, out var cachedReplacement)) {
                // 如果找到了，且當前顯示的不是我們要的，就換掉
                if (originalSprite != cachedReplacement) {
                    Player.i.PlayerSprite.sprite = cachedReplacement;
                }
                return; // 這裡直接返回，不用跑後面的字串邏輯
            }

            // 2. 如果快取沒中，才執行「昂貴」的 .name 讀取
            string currentName = originalSprite.name;
            if (cachePlayer.TryGetValue(currentName, out var newSprite)) {
                // 存入快取：下次遇到這個 originalSprite 引用，直接換成 newSprite
                _spriteMappingCache[originalSprite] = newSprite;
                _spriteMappingCache[newSprite] = newSprite; // 防止重複處理
                Player.i.PlayerSprite.sprite = newSprite;
            } else {
                // 如果這個 Sprite 不需要替換，也存入快取指向自己，避免下次又跑一次 .name
                _spriteMappingCache[originalSprite] = originalSprite;
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
            if (groupedRenderers.TryGetValue(path, out var list)) {
                // 直接遍歷 List，不產生任何字串拼接
                foreach (var r in list) {
                    ApplyToRenderer(r);
                }
            }
        }
    }

    private void SwordOnce() {
        // 1. 基本檢查邏輯 (維持你之前的設定)
        bool hasSprites = AssetLoader.cacheSwordSprites != null && AssetLoader.cacheSwordSprites.Count > 0;
        if (!hasSprites &&
            !AssetLoader.SwordCharingCirlceColor.HasValue &&
            !AssetLoader.SwordCharingAbsorbColor.HasValue &&
            !AssetLoader.SwordCharingGlowColor.HasValue) {
            return;
        }

        // 2. 獲取根路徑物件 (這部分可以考慮改用 FindObjectsOfTypeAll)
        // 為了效能，先找出父物件，再用 transform.Find 找子物件
        var rootObj = GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle");

        if (rootObj == null) {
            // 如果找不到，嘗試搜尋非活躍物件 (效能較重，慎用)
            var allRenderers = Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>();
            // 這裡需要透過過濾名稱或路徑來定位，但通常建議在物件生成時就取得引用
            return;
        }

        Transform rootTrans = rootObj.transform;

        // --- 圓形粒子 ---
        var chargePaths = new[] { "P_PowerCharged/F1", "P_PowerCharged/F2", "P_PowerCharged/F3", "P_PowerCharged/F4", "P_PowerCharged/F5" };
        foreach (var path in chargePaths) {
            var target = rootTrans.Find(path);
            if (target != null) {
                var renderer = target.GetComponent<ParticleSystemRenderer>();
                // 設定貼圖
                if (hasSprites && AssetLoader.cacheSwordSprites.TryGetValue("FooSmokeGlow", out var s)) {
                    renderer.materials[1].SetTexture("_MainTex", s.texture);
                }
                // 設定顏色
                if (AssetLoader.SwordCharingCirlceColor.HasValue) {
                    renderer.materials[1].color = AssetLoader.SwordCharingCirlceColor.Value;
                }
            }
        }

        // --- 吸收粒子 ---
        var absorbObj = rootTrans.Find("P_PowerCharging/P_hit");
        if (absorbObj != null) {
            var renderer = absorbObj.GetComponent<ParticleSystemRenderer>();
            if (hasSprites && AssetLoader.cacheSwordSprites.TryGetValue("bubbletrail", out var s2)) {
                renderer.materials[1].SetTexture("_MainTex", s2.texture);
            }
            if (AssetLoader.SwordCharingAbsorbColor.HasValue) {
                renderer.materials[1].color = AssetLoader.SwordCharingAbsorbColor.Value;
            }
        }

        // --- Glow ---
        var glowObj = rootTrans.Find("Glow");
        if (glowObj != null && AssetLoader.SwordCharingGlowColor.HasValue) {
            var sr = glowObj.GetComponent<SpriteRenderer>();
            sr.material.color = AssetLoader.SwordCharingGlowColor.Value;
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
        bool hasSprites = AssetLoader.cacheUISprites != null && AssetLoader.cacheUISprites.Count > 0;
        if (!hasSprites &&
            !AssetLoader.ArrowGlowColor.HasValue &&
            !AssetLoader.RageBarFrameColor.HasValue &&
            !AssetLoader.RageBarColor.HasValue) {
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
        bool hasSprites = AssetLoader.cacheUISprites != null && AssetLoader.cacheUISprites.Count > 0;
        if (!hasSprites &&
            !AssetLoader.expRingOuterColor.HasValue &&
            !AssetLoader.expRingInnerColor.HasValue) {
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
        bool hasSprites = AssetLoader.cacheUISprites != null && AssetLoader.cacheUISprites.Count > 0;
        if (!hasSprites &&
            !AssetLoader.normalHpColor.HasValue &&
            !AssetLoader.internalHpColor.HasValue) {
            return;
        }

        string normalHp = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/HealthBarBase/HealthBar/BG renderer/Health";
        if (AssetLoader.normalHpColor.HasValue && cachedSpriteRenderers.TryGetValue(normalHp, out var renderer)) {
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
        _spriteMappingCache.Clear();

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
        FooColorOnce();
        //YingZhaoOnce();
    }


    private void OnDestroy() {
        harmony.UnpatchSelf();
    }
}