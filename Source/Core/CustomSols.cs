using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NineSolsAPI;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CustomSols.Core;

[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CustomSols : BaseUnityPlugin 
{
    private Harmony harmony = null!;
    private ConfigEntry<string> selectedSkin = null!;
    private ConfigEntry<KeyboardShortcut> reloadShortcut = null!;
    private AcceptableValueList<string> skinList = null!;
    private ConfigEntry<bool> openFolder = null!;


    public static SpriteRenderer? CurrentDummyRenderer = null;

    public void Awake() {
        harmony = Harmony.CreateAndPatchAll(typeof(CustomSols).Assembly);
        Log.Init(Logger);

        AssetLoader.Init();
        AssetLoader.DiscoverSkins();
        InitializeConfigs();
        ReloadSkin(selectedSkin.Value);

        SceneManager.sceneLoaded += (s, m) => 
        {
            SpriteManager.ClearGeneratedCache();
            SpriteManager.InitializeSceneData(); 
            RefreshAllActiveSprites();
        };

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loaded.");
    }

    private void InitializeConfigs() 
    {
        skinList = new AcceptableValueList<string>(AssetLoader.AvailableSkins.ToArray());
        selectedSkin = Config.Bind("General", "Selected Skin", "Default", new ConfigDescription("Choose your skin.", skinList));
        reloadShortcut = Config.Bind("General", "Reload Shortcut", new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl), "Reload skins.");
        openFolder = Config.Bind("Folder", "Open CustomSols Folder", false, "");


        selectedSkin.SettingChanged += (s, e) => ReloadSkin(selectedSkin.Value);
        KeybindManager.Add(this, () => 
        {
            AssetLoader.DiscoverSkins();
            skinList = new AcceptableValueList<string>(AssetLoader.AvailableSkins.ToArray());
            ReloadSkin(selectedSkin.Value);
            ToastManager.Toast("Skins Reloaded");
        }, reloadShortcut);

        openFolder.SettingChanged += (sender, args) => {
            Process.Start(AssetLoader.RootPath);
        };
    }

    public void LateUpdate() 
    {
        if (AssetLoader.IsDefaultSkin) return;

        UpdateSpriteOverride(Player.i?.PlayerSprite);
        UpdateSpriteOverride(CurrentDummyRenderer);
        SpriteManager.OnLateUpdate();
    }

    private void UpdateSpriteOverride(SpriteRenderer? sr) 
    {
        if (sr == null || sr.sprite == null) return;

        if (SpriteManager.TryReplaceSprite(sr.sprite, out Sprite newSprite)) 
        {
            if (sr.sprite != newSprite) 
                sr.sprite = newSprite;
        }
    }

    private void ReloadSkin(string skinName) 
    {
        SpriteManager.ClearGeneratedCache();
        AssetLoader.LoadSkin(skinName);
        SpriteManager.InitializeSceneData();
        RefreshAllActiveSprites();
    }

    private void RefreshAllActiveSprites() {
        try 
        {
            foreach (var sr in FindObjectsOfType<SpriteRenderer>(true)) 
            {
                if (SpriteManager.TryReplaceSprite(sr.sprite, out Sprite newSprite))
                    sr.sprite = newSprite;
            }
            foreach (var img in FindObjectsOfType<Image>(true)) 
            {
                if (img.sprite != null && SpriteManager.TryReplaceSprite(img.sprite, out Sprite newSprite))
                    img.sprite = newSprite;
            }
        } finally { }
    }

    public void OnDestroy() => harmony.UnpatchSelf();
}