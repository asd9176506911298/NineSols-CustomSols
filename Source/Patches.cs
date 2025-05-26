using HarmonyLib;
using NineSolsAPI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CustomSols;

[HarmonyPatch]
public class Patches {
    [HarmonyPatch(typeof(RCGFSM.Projectiles.PlayerArrowProjectileFollower), "Update")]
    [HarmonyPrefix]
    private static bool HookArrow(RCGFSM.Projectiles.PlayerArrowProjectileFollower __instance) {
        if (!CustomSols.instance.isEnableBow.Value) return true;

        var arrow = __instance;
        var spritePaths = new[] {
            "Projectile FSM/FSM Animator/View/ChasingArrow/ChasingArrowLight",
            "Projectile FSM/FSM Animator/View/ChasingArrow/Parent 刺/刺/刺",
            "Projectile FSM/FSM Animator/View/ChasingArrow/Parent 刺/刺 (1)/刺"
        };

        foreach (var path in spritePaths) {
            var renderer = arrow.transform.Find(path)?.GetComponent<SpriteRenderer>();
            if (renderer != null && AssetLoader.cacheBowSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
                renderer.sprite = sprite;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(SpriteRenderer), "sprite", MethodType.Setter)]
    [HarmonyPostfix]
    private static void SpriteRendererPatch(SpriteRenderer __instance, Sprite value) {
        if (!CustomSols.instance.isEnableBow.Value && !CustomSols.instance.isEnableSword.Value) return;

        var go = __instance.gameObject;
        var path = CustomSols.GetGameObjectPath(go);
        var spriteName = value.name;

        if (CustomSols.instance.isEnableBow.Value && CustomSols.bowSpritePaths.Contains(path) && AssetLoader.cacheBowSprites.TryGetValue(spriteName, out var bowSprite)) {
            __instance.sprite = bowSprite;
        } else if (CustomSols.instance.isEnableSword.Value && CustomSols.swordSpritePaths.Contains(path) && AssetLoader.cacheSwordSprites.TryGetValue(spriteName, out var swordSprite)) {
            __instance.sprite = swordSprite;
        }
    }
}