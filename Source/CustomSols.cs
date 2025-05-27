using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using I2.Loc;
using NineSolsAPI;
using NineSolsAPI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        if (isEnableSword.Value) Sword();
        if (isToastPlayerSprite.Value && Player.i?.PlayerSprite != null)
            ToastManager.Toast(Player.i.PlayerSprite.sprite.name);
    }

    private void ChangeMenuLogo() {
        if (!isAssetsLoaded) {
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
        if (isEnableMenuLogo.Value) ChangeMenuLogo();
        if (isEnableUIChiBall.Value) ChangeUIChiBall();
        if (isEnableImPerfectParry.Value) ImPerfectParry();
        if (isEnableSword.Value) SwordOnce();
        if (isEnableBow.Value) InitializeBowSprites();

        arrowInit = false;
        arrowInit2 = false;
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
            }
        }
    }

    private void UCCharging() {
        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging C") is { } obj) {
            if (AssetLoader.cacheParrySprites.TryGetValue("UCCharging", out var sprite)) {
                var particleRenderer = obj.GetComponent<ParticleSystemRenderer>();
                particleRenderer.materials[0].SetTexture("_MainTex", sprite.texture);
                obj.GetComponent<ParticleSystem>().startColor = UCChargingColor.Value;
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

    public static void UpdateBowSprite(GameObject parent, string childPath) {
        if (parent == null) return;

        var renderer = parent.transform.Find(childPath)?.GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null && AssetLoader.cacheBowSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
            renderer.sprite = sprite;
        }
    }

    private void Sword() {
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

    private void ModOption() {
        var optionsButton = GameObject.Find("MainMenuButton_Option");

        var modOptions = ObjectUtils.InstantiateAutoReference(optionsButton, optionsButton.transform.parent, false);
        UnityEngine.Object.Destroy(modOptions.GetComponentInChildren<Localize>());
        modOptions.GetComponentInChildren<TMP_Text>().text = "CustomSols";
        modOptions.gameObject.transform.SetSiblingIndex(optionsButton.transform.GetSiblingIndex() + 1);
        var modOptionButton = modOptions.GetComponentInChildren<UIControlButton>();
        var providers = StartMenuLogic.Instance.gameObject.GetComponentInChildren<UICursorProvider>();

        // 建立主面板
        var obj = new GameObject("ModOptions Panel");
        obj.transform.SetParent(providers.transform, false);

        var rectTransform = obj.AddComponent<RectTransform>();
        var uiControlGroup = obj.AddComponent<UIControlGroup>();
        var rcgUiPanel = obj.AddComponent<RCGUIPanel>(); // 假設 RCGUIPanel 是自定義組件
        obj.AddComponent<CanvasRenderer>();
        obj.AddComponent<SelectableNavigationProvider>();
        obj.AddComponent<Animator>();
        AutoAttributeManager.AutoReference(obj);

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero; // 確保面板填滿父物件
        rectTransform.offsetMax = Vector2.zero;

        rcgUiPanel.OnShowInit = new UnityEvent();
        rcgUiPanel.OnHideInit = new UnityEvent();
        rcgUiPanel.OnShowComplete = new UnityEvent();
        rcgUiPanel.OnHideComplete = new UnityEvent();

        // 建立 ScrollView
        var scrollView = new GameObject("ScrollView");
        var scrollViewTransform = scrollView.AddComponent<RectTransform>();
        scrollViewTransform.SetParent(obj.transform, false);
        scrollViewTransform.anchorMin = new Vector2(0.5f, 0.5f);
        scrollViewTransform.anchorMax = new Vector2(0.5f, 0.5f);
        scrollViewTransform.sizeDelta = new Vector2(600, 800); // ScrollView 大小
        scrollViewTransform.anchoredPosition = Vector2.zero;
        scrollView.AddComponent<CanvasRenderer>();
        //scrollView.AddComponent<Image>(); // 可選，添加背景
        var scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.scrollSensitivity = 30f; // 設置滾輪靈敏度

        // 建立 Viewport
        var viewport = new GameObject("Viewport");
        var viewportTransform = viewport.AddComponent<RectTransform>();
        viewportTransform.SetParent(scrollViewTransform, false);
        viewportTransform.anchorMin = Vector2.zero;
        viewportTransform.anchorMax = Vector2.one;
        viewportTransform.offsetMin = Vector2.zero;
        viewportTransform.offsetMax = Vector2.zero;
        viewport.AddComponent<CanvasRenderer>();
        viewport.AddComponent<Image>(); // 可選，添加背景
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // 建立 Content
        var content = new GameObject("Content");
        var contentTransform = content.AddComponent<RectTransform>();
        contentTransform.SetParent(viewportTransform, false);
        contentTransform.anchorMin = new Vector2(0, 1); // 錨點設為頂部
        contentTransform.anchorMax = new Vector2(1, 1);
        contentTransform.pivot = new Vector2(0.5f, 1);
        contentTransform.anchoredPosition = Vector2.zero;
        contentTransform.sizeDelta = new Vector2(0, 0); // 寬度隨容器，高度動態計算
        var layoutGroup = content.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 20;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 設置 ScrollRect
        scrollRect.viewport = viewportTransform;
        scrollRect.content = contentTransform;
        scrollRect.horizontal = false; // 僅允許垂直滾動
        scrollRect.vertical = true;

        // 添加 Scrollbar
        var scrollbar = new GameObject("Scrollbar");
        var scrollbarTransform = scrollbar.AddComponent<RectTransform>();
        scrollbarTransform.SetParent(scrollViewTransform, false);
        scrollbarTransform.anchorMin = new Vector2(1, 0);
        scrollbarTransform.anchorMax = new Vector2(1, 1);
        scrollbarTransform.sizeDelta = new Vector2(20, 0);
        scrollbarTransform.anchoredPosition = Vector2.zero;
        scrollbar.AddComponent<CanvasRenderer>();
        //scrollbar.AddComponent<Image>(); // 可選，滾動條背景
        var scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
        scrollRect.verticalScrollbar = scrollbarComponent;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -3;

        // 標題
        var title = new GameObject("Title");
        var titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "CustomSols";
        titleText.alignment = TextAlignmentOptions.Center;
        title.AddComponent<LayoutElement>().preferredHeight = 50;
        title.transform.SetParent(contentTransform, false);

        // 間距
        var padding = new GameObject("Padding");
        padding.AddComponent<CanvasRenderer>();
        padding.AddComponent<RectTransform>();
        padding.AddComponent<LayoutElement>().minHeight = 20;
        padding.transform.SetParent(contentTransform, false);

        // 添加按鈕
        var buttonOrig = ObjectUtils.FindDisabledByName<Button>("Show HUD")!;
        for (int i = 0; i < 20; i++) {
            var c = ObjectUtils.InstantiateAutoReference(buttonOrig.gameObject, contentTransform);
            c.name = $"Button_{i + 1}"; // Optional: for easier debugging
            c.GetComponentInChildren<TMP_Text>().text = $"Button {i + 1}";
            var button = c.GetComponent<Button>();
            var flag = new FlagFieldEntryInt();
            var intFlag = ScriptableObject.CreateInstance<GameFlagInt>();
            intFlag.field = new FlagFieldInt();
            flag.flagBase = intFlag;
            flag.fieldName = "field";
            c.GetComponent<MultipleOptionSelector>().entry = flag;

            int buttonIndex = i + 1;
            button.onClick.RemoveAllListeners(); // Clear existing listeners
            button.onClick.AddListener(() => {
                ToastManager.Toast($"Click Button {buttonIndex}");
            });
            c.AddComponent<LayoutElement>().preferredHeight = buttonOrig.GetComponent<RectTransform>().sizeDelta.y;
        }

        // 設置默認選中
        uiControlGroup.defaultSelectable = content.GetComponentInChildren<Selectable>();
        modOptionButton.clickToShowGroup = uiControlGroup;
    }

    private void Reload() {
        ModOption();


        //InitializeAssets();
        //if (isEnableMenuLogo.Value) ChangeMenuLogo();
        //if (isEnableUIChiBall.Value) ChangeUIChiBall();
        //if (isEnableImPerfectParry.Value) ImPerfectParry();
        //if (isEnableSword.Value) SwordOnce();
        //if (isEnableBow.Value) InitializeBowSprites();
    }

    private void OnDestroy() {
        harmony.UnpatchSelf();
    }
}