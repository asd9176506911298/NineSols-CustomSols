using HarmonyLib;
using RCGFSM.Projectiles;
using System;
using UnityEngine;
using UnityEngine.UI;
using CustomSols.Core;

namespace CustomSols;

[HarmonyPatch]
public static class Patches {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerArrowProjectileFollower), "Update")]
    private static bool HookArrow(PlayerArrowProjectileFollower __instance) 
    {
        if (AssetLoader.IsDefaultSkin) return true;

        string[] paths = 
        {
            "Projectile FSM/FSM Animator/View/ChasingArrow /ChasingArrowLight",
            "Projectile FSM/FSM Animator/View/ChasingArrow /Parent 刺/刺/刺",
            "Projectile FSM/FSM Animator/View/ChasingArrow /Parent 刺/刺 (1)/刺"
        };

        foreach (string p in paths) 
            SpriteManager.UpdateObjectSprite(__instance.gameObject, p);
        
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PoolManager), "Borrow",
    [
        typeof(PoolObject),
        typeof(Vector3),
        typeof(Quaternion),
        typeof(Transform),
        typeof(Action<PoolObject>)
    ])]
    private static void PostfixBorrow(ref PoolObject __result, ref PoolObject prefab) 
    {
        if (AssetLoader.IsDefaultSkin || !__result) return;

        string prefabName = prefab.name;
        GameObject go = __result.gameObject;

        SpriteManager.TryApplySwordSprite(go, prefabName);
        SpriteManager.TryApplyArrowSprites(go, prefabName);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PoolObject), nameof(PoolObject.OnReturnToPool))]
    private static void ClearSwordReplacements(ref PoolObject __instance) 
    {
        SpriteManager.ClearSwordReplacements(__instance.GetInstanceID());
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.SetPlayerView))]
    private static void CatchCutsceneDummy(bool active) 
    {
        if (active == false) 
        {
            if (Player.i == null || Player.i.replacePlayer == null) return;

            var dummyRenderer = Player.i.replacePlayer.transform.parent.GetComponentInChildren<SpriteRenderer>(true);
            if (dummyRenderer)
                Core.CustomSols.CurrentDummyRenderer = dummyRenderer;
        } 
        else 
        {
            Core.CustomSols.CurrentDummyRenderer = null;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SpriteRenderer), nameof(SpriteRenderer.sprite), MethodType.Setter)]
    private static void OnSetSpriteRenderer(SpriteRenderer __instance, ref Sprite value) 
    {
        if (value == null || AssetLoader.IsDefaultSkin) return;
        SpriteManager.ProcessSpriteSet(__instance.GetInstanceID(), ref value);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter)]
    private static void OnSetImageSprite(Image __instance, ref Sprite value) {
        if (value == null || AssetLoader.IsDefaultSkin) return;
        SpriteManager.ProcessSpriteSet(__instance.GetInstanceID(), ref value);
    }
}