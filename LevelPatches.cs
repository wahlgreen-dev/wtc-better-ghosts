using HarmonyLib;
using Il2CppSpeed.Level;

namespace BetterGhosts;

[HarmonyPatch(typeof(LevelGameplayState), nameof(LevelGameplayState.OnEnterState))]
static class GameplayStartedPatch
{
    static void Postfix()
    {
        BetterGhostsMod.Instance.OnGameplayStarted();
    }
}

[HarmonyPatch(typeof(LevelInstance), nameof(LevelInstance.AddWonResult))]
static class LevelWonPatch
{
    static void Postfix()
    {
        BetterGhostsMod.Instance.OnLevelWon();
    }
}
