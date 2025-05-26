using BepInEx;
using NineSolsAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CustomSols;

public class AssetLoader {
    private static string assetFolder = Path.Combine(Paths.ConfigPath, "Asset");
    private static string playerFolder = Path.Combine(assetFolder, "Player");
    private static string menuFolder = Path.Combine(assetFolder, "MenuLogo");
    private static string uiChiBallFolder = Path.Combine(assetFolder, "UIParryBall");
    private static string talismanBallFolder = Path.Combine(assetFolder, "TalismanBall");
    private static string parryFolder = Path.Combine(assetFolder, "Parry");
    private static string swordFolder = Path.Combine(assetFolder, "Sword");
    private static string bowFolder = Path.Combine(assetFolder, "Bow");
    private static string fooFolder = Path.Combine(assetFolder, "Foo");

    public static readonly Dictionary<string, Sprite> cachePlayerSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheMenuLogoSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheUIChiBallSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheTalismanBallSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheParrySprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheSwordSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheBowSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheFooSprites = new Dictionary<string, Sprite>();

    public static void Init() {
#if DEBUG
        assetFolder = "E:\\Games\\Nine Sols1030\\BepInEx\\plugins\\CustomSols\\Asset";
        playerFolder = Path.Combine(assetFolder, "Player");
        menuFolder = Path.Combine(assetFolder, "MenuLogo");
        uiChiBallFolder = Path.Combine(assetFolder, "UIParryBall");
        talismanBallFolder = Path.Combine(assetFolder, "TalismanBall");
        parryFolder = Path.Combine(assetFolder, "Parry");
        swordFolder = Path.Combine(assetFolder, "Sword");
        bowFolder = Path.Combine(assetFolder, "Bow");
        fooFolder = Path.Combine(assetFolder, "Foo");
#endif

        playerFolder = Path.Combine(assetFolder, "Player", CustomSols.isUseExample.Value ? "example" : "");
        menuFolder = Path.Combine(assetFolder, "MenuLogo", CustomSols.isUseExample.Value ? "example" : "");
        uiChiBallFolder = Path.Combine(assetFolder, "UIParryBall", CustomSols.isUseExample.Value ? "example" : "");
        talismanBallFolder = Path.Combine(assetFolder, "TalismanBall", CustomSols.isUseExample.Value ? "example" : "");
        parryFolder = Path.Combine(assetFolder, "Parry", CustomSols.isUseExample.Value ? "example" : "");
        swordFolder = Path.Combine(assetFolder, "Sword", CustomSols.isUseExample.Value ? "example" : "");
        bowFolder = Path.Combine(assetFolder, "Bow", CustomSols.isUseExample.Value ? "example" : "");
        fooFolder = Path.Combine(assetFolder, "Foo", CustomSols.isUseExample.Value ? "example" : "");

        // 優先載入 MenuLogo
        LoadSpritesSync(menuFolder, cacheMenuLogoSprites, new Vector2(0.5f, 0f), 8.0f);
        // 其他資源
        LoadSpritesSync(playerFolder, cachePlayerSprites, new Vector2(0.5f, 0f), 8.0f);
        LoadSpritesSync(uiChiBallFolder, cacheUIChiBallSprites, new Vector2(0.5f, 0.5f), 2.0f);
        LoadSpritesSync(talismanBallFolder, cacheTalismanBallSprites, new Vector2(0.18f, -1.2f), 8.0f);
        LoadSpritesSync(parryFolder, cacheParrySprites, new Vector2(0.5f, 0f), 8.0f, filename => filename.StartsWith("ParrySparkAccurate") ? (new Vector2(0.5f, 0.5f), Vector4.zero) : null);
        LoadSpritesSync(swordFolder, cacheSwordSprites, new Vector2(0.5f, 0.5f), 8.0f);
        LoadSpritesSync(bowFolder, cacheBowSprites, new Vector2(0.5f, 0.5f), 8.0f, filename => {
            if (filename.StartsWith("Lv1光束")) return (new Vector2(0f, 0.5f), new Vector4(212f, 0f, 212f, 0f));
            if (filename.StartsWith("Lv2光束")) return (new Vector2(0f, 0.5f), new Vector4(220f, 0f, 220f, 0f));
            if (filename.StartsWith("Lv3光束")) return (new Vector2(0f, 0.5f), new Vector4(240f, 0f, 205f, 0f));
            return null;
        });
        LoadSpritesSync(fooFolder, cacheFooSprites, new Vector2(0.5f, 0.5f), 8.0f);
    }

    private static void LoadSpritesSync(string folder, Dictionary<string, Sprite> cache, Vector2 defaultPivot, float defaultPpu, Func<string, (Vector2 pivot, Vector4 border)?> pivotBorderSelector = null) {
        cache.Clear();
        var files = GetAllFilesWithExtensions(folder, "png");
        foreach (var file in files) {
            var filename = Path.GetFileNameWithoutExtension(file);
            var pivot = defaultPivot;
            Vector4 border = default;

            if (pivotBorderSelector != null) {
                var result = pivotBorderSelector(filename);
                if (result.HasValue) {
                    pivot = result.Value.pivot;
                    border = result.Value.border;
                }
            }

            var sprite = LoadSprite(file, pivot, defaultPpu, border);
            if (sprite != null && !cache.ContainsKey(filename)) {
                cache.Add(filename, sprite);
                //ToastManager.Toast($"Loaded sprite: {filename} from {file}");
            } else if (sprite == null) {
                ToastManager.Toast($"Failed to load sprite: {filename} from {file}");
            }
        }
    }

    public static string[] GetAllFilesWithExtensions(string directory, params string[] extensions) {
        try {
            return extensions.SelectMany(ext => Directory.GetFiles(directory, "*." + ext, SearchOption.TopDirectoryOnly)).ToArray();
        } catch (Exception ex) {
            ToastManager.Toast($"Failed to access directory {directory}: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    public static Sprite LoadSprite(string file, Vector2 pivot, float pixelsPerUnit, Vector4 border = default) {
        try {
            if (!File.Exists(file)) {
                ToastManager.Toast($"File does not exist: {file}");
                return null;
            }

            var data = File.ReadAllBytes(file);
            var tex2D = new Texture2D(2, 2);
            var filename = Path.GetFileNameWithoutExtension(file);

            if (tex2D.LoadImage(data)) {
                Sprite sprite = filename.StartsWith("Lv") && filename.Contains("光束")
                    ? Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border)
                    : Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), pivot, pixelsPerUnit);
                sprite.name = filename;
                return sprite;
            }

            ToastManager.Toast($"Failed to load sprite: {file}");
            return null;
        } catch (Exception ex) {
            ToastManager.Toast($"Exception loading {file}: {ex.Message}");
            return null;
        }
    }
}