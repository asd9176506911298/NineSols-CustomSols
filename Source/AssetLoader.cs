using BepInEx;
using Newtonsoft.Json;
using NineSolsAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CustomSols;

public class AssetLoader {
    private static string assetFolder;

    public static readonly Dictionary<string, Sprite> cachePlayerSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheMenuLogoSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheTalismanBallSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheParrySprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheSwordSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheBowSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheFooSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheOnlyOneSprites = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, Sprite> cacheUISprites = new Dictionary<string, Sprite>();

    public static Color? normalHpColor = null;
    public static Color? internalHpColor = null;
    public static Color? expRingOuterColor = null;
    public static Color? expRingInnerColor = null;

    public static void Init() {
        // 設置根目錄，根據 DEBUG 模式選擇路徑
        string basePath =
#if DEBUG
            @"E:\Games\Nine Sols1030\BepInEx\plugins\CustomSols\CustomSols";
#else
        Path.Combine(Paths.ConfigPath, "CustomSols");
#endif
        assetFolder = Path.Combine(basePath, CustomSols.currSkinFolder ?? "Default");
#if DEBUG
        ToastManager.Toast($"Load Directory：{assetFolder}"); // 顯示實際使用的 skin 目錄
#endif

        // 檢查 skin 目錄是否存在
        if (!Directory.Exists(assetFolder)) {
#if DEBUG
            ToastManager.Toast($"Error：Directory Not Exist：{assetFolder}");
#endif
            return;
        }

        var folders = new Dictionary<string, (Dictionary<string, Sprite> cache, Vector2 pivot, float ppu, Func<string, (Vector2 pivot, Vector4 border, float? ppu)?> selector)>
        {
            { "MenuLogo", (cacheMenuLogoSprites, new Vector2(0.5f, 0f), 8.0f, null) },
            { "Player", (cachePlayerSprites, new Vector2(0.5f, 0f), 8.0f, null) },
            { "TalismanBall", (cacheTalismanBallSprites, new Vector2(0.18f, -1.2f), 8.0f, null) },
            { "Parry", (cacheParrySprites, new Vector2(0.5f, 0f), 8.0f, filename => filename.StartsWith("ParrySparkAccurate") ? (new Vector2(0.5f, 0.5f), Vector4.zero, null) : null) },
            { "Sword", (cacheSwordSprites, new Vector2(0.5f, 0.5f), 8.0f, null) },
            { "Bow", (cacheBowSprites, new Vector2(0.5f, 0.5f), 8.0f, filename => {
                if (filename.StartsWith("Lv1光束")) return (new Vector2(0f, 0.5f), new Vector4(212f, 0f, 212f, 0f), null);
                if (filename.StartsWith("Lv2光束")) return (new Vector2(0f, 0.5f), new Vector4(220f, 0f, 220f, 0f), null);
                if (filename.StartsWith("Lv3光束")) return (new Vector2(0f, 0.5f), new Vector4(240f, 0f, 205f, 0f), null);
                return null;
            }) },
            { "Foo", (cacheFooSprites, new Vector2(0.5f, 0.5f), 8.0f, null) },
            { "PlayerSpriteAllUseThis", (cacheOnlyOneSprites, new Vector2(0.5f, 0.0f), 8.0f, null) },
            { "UI", (cacheUISprites, new Vector2(0.5f, 0.5f), 8.0f, filename => {
                if (filename.StartsWith("ChiBallLeftLine")) return (new Vector2(0.5f, 0.5f), Vector4.zero, 2.0f);
                if (filename.StartsWith("ButterflyRightLine")) return (new Vector2(0.5f, 0.5f), Vector4.zero, 2.0f);
                if (filename.Equals("ArrowLineA")) return (new Vector2(0f, 0.5f), new Vector4(94f, 0f, 125f, 0f), 2.0f);
                if (filename.StartsWith("Arrow")) return (new Vector2(0.5f, 0.5f), Vector4.zero, 1.0f);
                if (filename.StartsWith("ParryBalls")) return (new Vector2(0.5f, 0.5f), Vector4.zero, 2.0f);
                if (filename.StartsWith("Line_V")) return (new Vector2(0.5f, 0.5f), new Vector4(9f,5f,9f,5f), 2.0f);
                if (filename.StartsWith("CoreB_fill")) return (new Vector2(0.5f, 0.5f), Vector4.zero, 2.0f);
                if (filename.StartsWith("Icon_Blood")) return (new Vector2(0.5f, 0.5f), Vector4.zero, 2.0f);
                if (filename.StartsWith("Icon_BloodEmpty")) return (new Vector2(0.5f, 0.5f), Vector4.zero, 2.0f);
                if (filename.StartsWith("CoreC")) return (new Vector2(0.5f, 0.5f), Vector4.zero, 2.0f);
                if (filename.StartsWith("CoreD")) return (new Vector2(0.5f, 0.5f), Vector4.zero, 2.0f);
                return (new Vector2(0.5f, 0.5f), Vector4.zero, 8.0f);
            }) }
        };

        // 集中處理子目錄的檢查與載入
        foreach (var (folderName, (cache, pivot, ppu, selector)) in folders) {
            string folderPath = Path.Combine(assetFolder, folderName);
            if (Directory.Exists(folderPath)) {
                LoadSpritesSync(folderPath, cache, pivot, ppu, selector);
            }
        }

        var jsonFilePath = Path.Combine(assetFolder, "UI", "color.json");
        if (File.Exists(jsonFilePath)) {
            // 讀取 JSON 檔案內容
            string jsonContent = File.ReadAllText(jsonFilePath);

            // 反序列化 JSON 到物件
            ColorConfig config = JsonConvert.DeserializeObject<ColorConfig>(jsonContent);

            // 將十六進制顏色字串轉換為 Unity Color
            TrySetColor(ref normalHpColor, config.NormalHpColor);
            TrySetColor(ref internalHpColor, config.InternalHpColor);
            TrySetColor(ref expRingOuterColor, config.ExpRingOuterColor);
            TrySetColor(ref expRingInnerColor, config.ExpRingInnerColor);
        }
        //foreach (var x in cacheOnlyOneSprites)
        //    ToastManager.Toast(x.Key);
    }

    private static void LoadSpritesSync(
        string folder,
        Dictionary<string, Sprite> cache,
        Vector2 defaultPivot,
        float defaultPpu,
        Func<string, (Vector2 pivot, Vector4 border, float? ppu)?> pivotBorderSelector = null) {
        cache.Clear();
        var files = GetAllFilesWithExtensions(folder, "png");
        foreach (var file in files) {
            var filename = Path.GetFileNameWithoutExtension(file);
            var pivot = defaultPivot;
            Vector4 border = default;
            float ppu = defaultPpu;

            if (pivotBorderSelector != null) {
                var result = pivotBorderSelector(filename);
                if (result.HasValue) {
                    pivot = result.Value.pivot;
                    border = result.Value.border;
                    ppu = result.Value.ppu ?? defaultPpu;
                }
            }

            var sprite = LoadSprite(file, pivot, ppu, border);
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

    public static string[] GetAllDirectories(string directory) {
        try {
            return Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
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
                Sprite sprite = (filename.StartsWith("Lv") && filename.Contains("光束")) || filename.Equals("Line_V")
                    ? Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border)
                    : Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
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

    private static void TrySetColor(ref Color? field, string hexColor) {
        var parsed = ParseColor(hexColor);
        if (parsed.HasValue)
            field = parsed.Value;
    }

    private static Color? ParseColor(string hexColor) {
        if (string.IsNullOrWhiteSpace(hexColor) || hexColor == "#") {
            return null;
        }

        if (ColorUtility.TryParseHtmlString(hexColor, out Color color)) {
            return color;
        } else {
            return null;
        }
    }
}

public class ColorConfig {
    public string NormalHpColor { get; set; }
    public string InternalHpColor { get; set; }
    public string ExpRingOuterColor { get; set; }
    public string ExpRingInnerColor { get; set; }
}
