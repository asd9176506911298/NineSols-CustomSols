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
            "Projectile FSM/FSM Animator/View/ChasingArrow /ChasingArrowLight",
            "Projectile FSM/FSM Animator/View/ChasingArrow /Parent 刺/刺/刺",
            "Projectile FSM/FSM Animator/View/ChasingArrow /Parent 刺/刺 (1)/刺"
        };

        foreach (var path in spritePaths) {
            var renderer = arrow.transform.Find(path)?.GetComponent<SpriteRenderer>();
            if (renderer != null && AssetLoader.cacheBowSprites.TryGetValue(renderer.sprite.name, out var sprite)) {
                renderer.sprite = sprite;
            }
        }

        return true;
    }
}