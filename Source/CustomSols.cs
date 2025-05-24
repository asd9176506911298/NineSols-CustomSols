using Battlehub.MeshDeformer2;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NineSolsAPI;
using NineSolsAPI.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CustomSols.AssetLoader;

namespace CustomSols;

[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CustomSols : BaseUnityPlugin {
    private ConfigEntry<bool> isEnablePlayer = null!;
    private ConfigEntry<bool> isEnableMenuLogo = null!;
    private ConfigEntry<bool> isEnableUIChiBall = null!;
    private ConfigEntry<bool> isEnableTalismanBall = null!;
    private ConfigEntry<bool> isEnableDash = null!;
    private ConfigEntry<bool> isEnableAirJump = null!;
    private ConfigEntry<bool> isEnableimPerfectParry = null!;
    private ConfigEntry<bool> isEnablePerfectParry = null!;
    private ConfigEntry<bool> isEnableUCSuccess= null!;
    private ConfigEntry<bool> isEnableUCCharging = null!;
    private ConfigEntry<bool> isEnableUCAroundEffect = null!;

    private ConfigEntry<Color> UCChargingColor = null!;
    private ConfigEntry<Color> UCSuccessColor = null!;
    private ConfigEntry<KeyboardShortcut> reloadShortcut = null!;

    private Harmony harmony = null!;

    private void Awake() {
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        // Load patches from any class annotated with @HarmonyPatch
        harmony = Harmony.CreateAndPatchAll(typeof(CustomSols).Assembly);

        isEnablePlayer = Config.Bind("", "Player Sprite", true, "");
        isEnableMenuLogo = Config.Bind("", "Menu Logo Sprite", true, "");
        isEnableUIChiBall = Config.Bind("", "UI Chi Ball", true, "");
        isEnableTalismanBall = Config.Bind("", "EnableTalisman Ball Sprite", true, "");
        isEnableDash = Config.Bind("", "Dash Sprite", true, "");
        isEnableAirJump = Config.Bind("", "AirJump Sprite", true, "");
        isEnableimPerfectParry = Config.Bind("", "imPerfectParry Sprite", true, "");
        isEnablePerfectParry = Config.Bind("", "PerfectParry Sprite", true, "");
        isEnableUCSuccess = Config.Bind("", "UCSuccess Sprite", true, "");
        isEnableUCCharging = Config.Bind("", "UCCharging Sprite", true, "");
        isEnableUCAroundEffect = Config.Bind("", "UCAroundEffect Sprite", true, "");

        UCChargingColor = Config.Bind("Color", "UCCharging Color", new Color(1f, 0.837f, 0f, 1f), "");
        UCSuccessColor = Config.Bind("Color", "UCSuccess Color", new Color(1f, 0.718f, 1f, 1f), "");

        reloadShortcut = Config.Bind("Shortcut", "Reload Shortcut",
            new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl), "");

        KeybindManager.Add(this, reload, () => reloadShortcut.Value);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start() {
        AssetLoader.Init();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        //MenuLogo
        if (isEnableMenuLogo.Value)
            changeMenuLogo();

        //UI Chi Ball
        if (isEnableUIChiBall.Value)
            changeUIChiBall();

        if (isEnableimPerfectParry.Value) {
            imPerfectParry();
        }
        
    }

    private void LateUpdate() {
        if (isEnablePlayer.Value)
            PlayerSprite();

        // Perfect Parry
        if (isEnablePerfectParry.Value)
            PerfectParry();

        //Dash Sprite
        if (isEnableDash.Value)
            Dash();

        //Air Jump Sprite
        if (isEnableAirJump.Value) {
            airJump();
        }
        
        // UC Parry Around Effect
        if (isEnableUCAroundEffect.Value)
            UCAroundEffect();

        //UC Sucess
        if (isEnableUCSuccess.Value)
            UCSuccess();

        //UC Charging
        if (isEnableUCCharging.Value)
            UCCharging();

        if (isEnableTalismanBall.Value)
            TalismanBall();

        // Bow
        {
            // PPlayer Bow 僅須執行一次 Bow_0
            if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Yee_Skill/HoHoYee_Archery/Bow") != null) {
                //ToastManager.Toast(GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Yee_Skill/HoHoYee_Archery/Bow").GetComponent<SpriteRenderer>().sprite.name);
                var spriteName = GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Yee_Skill/HoHoYee_Archery/Bow").GetComponent<SpriteRenderer>().sprite.name;
                GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Yee_Skill/HoHoYee_Archery/Bow").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
            }

            // PPlayer Bow's Aimer
            if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Yee_Skill/HoHoYee_Archery/Bow/Bow_A") != null) {
                //ToastManager.Toast(GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Yee_Skill/HoHoYee_Archery/Bow/Bow_A").GetComponent<SpriteRenderer>().sprite.name);
                var spriteName = GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Yee_Skill/HoHoYee_Archery/Bow/Bow_A").GetComponent<SpriteRenderer>().sprite.name;
                GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Yee_Skill/HoHoYee_Archery/Bow/Bow_A").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
            }

            //尋影LV1~3 僅須執行一次
            {
                //尋影Lv1 箭矢 ChasingArrow_0 手上箭矢 + 角
                //if (GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv1(Clone)/Circle Shooter/Arrow (1)/ChasingArrow /ChasingArrowLight") != null) {
                //    //ToastManager.Toast(GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv1(Clone)/Circle Shooter/Arrow (1)/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name);
                //    //ToastManager.Toast(GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv1(Clone)/Circle Shooter/Arrow (1)/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>().sprite.name);
                //    //ChasingArrow_0
                //    var spriteName = GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv1(Clone)/Circle Shooter/Arrow (1)/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv1(Clone)/Circle Shooter/Arrow (1)/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //    //ChasingArrow_2
                //    spriteName = GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv1(Clone)/Circle Shooter/Arrow (1)/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv1(Clone)/Circle Shooter/Arrow (1)/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //    GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv1(Clone)/Circle Shooter/Arrow (1)/ChasingArrow /Parent 刺/刺 (1)/刺").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}

                ////尋影Lv2 箭矢 手上箭矢 + 角
                //if (GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv2(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /ChasingArrowLight") != null) {
                //    //ToastManager.Toast(GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv2(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name);
                //    //ToastManager.Toast(GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv2(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>().sprite.name);
                //    //ChasingArrow_3
                //    var spriteName = GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv2(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv2(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //    //ChasingArrow_2
                //    spriteName = GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv2(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv2(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //    GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv2(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /Parent 刺/刺 (1)/刺").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}

                ////尋影Lv3 箭矢 手上箭矢 + 角
                //if (GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv3(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /ChasingArrowLight") != null) {
                //    //ToastManager.Toast(GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv3(Clone)/Circle Shooter/Arrow (3)/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name);
                //    //ChasingArrow_4
                //    var spriteName = GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv3(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv3(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //    //ChasingArrow_1
                //    spriteName = GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv3(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv3(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //    GameObject.Find("Chasing Arrow Shooter 飛天御劍 lv3(Clone)/Circle Shooter/Arrow (2)/ChasingArrow /Parent 刺/刺 (1)/刺").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}

                ////尋影Lv1 箭矢 ChasingArrow_0 因為有很多個物件同名 會很卡 僅須執行一次 那就沒關係
                //foreach (var arrow in GameObject.FindObjectsOfType<RCGFSM.Projectiles.PlayerArrowProjectileFollower>(true)) {
                //    SpriteRenderer spriteRenderer = arrow.transform.Find("Projectile FSM/FSM Animator/View/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>();
                //    //箭矢
                //    if (spriteRenderer != null && AssetLoader.cacheParrySprites.ContainsKey("imPerfect")) {
                //        spriteRenderer.sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //    }

                //    spriteRenderer = arrow.transform.Find("Projectile FSM/FSM Animator/View/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>();
                //    if (spriteRenderer != null && AssetLoader.cacheParrySprites.ContainsKey("imPerfect")) {
                //        spriteRenderer.sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //    }
                //    spriteRenderer = arrow.transform.Find("Projectile FSM/FSM Animator/View/ChasingArrow /Parent 刺/刺 (1)/刺").GetComponent<SpriteRenderer>();
                //    if (spriteRenderer != null && AssetLoader.cacheParrySprites.ContainsKey("imPerfect")) {
                //        spriteRenderer.sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //    }
                //}

                //////尋影Lv1 箭矢 ChasingArrow_0 USe For Find test
                ////if (GameObject.Find("Yee Chasing Arrow 尋影箭 Lv1(Clone)/Projectile FSM/FSM Animator/View/ChasingArrow /ChasingArrowLight") != null) {
                ////    //ToastManager.Toast(GameObject.Find("Yee Chasing Arrow 尋影箭 Lv1(Clone)1/Projectile FSM/FSM Animator/View/ChasingArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name);
                ////    GameObject.Find("Yee Chasing Arrow 尋影箭 Lv1(Clone)/Projectile FSM/FSM Animator/View/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                ////}
            }

            //爆破Lv1~3 僅須執行一次
            {
                ////爆破Lv1 基座
                //if (GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv1(Clone)/Exploding Arrow/ExplodingArrow/ExplodingArrow") != null) {
                //    //ToastManager.Toast(GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv1(Clone)/Exploding Arrow/ExplodingArrow/ExplodingArrow").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv1(Clone)/Exploding Arrow/ExplodingArrow/ExplodingArrow").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv1 基座 Light
                //if (GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv1(Clone)/Exploding Arrow/ExplodingArrow/ChasingArrowLight") != null) {
                //    //ToastManager.Toast(GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv1(Clone)/Exploding Arrow/ExplodingArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv1(Clone)/Exploding Arrow/ExplodingArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv1 能量球
                //if (GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv1(Clone)/Exploding Arrow/EnergyBall/Core") != null) {
                //    //ToastManager.Toast(GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv1(Clone)/Exploding Arrow/EnergyBall/Core").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv1(Clone)/Exploding Arrow/EnergyBall/Core").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv1 能量球 爆炸 circle_mask
                //if (GameObject.Find("Explosion Damage 爆破箭 閃電 lv1(Clone)/ATTACK/Core") != null) {
                //    //ToastManager.Toast(GameObject.Find("Explosion Damage 爆破箭 閃電 lv1(Clone)/ATTACK/Core").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("Explosion Damage 爆破箭 閃電 lv1(Clone)/ATTACK/Core").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv2 基座
                //if (GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv2(Clone)/Exploding Arrow/ExplodingArrow/ExplodingArrow") != null) {
                //    //ToastManager.Toast(GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv2(Clone)/Exploding Arrow/ExplodingArrow/ExplodingArrow").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv2(Clone)/Exploding Arrow/ExplodingArrow/ExplodingArrow").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv2 基座 Light
                //if (GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv2(Clone)/Exploding Arrow/ExplodingArrow/ChasingArrowLight") != null) {
                //    //ToastManager.Toast(GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv2(Clone)/Exploding Arrow/ExplodingArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv2(Clone)/Exploding Arrow/ExplodingArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv2 能量球
                //if (GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv2(Clone)/Exploding Arrow/EnergyBall/Core") != null) {
                //    //ToastManager.Toast(GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv2(Clone)/Exploding Arrow/EnergyBall/Core").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv2(Clone)/Exploding Arrow/EnergyBall/Core").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv2 能量球 爆炸 circle_mask
                //if (GameObject.Find("Explosion Damage 爆破箭 閃電 lv2(Clone)/ATTACK/Core") != null) {
                //    //ToastManager.Toast(GameObject.Find("Explosion Damage 爆破箭 閃電 lv2(Clone)/ATTACK/Core").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("Explosion Damage 爆破箭 閃電 lv2(Clone)/ATTACK/Core").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv3 基座
                //if (GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv3(Clone)/Exploding Arrow/ExplodingArrow/ExplodingArrow") != null) {
                //    //ToastManager.Toast(GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv3(Clone)/Exploding Arrow/ExplodingArrow/ExplodingArrow").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv3(Clone)/Exploding Arrow/ExplodingArrow/ExplodingArrow").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv3 基座 Light
                //if (GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv3(Clone)/Exploding Arrow/ExplodingArrow/ChasingArrowLight") != null) {
                //    //ToastManager.Toast(GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv3(Clone)/Exploding Arrow/ExplodingArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv3(Clone)/Exploding Arrow/ExplodingArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv3 能量球
                //if (GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv3(Clone)/Exploding Arrow/EnergyBall/Core") != null) {
                //    //ToastManager.Toast(GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv3(Clone)/Exploding Arrow/EnergyBall/Core").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("ExplodingArrow Shooter 爆破發射器 Lv3(Clone)/Exploding Arrow/EnergyBall/Core").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}

                ////爆破Lv3 能量球 爆炸 circle_mask
                //if (GameObject.Find("Explosion Damage 爆破箭 閃電 lv3(Clone)/ATTACK/Core") != null) {
                //    //ToastManager.Toast(GameObject.Find("Explosion Damage 爆破箭 閃電 lv3(Clone)/ATTACK/Core").GetComponent<SpriteRenderer>().sprite.name);
                //    GameObject.Find("Explosion Damage 爆破箭 閃電 lv3(Clone)/ATTACK/Core").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["imPerfect"];
                //}
            }


            //穿雲lv1~3 僅須執行一次
            {
                //if (!isEnablePlayer.Value) return;
                //////穿雲lv1 Bow 光束
                //if (GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/光束") != null) {
                //    //ToastManager.Toast(GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/光束").GetComponent<SpriteRenderer>().sprite.name);
                //    //ToastManager.Toast(GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/光束").GetComponent<SpriteRenderer>().sprite.pixelsPerUnit);
                //    //GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/光束").GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Sliced;
                //    //GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/光束").GetComponent<SpriteRenderer>().size = new Vector2(488.3f, 64f);
                //    var spriteName = GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/光束").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/光束").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}

                ////////穿雲lv1 Bow Arrow
                //if (GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/NormalArrow") != null) {
                //    //ToastManager.Toast(GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/NormalArrow").GetComponent<SpriteRenderer>().sprite.name);
                //    var spriteName = GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/NormalArrow").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/NormalArrow").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}

                //////穿雲lv1 Bow Arrow Light
                //if (GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/NormalArrow/ChasingArrowLight") != null) {
                //    //ToastManager.Toast(GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/NormalArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name);
                //    var spriteName = GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/NormalArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("NormalArrow Shoot 穿雲 Lv1(Clone)/NormalArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}

                ////穿雲lv2 Bow 光束
                //if (GameObject.Find("NormalArrow Shoot 穿雲 Lv2(Clone)/光束") != null) {
                //    //ToastManager.Toast(GameObject.Find("NormalArrow Shoot 穿雲 Lv2(Clone)/光束").GetComponent<SpriteRenderer>().sprite.name);
                //    var spriteName = GameObject.Find("NormalArrow Shoot 穿雲 Lv2(Clone)/光束").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("NormalArrow Shoot 穿雲 Lv2(Clone)/光束").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //    //GameObject.Find("NormalArrow Shoot 穿雲 Lv2(Clone)/光束").GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Sliced;
                //    //GameObject.Find("NormalArrow Shoot 穿雲 Lv2(Clone)/光束").GetComponent<SpriteRenderer>().size = new Vector2(488.3f, 64f);
                //}

                //////穿雲lv2 Bow Arrow
                //if (GameObject.Find("NormalArrow Shoot 穿雲 Lv2(Clone)/NormalArrow") != null) {
                //    //ToastManager.Toast(GameObject.Find("NormalArrow Shoot 穿雲 Lv2(Clone)/NormalArrow").GetComponent<SpriteRenderer>().sprite.name);
                //    var spriteName = GameObject.Find("NormalArrow Shoot 穿雲 Lv2(Clone)/NormalArrow").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("NormalArrow Shoot 穿雲 Lv2(Clone)/NormalArrow").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}

                //////穿雲lv2 Bow Arrow Light
                //if (GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow/ChasingArrowLight") != null) {
                //    //ToastManager.Toast(GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name);
                //    var spriteName = GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}

                //////穿雲lv3 Bow 光束
                //if (GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/光束") != null) {
                //    //ToastManager.Toast(GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/光束").GetComponent<SpriteRenderer>().sprite.name);
                //    var spriteName = GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/光束").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/光束").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}

                //////穿雲lv3 Bow Arrow
                //if (GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow") != null) {
                //    //ToastManager.Toast(GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow").GetComponent<SpriteRenderer>().sprite.name);
                //    var spriteName = GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}

                ////穿雲lv3 Bow Arrow Light
                //if (GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow/ChasingArrowLight") != null) {
                //    //ToastManager.Toast(GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name);
                //    var spriteName = GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name;
                //    GameObject.Find("NormalArrow Shoot 穿雲 Lv3(Clone)/NormalArrow/ChasingArrowLight").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheBowSprites[spriteName];
                //}
            }
        }

        //Sword
        {
            if (!isEnablePlayer.Value) return;
            //Attack A
            //if (GameObject.Find("HoHoYee_AttackA_PoolObject_Variant(Clone)/Sprite") != null) {
            //    //ToastManager.Toast(GameObject.Find("HoHoYee_AttackA_PoolObject_Variant(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite.name);
            //    var spriteName = GameObject.Find("HoHoYee_AttackA_PoolObject_Variant(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite.name;
            //    GameObject.Find("HoHoYee_AttackA_PoolObject_Variant(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheSwordSprites[spriteName];
            //}

            ////Attack B
            //if (GameObject.Find("HoHoYee_AttackB_PoolObject_Variant(Clone)/Sprite") != null) {
            //    //ToastManager.Toast(GameObject.Find("HoHoYee_AttackB_PoolObject_Variant(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite.name);
            //    var spriteName = GameObject.Find("HoHoYee_AttackB_PoolObject_Variant(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite.name;
            //    GameObject.Find("HoHoYee_AttackB_PoolObject_Variant(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheSwordSprites[spriteName];
            //}

            ////Attack C
            //if (GameObject.Find("HoHoYee_AttackC ThirdAttack Effect(Clone)/Sprite") != null) {
            //    //ToastManager.Toast(GameObject.Find("HoHoYee_AttackC ThirdAttack Effect(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite.name);
            //    var spriteName = GameObject.Find("HoHoYee_AttackC ThirdAttack Effect(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite.name;
            //    GameObject.Find("HoHoYee_AttackC ThirdAttack Effect(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheSwordSprites[spriteName];
            //}

            ////Attack C With Qi Balde Jade Effect_劍氣玉0~4
            //if (GameObject.Find("Yee 氣刃 chi blade(Clone)/Projectile FSM/FSM Animator/View/Sprite") != null) {
            //    //ToastManager.Toast(GameObject.Find("Yee 氣刃 chi blade(Clone)/Projectile FSM/FSM Animator/View/Sprite").GetComponent<SpriteRenderer>().sprite.name);
            //    var spriteName = GameObject.Find("Yee 氣刃 chi blade(Clone)/Projectile FSM/FSM Animator/View/Sprite").GetComponent<SpriteRenderer>().sprite.name;
            //    GameObject.Find("Yee 氣刃 chi blade(Clone)/Projectile FSM/FSM Animator/View/Sprite").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheSwordSprites[spriteName];
            //}

            ////Attack C With Qi Balde Jade Base Effect_HoHoYee_AttackD2~8
            //if (GameObject.Find("HoHoYee_AttackC ThirdAttack 劍氣玉 Effect(Clone)/Sprite") != null) {
            //    //ToastManager.Toast(GameObject.Find("HoHoYee_AttackC ThirdAttack 劍氣玉 Effect(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite.name);
            //    var spriteName = GameObject.Find("HoHoYee_AttackC ThirdAttack 劍氣玉 Effect(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite.name;
            //    GameObject.Find("HoHoYee_AttackC ThirdAttack 劍氣玉 Effect(Clone)/Sprite").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheSwordSprites[spriteName];
            //}

            ////// Release Charging Sword Cross Effect EFFECT_HoHoYee_ChargingAttack0
            //if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/Effect_Attack/ChargeAttack/ChargeAttackSprite") != null) {
            //    //ToastManager.Toast(GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/Effect_Attack/ChargeAttack/ChargeAttackSprite").GetComponent<SpriteRenderer>().sprite.name);
            //    var spriteName = GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/Effect_Attack/ChargeAttack/ChargeAttackSprite").GetComponent<SpriteRenderer>().sprite.name;
            //    GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/Effect_Attack/ChargeAttack/ChargeAttackSprite").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheSwordSprites[spriteName];
            //}

            //////// Release Charge Sword Sprite
            //if (GameObject.Find("HoHoYee_Charging 蓄力攻擊特效(Clone)/ChargeAttackSprite") != null) {
            //    //ToastManager.Toast(GameObject.Find("HoHoYee_Charging 蓄力攻擊特效(Clone)/ChargeAttackSprite").GetComponent<SpriteRenderer>().sprite.name);
            //    var spriteName = GameObject.Find("HoHoYee_Charging 蓄力攻擊特效(Clone)/ChargeAttackSprite").GetComponent<SpriteRenderer>().sprite.name;
            //    //ToastManager.Toast(spriteName);
            //    GameObject.Find("HoHoYee_Charging 蓄力攻擊特效(Clone)/ChargeAttackSprite").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheSwordSprites[spriteName];
            //}

            //// Jade Big Sword Release Charge Sword Sprite // EFFECT_HoHoYee_ChargingAttack0
            //if (GameObject.Find("HoHoYee_Charging 蓄力攻擊特效(Clone)/Super Charge Ability/childNode/ChargeAttackSprite") != null){
            //    var spriteName = GameObject.Find("HoHoYee_Charging 蓄力攻擊特效(Clone)/Super Charge Ability/childNode/ChargeAttackSprite").GetComponent<SpriteRenderer>().sprite.name;
            //    GameObject.Find("HoHoYee_Charging 蓄力攻擊特效(Clone)/Super Charge Ability/childNode/ChargeAttackSprite").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheSwordSprites[spriteName];
            //}


            ////Sword Charge 5 round
            //if(GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharged/F5") != null) {
            //    GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharged/F1").GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", AssetLoader.cacheParrySprites["imPerfect"].texture);
            //    GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharged/F2").GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", AssetLoader.cacheParrySprites["imPerfect"].texture);
            //    GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharged/F3").GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", AssetLoader.cacheParrySprites["imPerfect"].texture);
            //    GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharged/F4").GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", AssetLoader.cacheParrySprites["imPerfect"].texture);
            //    GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharged/F5").GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", AssetLoader.cacheParrySprites["imPerfect"].texture);
            //}

            ////Sword Center absorb
            //if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharging/P_hit") != null) {
            //    GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/ChargeAttackParticle/P_PowerCharging/P_hit").GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", AssetLoader.cacheParrySprites["imPerfect"].texture);
            //}
        }


        //Eigong Sword 10
        //GameObject.Find("GameLevel/Room/Prefab/EventBinder/General Boss Fight FSM Object Variant/FSM Animator/LogicRoot/---Boss---/Boss_Yi Gung/MonsterCore/Animator(Proxy)/Animator/View/YiGung/Weapon/Sword/Sword Sprite").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites["Sword10"];
    }

    private void changeMenuLogo() {
        if (GameObject.Find("MenuLogic/MainMenuLogic/Providers/MenuUIPanel/Logo") != null) {
            if (AssetLoader.cacheMenuLogoSprites.ContainsKey("9sLOGO_1")) {
                GameObject.Find("MenuLogic/MainMenuLogic/Providers/MenuUIPanel/Logo").GetComponent<UnityEngine.UI.Image>().sprite = AssetLoader.cacheMenuLogoSprites["9sLOGO_1"];
            }
        }
    }

    private void changeUIChiBall() {
        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint/BG/Rotate/Fill") != null) {
            if (AssetLoader.cacheUIChiBallSprites.ContainsKey("ParryBalls")) {
                var sprite = AssetLoader.cacheUIChiBallSprites["ParryBalls"];
                GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint/BG/Rotate/Fill").GetComponent<SpriteRenderer>().sprite = sprite;
                GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (5)/BG/Rotate/Fill").GetComponent<SpriteRenderer>().sprite = sprite;
                GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (6)/BG/Rotate/Fill").GetComponent<SpriteRenderer>().sprite = sprite;
                GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (7)/BG/Rotate/Fill").GetComponent<SpriteRenderer>().sprite = sprite;
                GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/ParryBalls/ParryPoint (8)/BG/Rotate/Fill").GetComponent<SpriteRenderer>().sprite = sprite;
            }
        }
    }

    private void imPerfectParry() {
        foreach (var x in GameObject.FindObjectsOfType<ParticleSystemRenderer>(true)) {
            if (x.transform.parent.name == "YeeParryEffect_Not Accurate(Clone)") {
                x.GetComponent<ParticleSystemRenderer>().materials[1].SetTexture("_MainTex", AssetLoader.cacheParrySprites["imPerfect"].texture);
            }
        }
    }

    private void PerfectParry() {
        if (GameObject.Find("YeeParryEffectAccurate_Green(Clone)/ParrySparkAccurate0") != null) {
            //ToastManager.Toast(GameObject.Find("YeeParryEffectAccurate_Green(Clone)/ParrySparkAccurate0").GetComponent<SpriteRenderer>().sprite.name);
            var spriteName = GameObject.Find("YeeParryEffectAccurate_Green(Clone)/ParrySparkAccurate0").GetComponent<SpriteRenderer>().sprite.name;
            GameObject.Find("YeeParryEffectAccurate_Green(Clone)/ParrySparkAccurate0").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites[spriteName];
            if (Player.i.Facing == Facings.Left)
                GameObject.Find("YeeParryEffectAccurate_Green(Clone)/ParrySparkAccurate0").transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            else
                GameObject.Find("YeeParryEffectAccurate_Green(Clone)/ParrySparkAccurate0").transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    private void Dash() {
        if (GameObject.Find("Effect_Roll Dodge AfterImage(Clone)/Effect_HoHoYee_AirJump0") != null) {
            var name = GameObject.Find("Effect_Roll Dodge AfterImage(Clone)/Effect_HoHoYee_AirJump0").GetComponent<SpriteRenderer>().sprite.name;
            GameObject.Find("Effect_Roll Dodge AfterImage(Clone)/Effect_HoHoYee_AirJump0").GetComponent<SpriteRenderer>().sprite = AssetLoader.cachePlayerSprites[name];
        }
    }

    private void airJump() {
        if (GameObject.Find("Effect_AirJump(Clone)/Effect_HoHoYee_AirJump0") != null) {
            var name = GameObject.Find("Effect_AirJump(Clone)/Effect_HoHoYee_AirJump0").GetComponent<SpriteRenderer>().sprite.name;
            GameObject.Find("Effect_AirJump(Clone)/Effect_HoHoYee_AirJump0").GetComponent<SpriteRenderer>().sprite = AssetLoader.cachePlayerSprites[name];
        }
    }

    private void UCAroundEffect() {
        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/Effect_ParryCounterAttack0") != null) {
            var spriteName = GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/Effect_ParryCounterAttack0").GetComponent<SpriteRenderer>().sprite.name;
            GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/Effect_ParryCounterAttack0").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheParrySprites[spriteName];
        }
    }

    private void UCSuccess() {
        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging") != null) {
            GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging").GetComponent<ParticleSystemRenderer>().materials[0].SetTexture("_MainTex", AssetLoader.cacheParrySprites["UCSuccess"].texture);
            GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging").GetComponent<ParticleSystem>().startColor = UCSuccessColor.Value;
        }
    }

    private void UCCharging() {
        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging C") != null) {
            GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging C").GetComponent<ParticleSystemRenderer>().materials[0].SetTexture("_MainTex", AssetLoader.cacheParrySprites["UCCharging"].texture);
            GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_TAICHIParry/P_Charging C").GetComponent<ParticleSystem>().startColor = UCChargingColor.Value;
        }
    }

    private void TalismanBall() {
        if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo") != null) {
            if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo").activeSelf) {
                for (int i = 1; i < 6; i++) {
                    var ball = GameObject.Find($"GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo/FooDots/D{i}/FooDot ({i})/JENG/Ball");
                    if (ball != null) {
                        var ballSpriteName = ball.GetComponent<SpriteRenderer>().sprite.name;
                        if (ballSpriteName == "") continue;
                        if (AssetLoader.cacheTalismanBallSprites.ContainsKey(ballSpriteName)) {
                            ball.GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheTalismanBallSprites[ballSpriteName];
                        }
                    }
                }
            }
        }
    }

    private void PlayerSprite() {
        if (Player.i != null && Player.i.PlayerSprite != null && Player.i.PlayerSprite.sprite != null) {
            string spriteName = Player.i.PlayerSprite.sprite.name;
            if (AssetLoader.cachePlayerSprites.ContainsKey(spriteName)) {
                Player.i.PlayerSprite.sprite = AssetLoader.cachePlayerSprites[spriteName];
            }
        }
    }

    private void reload() {
        AssetLoader.Init();
        changeMenuLogo();
        changeUIChiBall();

        // Not Accurate
        imPerfectParry();
    }

    private void OnDestroy() {
        // Make sure to clean up resources here to support hot reloading

        harmony.UnpatchSelf();
    }
}