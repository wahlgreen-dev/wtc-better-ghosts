using HarmonyLib;
using Il2Cpp;
using Il2CppSpeed.UI;
using Il2CppTriband.Localization;
using UnityEngine;
using UnityEngine.Events;

namespace BetterGhosts;

[HarmonyPatch(typeof(SettingsWindow), nameof(SettingsWindow.AddGeneralTab))]
internal static class GeneralSettingsTabPatch
{
    private const string SwitchLabel = "Better Ghosts";

    private static void Postfix(SettingsWindow __instance, TabPanel __result)
    {
        SettingsSwitch gameGhostSwitch = FindSwitch(__result, Loc.GHOSTS_PLAYER_LEVELS);
        if (gameGhostSwitch == null)
            return;

        Transform gameGhostRow = gameGhostSwitch.transform;
        SettingsSwitch betterGhostSwitch = __instance.AddSwitch(gameGhostRow.parent, SwitchLabel, (Il2CppSystem.Func<bool>)IsShowingBetterGhost, (UnityAction<bool>)SetShowingBetterGhost);
        betterGhostSwitch.transform.SetSiblingIndex(gameGhostRow.GetSiblingIndex() + 1);

        TextLocalizer label = betterGhostSwitch._textLabel;
        label.disableTranslation = true;
        label.RefreshText();
    }

    private static SettingsSwitch FindSwitch(TabPanel tab, string term)
    {
        var switches = tab.GetComponentsInChildren<SettingsSwitch>(true);
        for (int i = 0; i < switches.Length; i++)
        {
            if (switches[i]._textLabel.termId == term)
                return switches[i];
        }
        return null;
    }

    private static bool IsShowingBetterGhost()
    {
        return BetterGhostsMod.Instance.ShowBetterGhost;
    }

    private static void SetShowingBetterGhost(bool show)
    {
        BetterGhostsMod.Instance.ShowBetterGhost = show;
    }
}
