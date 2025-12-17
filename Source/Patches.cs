using HarmonyLib;
using NineSolsAPI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CustomSols;

[HarmonyPatch]
public class Patches {
    [HarmonyPatch(typeof(RCGFSM.Projectiles.PlayerArrowProjectileFollower), "Update")]
    [HarmonyPrefix]
    private static bool HookArrow(RCGFSM.Projectiles.PlayerArrowProjectileFollower __instance) {
        // 檢查 cacheBowSprites 是否為空
        if (AssetLoader.cacheBowSprites == null || AssetLoader.cacheBowSprites.Count == 0) {
            return true; // 如果為空，直接返回，跳過後續處理
        }

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

    [HarmonyPatch(typeof(PoolManager), "Borrow",
    new Type[] { typeof(PoolObject), typeof(Vector3), typeof(Quaternion), typeof(Transform), typeof(Action<PoolObject>) })]
    [HarmonyPostfix]
    public static void Postfix(ref PoolObject __result, PoolObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, Action<PoolObject> handler = null) {
        if (CustomSols.arrowInit && CustomSols.arrowInit2) return;

        if (prefab.name == "ExplodingArrow Shooter 爆破發射器 Lv3") {
            var obj = __result.gameObject;
            CustomSols.UpdateBowSprite(obj, "Exploding Arrow/ExplodingArrow/ExplodingArrow");
            CustomSols.UpdateBowSprite(obj, "Exploding Arrow/ExplodingArrow/ChasingArrowLight");
            CustomSols.UpdateBowSprite(obj, "Exploding Arrow/EnergyBall/Core");
            CustomSols.arrowInit = true;
        }

        if (prefab.name == "Explosion Damage 爆破箭 閃電 lv3") {
            CustomSols.UpdateBowSprite(__result.gameObject, "ATTACK/Core");
            CustomSols.arrowInit2 = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.SetPlayerView))]
    private static void CatchCutsceneDummy(bool active) {
        if (active == false) { // 進入劇情模式
            if (Player.i == null || Player.i.replacePlayer == null) return;

            var dummyRenderer = Player.i.replacePlayer.transform.parent.GetComponentInChildren<SpriteRenderer>(true);
            if (dummyRenderer) {
                // 1. 放入 List (給統一替換邏輯用)
                if (!CustomSols.DummyRenderers.Contains(dummyRenderer)) {
                    CustomSols.DummyRenderers.Add(dummyRenderer);
                }
                // 2. 賦值給特定變數 (給 Toast 功能用)
                CustomSols.CurrentDummyRenderer = dummyRenderer;
            }
        } else {
            // 離開劇情模式時，清理特定引用
            CustomSols.CurrentDummyRenderer = null;
        }
    }

    // 輔助方法：確保不重複添加且非空
    private static void AddToDummyList(SpriteRenderer renderer) {
        if (renderer != null && !CustomSols.DummyRenderers.Contains(renderer)) {
            CustomSols.DummyRenderers.Add(renderer);
        }
    }

    // 1. RootDummy Patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(_2dxFX_Twist), "OnEnable")]
    private static void CatchRootDummy(_2dxFX_Twist __instance) {
        var renderer = __instance.transform.GetComponentInChildren<SpriteRenderer>(true);
        AddToDummyList(renderer);
    }

    // 2. ElevatorDummy Patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(_2_Art._2_Character.HoHoYee.DummyPlayer.ScaleFollowByPlayerFacing), "OnEnable")]
    private static void CatchElevatorDummy(_2_Art._2_Character.HoHoYee.DummyPlayer.ScaleFollowByPlayerFacing __instance) {
        var renderer = __instance.transform.GetComponentInChildren<SpriteRenderer>(true);
        AddToDummyList(renderer);
    }

    // 3. LevelUpDummy Patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(_2dxFX_NewTeleportation2), "OnEnable")]
    private static void CatchLevelUpDummy(_2dxFX_NewTeleportation2 __instance) {
        var renderer = __instance.transform.GetComponentInChildren<SpriteRenderer>(true);
        AddToDummyList(renderer);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(_2dxFX_GoldFX), "OnEnable")]
    private static void CatchHealDummy(_2dxFX_GoldFX __instance) {
        // 取得父物件下所有的 SpriteRenderer (包含隱藏的)
        var renderers = __instance.transform.parent.GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers != null && renderers.Length > 0) {
            foreach (var r in renderers) {
                // 防止重複加入同一個渲染器
                if (!CustomSols.DummyRenderers.Contains(r)) {
                    CustomSols.DummyRenderers.Add(r);
                }
            }
        }
    }
}