using BepInEx;
using NineSolsAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomSols {
    public class AssetLoader {
        private static string assetFolder = Assembly.GetExecutingAssembly().Location + "Asset";
        private static string playerFolder = assetFolder + "Player";
        private static string menuFolder = assetFolder + "MenuLogo";
        private static string uiChiBallFolder = assetFolder + "UIParryBall";
        private static string talismanBallFolder = assetFolder + "TalismanBall";
        private static string parryFolder = assetFolder + "Parry";
        private static string swordFolder = assetFolder + "Sword";
        private static string bowFolder = assetFolder + "Bow";
        private static string fooFolder = assetFolder + "Foo";

        public readonly static Dictionary<string, Sprite> cachePlayerSprites = new Dictionary<string, Sprite>();
        public readonly static Dictionary<string, Sprite> cacheMenuLogoSprites = new Dictionary<string, Sprite>();
        public readonly static Dictionary<string, Sprite> cacheUIChiBallSprites = new Dictionary<string, Sprite>();
        public readonly static Dictionary<string, Sprite> cacheTalismanBallSprites = new Dictionary<string, Sprite>();
        public readonly static Dictionary<string, Sprite> cacheParrySprites = new Dictionary<string, Sprite>();
        public readonly static Dictionary<string, Sprite> cacheSwordSprites = new Dictionary<string, Sprite>();
        public readonly static Dictionary<string, Sprite> cacheBowSprites = new Dictionary<string, Sprite>();
        public readonly static Dictionary<string, Sprite> cacheFooSprites = new Dictionary<string, Sprite>();

        public static void Init() {
            #if DEBUG
                assetFolder = "E:\\Games\\Nine Sols1030\\BepInEx\\plugins\\CustomSols\\Asset";
                playerFolder = "E:\\Games\\Nine Sols1030\\BepInEx\\plugins\\CustomSols\\Asset\\Player";
                menuFolder = "E:\\Games\\Nine Sols1030\\BepInEx\\plugins\\CustomSols\\Asset\\MenuLogo";
                uiChiBallFolder = "E:\\Games\\Nine Sols1030\\BepInEx\\plugins\\CustomSols\\Asset\\UIParryBall";
                talismanBallFolder = "E:\\Games\\Nine Sols1030\\BepInEx\\plugins\\CustomSols\\Asset\\TalismanBall";
                parryFolder = "E:\\Games\\Nine Sols1030\\BepInEx\\plugins\\CustomSols\\Asset\\Parry";
                swordFolder = "E:\\Games\\Nine Sols1030\\BepInEx\\plugins\\CustomSols\\Asset\\Sword";
                bowFolder = "E:\\Games\\Nine Sols1030\\BepInEx\\plugins\\CustomSols\\Asset\\Bow";
                fooFolder = "E:\\Games\\Nine Sols1030\\BepInEx\\plugins\\CustomSols\\Asset\\Foo";
        #endif

            cachePlayerSprites.Clear();
            cacheMenuLogoSprites.Clear();
            cacheUIChiBallSprites.Clear();
            cacheTalismanBallSprites.Clear();
            cacheParrySprites.Clear();
            cacheSwordSprites.Clear();
            cacheBowSprites.Clear();

            Vector2 playerPivot = new Vector2(0.5f, 0f);
            var spriteFiles = GetAllFilesWithExtensions(playerFolder, "png");
            foreach (var file in spriteFiles) {
                string filename = Path.GetFileNameWithoutExtension(file);
                var sprite = LoadSprite(file, playerPivot, 8.0f);
                if(sprite != null) {
                    if (!cachePlayerSprites.ContainsKey(filename)) {
                        cachePlayerSprites.Add(filename, sprite);
                    }
                }
            }

            var menuLogoFiles = GetAllFilesWithExtensions(menuFolder, "png");
            foreach (var file in menuLogoFiles) {
                string filename = Path.GetFileNameWithoutExtension(file);
                var sprite = LoadSprite(file, new Vector2(0.5f, 0f), 8.0f);
                if (sprite != null) {
                    if (!cacheMenuLogoSprites.ContainsKey(filename)) {
                        cacheMenuLogoSprites.Add(filename, sprite);
                    }
                }
            }

            var uIParryBallFiles = GetAllFilesWithExtensions(uiChiBallFolder, "png");
            Vector2 uiParryBallPivot = new Vector2(0.5f, 0.5f);
            foreach (var file in uIParryBallFiles) {
                string filename = Path.GetFileNameWithoutExtension(file);
                var sprite = LoadSprite(file, uiParryBallPivot, 2.0f);
                if (sprite != null) {
                    if (!cacheUIChiBallSprites.ContainsKey(filename)) {
                        cacheUIChiBallSprites.Add(filename, sprite);
                    }
                }
            }

            var talismanBallFiles = GetAllFilesWithExtensions(talismanBallFolder, "png");
            Vector2 talismanBallPivot = new Vector2(0.18f, -1.2f);
            foreach (var file in talismanBallFiles) {
                string filename = Path.GetFileNameWithoutExtension(file);
                var sprite = LoadSprite(file, talismanBallPivot, 8.0f);
                if (sprite != null) {
                    if (!cacheTalismanBallSprites.ContainsKey(filename)) {
                        cacheTalismanBallSprites.Add(filename, sprite);
                    }
                }
            }

            var parryFiles = GetAllFilesWithExtensions(parryFolder, "png");
            Vector2 parryPivot = new Vector2(0.5f, 0f);
            foreach (var file in parryFiles) {
                string filename = Path.GetFileNameWithoutExtension(file);
                if (filename.StartsWith("ParrySparkAccurate"))
                    parryPivot = new Vector2(0.5f, 0.5f);
                var sprite = LoadSprite(file, parryPivot, 8.0f);
                if (sprite != null) {
                    if (!cacheParrySprites.ContainsKey(filename)) {
                        cacheParrySprites.Add(filename, sprite);
                    }
                }
            }

            var swordFiles = GetAllFilesWithExtensions(swordFolder, "png");
            Vector2 swordPivot = new Vector2(0.5f, 0.5f);
            foreach (var file in swordFiles) {
                string filename = Path.GetFileNameWithoutExtension(file);
                var sprite = LoadSprite(file, swordPivot, 8.0f);
                if (sprite != null) {
                    if (!cacheSwordSprites.ContainsKey(filename)) {
                        cacheSwordSprites.Add(filename, sprite);
                    }
                }
            }

            var bowFiles = GetAllFilesWithExtensions(bowFolder, "png");
            Vector2 bowPivot = new Vector2(0.5f, 0.5f);
            foreach (var file in bowFiles) {
                string filename = Path.GetFileNameWithoutExtension(file);
                Sprite sprite = null;
                if (filename.StartsWith("Lv1光束")) {
                    bowPivot = new Vector2(0f, 0.5f);
                    sprite = LoadSprite(file, bowPivot, 8.0f, new Vector4(212f,0f,212f,0f));
                }else if (filename.StartsWith("Lv2光束")) {
                    bowPivot = new Vector2(0f, 0.5f);
                    sprite = LoadSprite(file, bowPivot, 8.0f, new Vector4(220f, 0f, 220f, 0f));
                } else if (filename.StartsWith("Lv3光束")) {
                    bowPivot = new Vector2(0f, 0.5f);
                    sprite = LoadSprite(file, bowPivot, 8.0f, new Vector4(240f, 0f, 205f, 0f));
                } else
                    sprite = LoadSprite(file, bowPivot, 8.0f);
                if (sprite != null) {
                    if (!cacheBowSprites.ContainsKey(filename)) {
                        cacheBowSprites.Add(filename, sprite);
                    }
                }
            }

            var fooFiles = GetAllFilesWithExtensions(fooFolder, "png");
            Vector2 fooPivot = new Vector2(0.5f, 0.5f);
            foreach (var file in fooFiles) {
                string filename = Path.GetFileNameWithoutExtension(file);
                var sprite = LoadSprite(file, fooPivot, 8.0f);
                if (sprite != null) {
                    if (!cacheFooSprites.ContainsKey(filename)) {
                        cacheFooSprites.Add(filename, sprite);
                    }
                }
            }
            foreach (var x in cacheFooSprites) {
                ToastManager.Toast(x.Key);
            }
        }

        public static string[] GetAllFilesWithExtensions(string directory, params string[] extensions) {
            return extensions.SelectMany(extension => Directory.GetFiles(directory, "*." + extension, SearchOption.TopDirectoryOnly)).ToArray();
        }

        public static Sprite LoadSprite(string file, Vector2 customPivot = default, float customPixelPerUnit = 8.0f, Vector4 border = default) {
            try {
                if (!File.Exists(file)) {
                    ToastManager.Toast($"File does not exist: {file}");
                    return null;
                }
                Vector2 pivot = new Vector2(0.5f, 0f);
                float pixelPerUnit = 8.0f;
                if (customPivot != null)
                    pivot = customPivot;
                if (customPixelPerUnit != null)
                    pixelPerUnit = customPixelPerUnit;

                byte[] data = File.ReadAllBytes(file);
                Texture2D tex2D = new Texture2D(2, 2);
                string filename = Path.GetFileNameWithoutExtension(file);
                if (tex2D.LoadImage(data)) {
                    Sprite sprite = null;
                    if (filename.StartsWith("Lv1光束"))
                        sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), pivot, pixelPerUnit,0,SpriteMeshType.FullRect, border);
                    else
                        sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), pivot, pixelPerUnit);
                    sprite.name = filename;
                    return sprite;
                } else {
                    ToastManager.Toast($"Failed to load sprite: {file}");
                    return null;
                }
            } catch (Exception ex) {
                ToastManager.Toast($"Expection: {file}: {ex.Message}");
                return null;
            }
        }
    }
}
