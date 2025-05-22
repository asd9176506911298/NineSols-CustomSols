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
    private ConfigEntry<KeyboardShortcut> reloadShortcut = null!;

    private Harmony harmony = null!;

    private void Awake() {
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        // Load patches from any class annotated with @HarmonyPatch
        harmony = Harmony.CreateAndPatchAll(typeof(CustomSols).Assembly);

        isEnablePlayer = Config.Bind("", "Enable Change Player Sprite", true, "");
        isEnableMenuLogo = Config.Bind("", "Enable Change Menu Logo Image", true, "");
        isEnableUIChiBall = Config.Bind("", "Enable Change UI Chi Ball", true, "");
        isEnableTalismanBall = Config.Bind("", "Enable Change Talisman Ball Image", true, "");
        reloadShortcut = Config.Bind("", "Shortcut",
            new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl), "Shortcut to execute");

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
    }

    private void LateUpdate() {
        if (isEnablePlayer.Value) 
        {
            if (Player.i != null && Player.i.PlayerSprite != null && Player.i.PlayerSprite.sprite != null) {
                string spriteName = Player.i.PlayerSprite.sprite.name;
                if (AssetLoader.cachePlayerSprites.ContainsKey(spriteName)) {
                    Player.i.PlayerSprite.sprite = AssetLoader.cachePlayerSprites[spriteName];  
                }
            }
        }


        if (isEnableTalismanBall.Value) 
        {
            if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo") != null) {
                if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo").activeSelf)
                for (int i = 1; i < 6; i++) 
                {
                    var ball = GameObject.Find($"GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo/FooDots/D{i}/FooDot ({i})/JENG/Ball");
                    if (ball != null) 
                    {
                        var ballSpriteName = ball.GetComponent<SpriteRenderer>().sprite.name;
                        if (ballSpriteName == "") continue;
                        if (AssetLoader.cacheTalismanBallSprites.ContainsKey(ballSpriteName)) 
                        {
                            ball.GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheTalismanBallSprites[ballSpriteName];
                        }
                    }
                }
            }
        }
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

    private void reload() {
        AssetLoader.Init();
        changeMenuLogo();
        changeUIChiBall();
    }

    private void OnDestroy() {
        // Make sure to clean up resources here to support hot reloading

        harmony.UnpatchSelf();
    }
}