using HarmonyLib;
using Il2CppSpeed.GhostCar;

namespace BetterGhosts;

[HarmonyPatch(typeof(GhostCarFileManager), nameof(GhostCarFileManager.SaveGhostRunLocally))]
static class SaveGhostRunLocallyPatch
{
    static void Postfix(GhostCarInfo info, GhostCarData data)
    {
        BetterGhostsMod.Instance.Session.OnGhostSaved(info, data);
    }
}
