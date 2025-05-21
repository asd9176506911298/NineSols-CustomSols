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
    private ConfigEntry<bool> testSprite = null!;
    private ConfigEntry<KeyboardShortcut> somethingKeyboardShortcut = null!;

    private Harmony harmony = null!;

    private void Awake() {
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        // Load patches from any class annotated with @HarmonyPatch
        harmony = Harmony.CreateAndPatchAll(typeof(CustomSols).Assembly);

        testSprite = Config.Bind("General.Something", "Enable", true, "Enable the thing");
        somethingKeyboardShortcut = Config.Bind("General.Something", "Shortcut",
            new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl), "Shortcut to execute");

        KeybindManager.Add(this, TestMethod, () => somethingKeyboardShortcut.Value);
        
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start() {
        AssetLoader.Init();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        //MenuLogo
        if (SceneManager.GetActiveScene().name == "TitleScreenMenu") {
            if (GameObject.Find("MenuLogic/MainMenuLogic/Providers/MenuUIPanel/Logo") != null) {
                if (AssetLoader.cachePlayerSprites.ContainsKey("9sLOGO_1")) {
                    GameObject.Find("MenuLogic/MainMenuLogic/Providers/MenuUIPanel/Logo").GetComponent<UnityEngine.UI.Image>().sprite = AssetLoader.cacheMenuLogoSprites["9sLOGO_1"];
                }
            }
        }

        ////UI Chi ParryBall
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

    private void LateUpdate() {
        if (!testSprite.Value) return;

        var player = Player.i;
        if (player != null && player.PlayerSprite != null && player.PlayerSprite.sprite != null) {
            string spriteName = player.PlayerSprite.sprite.name;
            if (AssetLoader.cachePlayerSprites.ContainsKey(spriteName)) {
                player.PlayerSprite.sprite = AssetLoader.cachePlayerSprites[spriteName];
            }
        }

        if(GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo").activeSelf) {
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
        

        //if (GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo/FooDots/D1/FooDot (1)/JENG/Ball") != null) {
        //    var spriteName = GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo/FooDots/D1/FooDot (1)/JENG/Ball").GetComponent<SpriteRenderer>().sprite.name;
        //    if (spriteName == "") return;
        //    string path = $"E:\\Games\\Nine Sols1030\\NineSols_Data\\extract\\testSkin2\\Sprite\\{GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo/FooDots/D1/FooDot (1)/JENG/Ball").GetComponent<SpriteRenderer>().sprite.name}.png";
        //    if (File.Exists(path)) {
        //        byte[] data = File.ReadAllBytes(path);
        //        Texture2D tex2D = new Texture2D(2, 2);
        //        if (tex2D.LoadImage(data)) {
        //            GameObject.Find("GameCore(Clone)/RCG LifeCycle/PPlayer/RotateProxy/SpriteHolder/PlayerSprite/Effect_Foo/FooDots/D1/FooDot (1)/JENG/Ball").GetComponent<SpriteRenderer>().sprite = AssetLoader.cacheTalismanBallSprites["Effect_ParryCounterPrepare10"];
        //        }
        //    } else {
        //        Debug.LogWarning($"File not exist: {path}");
        //    }
        //}

    }

    private void TestMethod() {
        //ToastManager.Toast(Assembly.GetExecutingAssembly().Location);
        AssetLoader.Init();
    }

    private void OnDestroy() {
        // Make sure to clean up resources here to support hot reloading

        harmony.UnpatchSelf();
    }
}