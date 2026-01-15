using HarmonyLib;
using NineSolsAPI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using BepInEx;
using System.IO;

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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(_2dxFX_ColorRGB), "OnEnable")]
    private static void CatchSprite(_2dxFX_ColorRGB __instance) {
        // 1. 安全檢查：確保實例本身存在
        if (__instance == null) return;

        // 2. 確定搜尋起點：如果有父物件就從父物件找，沒有就從自己找
        var searchRoot = __instance.transform.parent != null
                         ? __instance.transform.parent
                         : __instance.transform;

        // 3. 取得所有 SpriteRenderer
        var renderers = searchRoot.GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers != null && renderers.Length > 0) {
            // 4. 安全檢查：確保存放的列表已經初始化
            if (CustomSols.DummyRenderers == null) {
                CustomSols.DummyRenderers = new List<SpriteRenderer>();
            }
            
            foreach (var r in renderers) {
                // 防止重複加入
                if (!CustomSols.DummyRenderers.Contains(r)) {
                    CustomSols.DummyRenderers.Add(r);
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DialogueCharacter), "OnEnable")]
    private static void DialogueCharacter_OnEnable_Patch(DialogueCharacter __instance) {
        string fullName = __instance.name;
        string coreName = "";
        if (fullName.Contains("_")) {
            coreName = fullName.Split('_')[1].Split(' ', '(')[0];
        }
        if(CustomSols.instance.isToastDialogue.Value)
            ToastManager.Toast(coreName);
        if (string.IsNullOrEmpty(coreName)) return;

        // 直接從 AssetLoader 拿預載好的 4K 大圖
        // 假設你的檔案叫 Portrait_GouMang_master.png，那 key 就是 "Portrait_GouMang_master"
        Texture2D atlas = AssetLoader.GetAtlas($"Portrait_{coreName}");

        if (atlas != null) {
            ApplyAtlasToRoot(__instance.gameObject, atlas, false);
        }
    }

    // 通用應用方法
    private static void ApplyAtlasToRoot(GameObject root, Texture2D atlas, bool resetUV) {
        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        foreach (var r in renderers) {
            r.GetPropertyBlock(mpb);
            mpb.SetTexture("_MainTex", atlas);
            mpb.SetTexture("_Texture", atlas);
            if (resetUV) mpb.SetVector("_MainTex_ST", new Vector4(1, 1, 0, 0));
            r.SetPropertyBlock(mpb);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DialogueAnswerManager), "DisplayAnswers")]
    private static void DialogueAnswerManager_DisplayAnswers_Patch(DialogueAnswerManager __instance) {
        try {
            // 1. 定位 Yi 的頭像路徑
            string yiPath = "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/Always Canvas/DialoguePlayer(KeepThisEnable)/UIControlGroup_AnswerPanels/AnswerPanel(Manga)/PlayerFrame/Portrait_Yi";
            GameObject yiObj = GameObject.Find(yiPath);

            if (yiObj == null) {
                // 如果沒找到，可能是因為 UI 結構變動或尚未生成
                return;
            }

            // 2. 從 AssetLoader 獲取已經預載好的 Yi 大圖
            // 注意：這裡的 Key 要對應你放在 Atlas 資料夾下的檔名 (不含副檔名)
            Texture2D atlas = AssetLoader.GetAtlas("Portrait_Yee");

            if (atlas != null) {
                // 3. 使用通用方法替換，resetUV 設為 true (UI 需要校正)
                ApplyAtlasToRoot(yiObj, atlas, true);
                // ToastManager.Toast("Yi 的頭像已透過 Atlas 快取替換");
            } else {
                // 如果快取沒抓到，可以檢查檔名或路徑
                // UnityEngine.Debug.LogWarning("[Mod] 找不到 Portrait_Yee_master 快取");
            }
        } catch (Exception ex) {
            UnityEngine.Debug.LogError("Yi 替換出錯: " + ex.Message);
        }
    }
    // 紀錄每個實例 ID 對應的高清貼圖，避免重複解析名字
    private static Dictionary<int, Texture2D> instanceAtlasMapping = new Dictionary<int, Texture2D>();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PhoneCharacter), "Update")]
    private static void PhoneCharacter_Update_Patch(PhoneCharacter __instance) {
        int id = __instance.GetInstanceID();

        // 1. 嘗試從快取拿已經解析好的貼圖
        if (!instanceAtlasMapping.TryGetValue(id, out Texture2D atlas)) {
            // 2. 如果快取沒有，才執行解析邏輯 (這部分每個實例只會跑一次)
            string fullName = __instance.name;
            string coreName = "";
            if (fullName.Contains("_")) {
                coreName = fullName.Split('_')[1].Split(' ', '(')[0];
            }

            if (!string.IsNullOrEmpty(coreName)) {
                // 從 AssetLoader 的全局快取拿貼圖
                if (!AssetLoader.cacheAtlasTextures.TryGetValue(coreName, out atlas)) {
                    atlas = AssetLoader.GetAtlas($"Portrait_{coreName}");
                    AssetLoader.cacheAtlasTextures[coreName] = atlas;
                }
            }

            // 將解析結果存入實例快取 (即使是 null 也存，避免解析失敗的物件每幀都在重複解析)
            instanceAtlasMapping[id] = atlas;
        }

        // 3. 只要有貼圖，每一幀都執行套用 (防止 Animator 換回原圖)
        if (atlas != null) {
            ApplyAtlasToRoot(__instance.gameObject, atlas, false);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PhoneCharacter), "EnterLevelReset")]
    private static void PhoneCharacter_EnterLevelReset_Patch(PhoneCharacter __instance) {
        if (CustomSols.instance.isToastDialogue.Value) {
            string fullName = __instance.name;
            string coreName = "";
            if (fullName.Contains("_")) {
                coreName = fullName.Split('_')[1].Split(' ', '(')[0];
            }
            if (!string.IsNullOrEmpty(coreName)) {
                ToastManager.Toast(coreName);
            }
        }
    }
}