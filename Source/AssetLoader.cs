using BepInEx;
using Newtonsoft.Json;
using NineSolsAPI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

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
    public static readonly Dictionary<string, Sprite> cacheYingZhaoSprites = new Dictionary<string, Sprite>();

    public static Color? normalHpColor = null;
    public static Color? internalHpColor = null;
    public static Color? expRingOuterColor = null;
    public static Color? expRingInnerColor = null;
    public static Color? RageBarColor = null;
    public static Color? RageBarFrameColor = null;
    public static Color? ArrowLineBColor = null;
    public static Color? ArrowGlowColor = null;
    public static Color? ChiBallLeftLineColor = null;
    public static Color? ButterflyRightLineColor = null;
    public static Color? CoreCColor = null;
    public static Color? CoreDColor = null;

    public static Vector3? NormalArrowLv1Pos = null;
    public static Vector3? NormalArrowLv2Pos = null;
    public static Vector3? NormalArrowLv3Pos = null;

    public static void Init() {
        ColorFieldNull();

        string basePath =
#if DEBUG
            @"E:\Games\Nine Sols1030\BepInEx\plugins\CustomSols\CustomSols";
#else
            Path.Combine(Paths.ConfigPath, "CustomSols");
#endif
        assetFolder = Path.Combine(basePath, CustomSols.currSkinFolder ?? "Default");

#if DEBUG
        ToastManager.Toast($"Load Directory：{assetFolder}");
#endif

        if (!Directory.Exists(assetFolder)) {
#if DEBUG
            ToastManager.Toast($"Error：Directory Not Exist：{assetFolder}");
#endif
            return;
        }

        // 這裡是你定義的規則表，完全保留
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
                if (filename.Equals("circle_mask")) return (new Vector2(0.5f, 0.5f), new Vector4(240f, 0f, 205f, 0f), 100.0f);
                if (filename.Equals("ExplosionCenter")) return (new Vector2(0.5f, 0.5f), new Vector4(240f, 0f, 205f, 0f), 100.0f);
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
            }) },
            { "YingZhao", (cacheYingZhaoSprites, new Vector2(0.5f, 0.5f), 8.0f, null) }
        };

        foreach (var (folderName, (cache, pivot, ppu, selector)) in folders) {
            string folderPath = Path.Combine(assetFolder, folderName);
            if (Directory.Exists(folderPath)) {
                LoadSpritesSync(folderPath, cache, pivot, ppu, selector);
            } else {
                // 目錄不存在時，只清除 List，不銷毀 Texture
                cache.Clear();
            }
        }

        LoadConfigs();
    }

    private static void LoadConfigs() {
        var jsonFilePath = Path.Combine(assetFolder, "UI", "color.json");
        if (File.Exists(jsonFilePath)) {
            try {
                string jsonContent = File.ReadAllText(jsonFilePath);
                ColorConfig config = JsonConvert.DeserializeObject<ColorConfig>(jsonContent);
                if (config != null) {
                    TrySetColor(ref normalHpColor, config.NormalHpColor);
                    TrySetColor(ref internalHpColor, config.InternalHpColor);
                    TrySetColor(ref expRingOuterColor, config.ExpRingOuterColor);
                    TrySetColor(ref expRingInnerColor, config.ExpRingInnerColor);
                    TrySetColor(ref RageBarColor, config.RageBarColor);
                    TrySetColor(ref RageBarFrameColor, config.RageBarFrameColor);
                    TrySetColor(ref ArrowLineBColor, config.ArrowLineBColor);
                    TrySetColor(ref ArrowGlowColor, config.ArrowGlowColor);
                    TrySetColor(ref ChiBallLeftLineColor, config.ChiBallLeftLineColor);
                    TrySetColor(ref ButterflyRightLineColor, config.ButterflyRightLineColor);
                    TrySetColor(ref CoreCColor, config.CoreCColor);
                    TrySetColor(ref CoreDColor, config.CoreDColor);
                }
            } catch (Exception ex) {
                ToastManager.Toast($"Color Config Error: {ex.Message}");
            }
        }

        var bowJsonFilePath = Path.Combine(assetFolder, "Bow", "bow.json");
        if (File.Exists(bowJsonFilePath)) {
            try {
                string jsonContent = File.ReadAllText(bowJsonFilePath);
                BowConfig config = JsonConvert.DeserializeObject<BowConfig>(jsonContent);
                if (config != null) {
                    TrySetVector3(ref NormalArrowLv1Pos, config.NormalArrowLv1);
                    TrySetVector3(ref NormalArrowLv2Pos, config.NormalArrowLv2);
                    TrySetVector3(ref NormalArrowLv3Pos, config.NormalArrowLv3);
                }
            } catch (Exception ex) {
                ToastManager.Toast($"Bow Config Error: {ex.Message}");
            }
        }
    }

    private static void LoadSpritesSync(
        string folder,
        Dictionary<string, Sprite> cache,
        Vector2 defaultPivot,
        float defaultPpu,
        Func<string, (Vector2 pivot, Vector4 border, float? ppu)?> pivotBorderSelector = null) {

        // 修正重點：只清除引用，不要 Destroy Texture
        cache.Clear();

        string[] files;
        try {
            files = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
        } catch { return; }

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
            if (sprite != null) {
                cache[filename] = sprite;
            }
        }
    }

    public static string[] GetAllDirectories(string directory) {
        try {
            return Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
        } catch {
            return Array.Empty<string>();
        }
    }

    public static Sprite LoadSprite(string file, Vector2 pivot, float pixelsPerUnit, Vector4 border = default) {
        try {
            var data = File.ReadAllBytes(file);

            // 保持優化：TextureFormat.RGBA32 和 mipChain: false
            // 這會顯著減少記憶體佔用，且不會造成崩潰
            var tex2D = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (tex2D.LoadImage(data)) {
                // 保持優化：使用 Clamp 防止圖片邊緣溢出雜色
                tex2D.wrapMode = TextureWrapMode.Clamp;

                var filename = Path.GetFileNameWithoutExtension(file);

                Sprite sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
                sprite.name = filename;
                return sprite;
            }

            // 如果讀取失敗才銷毀這個空殼
            Object.Destroy(tex2D);
            return null;
        } catch (Exception ex) {
            ToastManager.Toast($"Error loading {Path.GetFileName(file)}: {ex.Message}");
            return null;
        }
    }

    // Helper 方法
    private static void TrySetVector3(ref Vector3? field, float[] vectorArray) {
        if (vectorArray != null && vectorArray.Length == 3) {
            field = new Vector3(vectorArray[0], vectorArray[1], vectorArray[2]);
        }
    }

    private static void TrySetColor(ref Color? field, string hexColor) {
        if (!string.IsNullOrWhiteSpace(hexColor) && hexColor != "#" && ColorUtility.TryParseHtmlString(hexColor, out Color color)) {
            field = color;
        }
    }

    private static void ColorFieldNull() {
        normalHpColor = null;
        internalHpColor = null;
        expRingOuterColor = null;
        expRingInnerColor = null;
        RageBarColor = null;
        RageBarFrameColor = null;
        ArrowLineBColor = null;
        ArrowGlowColor = null;
        ChiBallLeftLineColor = null;
        ButterflyRightLineColor = null;
        CoreCColor = null;
        CoreDColor = null;
    }
}