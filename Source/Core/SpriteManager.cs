using NineSolsAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomSols.Core;

public static class SpriteManager {
    private class ColorTask {
        public required string Path;
        public Color TargetColor;
    }

    private static List<ColorTask> activeColorTasks = new();

    private static readonly List<SpriteRenderer> pendingSwordReplacements = new();
    private static readonly Dictionary<int, List<SpriteRenderer>> swordRenderers = new();

    private static readonly Dictionary<string, Sprite> generatedSpriteCache = new();
    private static Dictionary<string, SpriteRenderer> sceneRendererCache = new();

    public static readonly Dictionary<string, List<(string ChildPath, string TextureName)>> SwordPoolMappings = new() {
        { "HoHoYee_AttackA_PoolObject_Variant", new() { ("Sprite", "HoHoYee_AttackA_PoolObject_Variant") } }, 
        { "HoHoYee_AttackB_PoolObject_Variant", new() { ("Sprite", "HoHoYee_AttackB_PoolObject_Variant") } },
        { "HoHoYee_AttackC ThirdAttack Effect", new() { ("Sprite", "HoHoYee_AttackC ThirdAttack Effect") } },
        { "Yee 氣刃 chi blade", new() { ("Projectile FSM/FSM Animator/View/Sprite", "Yee 氣刃 chi blade") } },
        { "HoHoYee_AttackC ThirdAttack 劍氣玉 Effect", new() { ("Sprite", "HoHoYee_AttackC ThirdAttack 劍氣玉 Effect") } },
        { "HoHoYee_Charging 蓄力攻擊特效", new() {
            ("ChargeAttackSprite", "ChargeAttackSprite"),
            ("Super Charge Ability/childNode/ChargeAttackSprite", "ChargeAttackSprite")
        }}
    };

    public static void InitializeSceneData() 
    {
        if (AssetLoader.IsDefaultSkin) return;

        ClearSceneCaches();

        var allRenderers = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
        foreach (var sr in allRenderers) {
            if (sr.gameObject.scene.rootCount == 0) continue;
            string path = GetGameObjectPath(sr.gameObject);
            sceneRendererCache[path] = sr;
        }

        SetBowPositions();
        SetupColorTasks();
    }

    public static void OnLateUpdate() 
    {
        if (AssetLoader.IsDefaultSkin) return;

        UpdateColorTasks();
        UpdateSwordEffects();
    }

    private static void UpdateColorTasks() 
    {
        foreach (var task in activeColorTasks) 
        {
            if (sceneRendererCache.TryGetValue(task.Path, out var sr) && sr != null) 
            {
                if (sr.color != task.TargetColor)
                    sr.color = task.TargetColor;
            }
        }
    }

    private static void UpdateSwordEffects() 
    {
        for (int i = pendingSwordReplacements.Count - 1; i >= 0; i--) 
        {
            var sr = pendingSwordReplacements[i];

            if (sr.sprite != null) 
            {
                if (TryReplaceSprite(sr.sprite, out Sprite newSprite))
                    sr.sprite = newSprite;
            }
        }
    }

    public static void TryApplySwordSprite(GameObject go, string prefabName) {
        string cleanName = prefabName.Replace("(Clone)", "").Trim();

        if (!SwordPoolMappings.TryGetValue(cleanName, out var mappings))
            return;

        int poolID = go.GetInstanceID();

        if (!swordRenderers.ContainsKey(poolID))
            swordRenderers[poolID] = new List<SpriteRenderer>();

        foreach (var mapping in mappings) 
        {
            var tr = go.transform.Find(mapping.ChildPath);
            if (!tr || !tr.TryGetComponent(out SpriteRenderer sr))
                continue;

            swordRenderers[poolID].Add(sr);

            if (sr.sprite != null) 
            {
                if (TryReplaceSprite(sr.sprite, out Sprite newSprite))
                    sr.sprite = newSprite;

                if (!pendingSwordReplacements.Contains(sr))
                    pendingSwordReplacements.Add(sr);
            } 
            else 
            {
                if (!pendingSwordReplacements.Contains(sr))
                    pendingSwordReplacements.Add(sr);
            }
        }
    }

    public static void ClearSwordReplacements(int poolObjectID) 
    {
        if (!swordRenderers.TryGetValue(poolObjectID, out var list))
            return;

        foreach (var sr in list)
            pendingSwordReplacements.Remove(sr);

        swordRenderers.Remove(poolObjectID);
    }

    public static void TryApplyArrowSprites(GameObject go, string prefabName) 
    {
        if (prefabName == "ExplodingArrow Shooter 爆破發射器 Lv3") 
        {
            UpdateObjectSprite(go, "Exploding Arrow/ExplodingArrow/ExplodingArrow");
            UpdateObjectSprite(go, "Exploding Arrow/ExplodingArrow/ChasingArrowLight");
            UpdateObjectSprite(go, "Exploding Arrow/EnergyBall/Core");
        } 
        else if (prefabName == "Explosion Damage 爆破箭 閃電 lv3") 
        {
            if (!AssetLoader.TryGetTexture("ExplosionCenter", out var tex)) return;
            
            var target = go.transform.Find("ATTACK/Core");
            if (target && target.TryGetComponent<SpriteRenderer>(out var sr)) 
            {
                if (TryReplaceSprite(sr.sprite, out Sprite newSprite))
                    sr.sprite = newSprite;
            }
        }
    }

    private static void SetupColorTasks() {
        activeColorTasks.Clear();
        var config = AssetLoader.CurrentColors;
        if (config == null) return;

        void AddTask(string path, string hex) 
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color c)) 
            {
                activeColorTasks.Add(new ColorTask 
                { 
                    Path = path,
                    TargetColor = c 
                });
            }
        }

        // HP & UI
        AddTask("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/HealthBarBase/HealthBar/BG renderer/Health", config.NormalHpColor);
        AddTask("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/HealthBarBase/HealthBar/BG renderer/RecoverableHealth", config.InternalHpColor);
        AddTask("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreB(ExpUILogic)", config.ExpRingOuterColor);
        AddTask("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreB(ExpUILogic)/BarFill", config.ExpRingInnerColor);

        // Rage Bar
        for (int i = 0; i <= 7; i++) 
        {
            string baseStr = i == 0 ?
                "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftDown/Bow UI Area/RageUI renderer/slots/RagePart_spr" :
                $"GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftDown/Bow UI Area/RageUI renderer/slots/RagePart_spr ({i})";
            AddTask($"{baseStr}/RageBar Frame", config.RageBarFrameColor);
            AddTask($"{baseStr}/RageBar", config.RageBarColor);
        }

        // Misc
        AddTask("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftDown/Bow UI Area/ItemSelection/CurrentItemPanel spr/Glow", config.ArrowGlowColor);
        AddTask("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftDown/Bow UI Area/RageUI renderer/ArrowLineB (1)", config.ArrowLineBColor);
        AddTask("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/ParryCharge/LineA", config.ChiBallLeftLineColor);
        AddTask("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/RightDown/Butterfly_UIHintPanel/LineA", config.ButterflyRightLineColor);
        AddTask("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreC", config.CoreCColor);
        AddTask("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/HideUIAbilityCheck/[Activate] PlayerUI Folder/PlayerInGameUI renderer/LeftTop/EXP_RING/CoreD", config.CoreDColor);
    }

    public static string GetGameObjectPath(GameObject obj) 
    {
        string text = obj.name;
        Transform transform = obj.transform;

        while (transform.parent != null) 
        {
            transform = transform.parent;
            text = transform.name + "/" + text;
        }
        return text;
    }

    public static bool TryReplaceSprite(Sprite original, out Sprite replacement, bool toast = false) 
    {
        replacement = original;

        if (original == null) 
            return false;

        string cleanName = original.name.Replace("(Clone)", "").Trim();

        if (cleanName.EndsWith("_Custom"))
            return false;

        if (!AssetLoader.TryGetTexture(cleanName, out Texture2D customTexture)) 
            return false;

        string cacheKey = $"{cleanName}_{original.pixelsPerUnit}_{original.pivot.x}_{original.pivot.y}";

        if (generatedSpriteCache.TryGetValue(cacheKey, out replacement))
            return true;
        
        Vector2 normalizedPivot = new Vector2(original.pivot.x / original.rect.width, original.pivot.y / original.rect.height);
        Rect newRect = new Rect(0, 0, customTexture.width, customTexture.height);
        Sprite newSprite = Sprite.Create(customTexture, newRect, normalizedPivot, original.pixelsPerUnit);
        newSprite.name = cleanName + "_Custom";

        generatedSpriteCache[cacheKey] = newSprite;
        replacement = newSprite;
        return true;
    }

    public static void ProcessSpriteSet(int instanceId, ref Sprite value) 
    {
        if (value == null) return;

        if (TryReplaceSprite(value, out Sprite customSprite))
            value = customSprite;
    }

    public static void UpdateObjectSprite(GameObject root, string childPath) 
    {
        if (!root) return;

        var target = root.transform.Find(childPath);
        if (target && target.TryGetComponent<SpriteRenderer>(out var sr)) 
        {
            var sprite = sr.sprite;
            ProcessSpriteSet(0, ref sprite);

            if (sr.sprite != sprite) 
                sr.sprite = sprite;
        }
    }

    private static void SetBowPositions() {
        var config = AssetLoader.CurrentBowSettings;
        if (config == null) return;

        void SetPos(string nameStart, float[]? pos) 
        {
            if (pos == null || pos.Length != 3) return;
            
            Vector3 vector = new Vector3(pos[0], pos[1], pos[2]);
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>()) 
            {
                if (go.name.StartsWith(nameStart)) 
                {
                    var beam = go.transform.Find("光束");
                    if (beam) 
                        beam.localPosition = vector;
                }
            }
        }
        SetPos("NormalArrow Shoot 穿雲 Lv1", config.NormalArrowLv1);
        SetPos("NormalArrow Shoot 穿雲 Lv2", config.NormalArrowLv2);
        SetPos("NormalArrow Shoot 穿雲 Lv3", config.NormalArrowLv3);
    }

    public static void ClearGeneratedCache() {
        generatedSpriteCache.Clear();
        ClearSceneCaches();
    }

    private static void ClearSceneCaches() {
        sceneRendererCache.Clear();
        activeColorTasks.Clear();
    }
}