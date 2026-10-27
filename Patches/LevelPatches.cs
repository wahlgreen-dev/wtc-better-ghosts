using HarmonyLib;
using Il2CppSpeed.Level;

namespace BetterGhosts;

[HarmonyPatch(typeof(LevelIntroState), nameof(LevelIntroState.OnEnterState))]
internal static class LevelIntroPatch
{
    private static void Postfix()
    {
        BetterGhostsMod.Instance.OnLevelIntro();
    }
}

[HarmonyPatch(typeof(LevelGameplayState), nameof(LevelGameplayState.OnEnterState))]
internal static class GameplayStartedPatch
{
    private static void Postfix()
    {
        BetterGhostsMod.Instance.OnGameplayStarted();
    }
}

[HarmonyPatch(typeof(LevelInstance), nameof(LevelInstance.AddWonResult))]
internal static class LevelWonPatch
{
    private static void Postfix()
    {
        BetterGhostsMod.Instance.OnLevelWon();
    }
}
