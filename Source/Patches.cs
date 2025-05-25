using HarmonyLib;
using NineSolsAPI;
using UnityEngine;

namespace CustomSols;

[HarmonyPatch]
public class Patches {
    //[HarmonyPatch(typeof(Player), nameof(Player.SetStoryWalk))]
    //[HarmonyPrefix]
    //private static bool PatchStoryWalk(ref float walkModifier) {
    //    walkModifier = 1.0f;

    //    return true; // the original method should be executed
    //}

    //Chase Arrow
    [HarmonyPatch(typeof(RCGFSM.Projectiles.PlayerArrowProjectileFollower), "Update")]
    [HarmonyPrefix]
    private static bool HookArrow(ref RCGFSM.Projectiles.PlayerArrowProjectileFollower __instance) {
        //ToastManager.Toast(__instance.name);
        var arrow = __instance;
        var spriteName = "";
        SpriteRenderer spriteRenderer = arrow.transform.Find("Projectile FSM/FSM Animator/View/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>();
        //箭矢
        spriteName = arrow.transform.Find("Projectile FSM/FSM Animator/View/ChasingArrow /ChasingArrowLight").GetComponent<SpriteRenderer>().sprite.name;
        if (spriteRenderer != null && AssetLoader.cacheBowSprites.ContainsKey(spriteName)) {
            spriteRenderer.sprite = AssetLoader.cacheBowSprites[spriteName];
        }

        spriteRenderer = arrow.transform.Find("Projectile FSM/FSM Animator/View/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>();
        spriteName = arrow.transform.Find("Projectile FSM/FSM Animator/View/ChasingArrow /Parent 刺/刺/刺").GetComponent<SpriteRenderer>().sprite.name;
        if (spriteRenderer != null && AssetLoader.cacheBowSprites.ContainsKey(spriteName)) {
            spriteRenderer.sprite = AssetLoader.cacheBowSprites[spriteName];
        }
        spriteRenderer = arrow.transform.Find("Projectile FSM/FSM Animator/View/ChasingArrow /Parent 刺/刺 (1)/刺").GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && AssetLoader.cacheBowSprites.ContainsKey(spriteName)) {
            spriteRenderer.sprite = AssetLoader.cacheBowSprites[spriteName];
        }

        return true; // the original method should be executed
    }
}