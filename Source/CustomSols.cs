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