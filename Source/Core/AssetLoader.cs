using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CustomSols.Core;

public static class AssetLoader {
    public static readonly string RootPath = Path.Combine(Paths.ConfigPath, "CustomSols");
    private static readonly Dictionary<string, Texture2D> TextureCache = new();

    public static bool IsDefaultSkin { get; private set; } = true;
    public static ColorConfig? CurrentColors { get; private set; }
    public static BowConfig? CurrentBowSettings { get; private set; }
    public static List<string> AvailableSkins { get; private set; } = new();

    public static bool TryGetTexture(string name, out Texture2D texture) 
    {
        if (TextureCache.TryGetValue(name, out var tex) && tex != null) 
        {
            texture = tex;
            return true;
        }
        texture = null!;
        return false;
    }

    public static void Init() 
    {
        if (!Directory.Exists(RootPath)) 
            Directory.CreateDirectory(RootPath);
    }

    public static void DiscoverSkins()
    {
        AvailableSkins.Clear();
        AvailableSkins.Add("Default");

        if (!Directory.Exists(RootPath)) return;

        foreach (var dir in Directory.GetDirectories(RootPath)) 
        {
            if (new DirectoryInfo(dir).Name != "Default") 
                AvailableSkins.Add(new DirectoryInfo(dir).Name);
        }
    }

    public static void LoadSkin(string skinName) 
    {
        TextureCache.Clear();
        CurrentColors = null;
        CurrentBowSettings = null;
        IsDefaultSkin = skinName == "Default";

        if (IsDefaultSkin) return;

        string skinPath = Path.Combine(RootPath, skinName);

        if (!Directory.Exists(skinPath)) return;

        foreach (var file in Directory.GetFiles(skinPath, "*.png", SearchOption.AllDirectories)) 
        {
            if (LoadTextureFromDisk(file) is Texture2D tex)
                TextureCache[Path.GetFileNameWithoutExtension(file).Trim()] = tex;
        }

        try 
        {
            string jsonPath = Path.Combine(skinPath, "skinConfig.json");
            if (File.Exists(jsonPath)) {
                string jsonText = File.ReadAllText(jsonPath);
                CurrentColors = JsonConvert.DeserializeObject<ColorConfig>(jsonText);
                CurrentBowSettings = JsonConvert.DeserializeObject<BowConfig>(jsonText);
            } else { // Old Version
                string colorJson = File.ReadAllText(Path.Combine(skinPath, "UI", "color.json"));
                CurrentColors = JsonConvert.DeserializeObject<ColorConfig>(colorJson);

                string bowJson = File.ReadAllText(Path.Combine(skinPath, "Bow", "bow.json"));
                CurrentBowSettings = JsonConvert.DeserializeObject<BowConfig>(bowJson);
            }
        } 
        catch (Exception ex) 
        {
            Log.Error($"[CustomSols] JSON Config Error: {ex.Message}");
        }
    }

    private static Texture2D? LoadTextureFromDisk(string path) 
    {
        try 
        {
            byte[] data = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(data)) 
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.name = Path.GetFileNameWithoutExtension(path);
                return tex;
            }
        } 
        catch (Exception ex) 
        { 
            Log.Error(ex); 
        }
        return null;
    }
}