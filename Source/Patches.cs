﻿using HarmonyLib;

namespace CustomSols;

[HarmonyPatch]
public class Patches {
    //[HarmonyPatch(typeof(Player), nameof(Player.SetStoryWalk))]
    //[HarmonyPrefix]
    //private static bool PatchStoryWalk(ref float walkModifier) {
    //    walkModifier = 1.0f;

    //    return true; // the original method should be executed
    //}
}