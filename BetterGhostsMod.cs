using System;
using Il2CppSpeed.GhostCar;
using MelonLoader;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(BetterGhosts.BetterGhostsMod), "Better Ghosts", "0.1.0", "wahlgreen-dev")]
[assembly: MelonGame("Triband", "WHATTHECAR")]

namespace BetterGhosts;

public class BetterGhostsMod : MelonMod
{
    private MelonPreferences_Category preferences;
    private MelonPreferences_Entry<bool> showBetterGhost;
    private MelonPreferences_Entry<string> toggleKey;

    public static BetterGhostsMod Instance { get; private set; }

    public LevelSession Session { get; private set; }

    public override void OnInitializeMelon()
    {
        Instance = this;
        preferences = MelonPreferences.CreateCategory("BetterGhosts");
        showBetterGhost = preferences.CreateEntry("ShowBetterGhost", true, description: "Show the ghost with the vehicle's shape instead of the game's own ghost");
        toggleKey = preferences.CreateEntry("ToggleKey", "F2", description: "Key that switches between the Better Ghosts ghost and the game's own ghost, from the next run");
        Session = new LevelSession(LoggerInstance);
        Session.ShowBetterGhost = showBetterGhost.Value;
    }

    public void OnLevelIntro()
    {
        Session.OnLevelIntro(Object.FindObjectOfType<GhostCarManager>());
    }

    public void OnGameplayStarted()
    {
        Session.OnGameplayStarted(Object.FindObjectOfType<GhostCarManager>());
    }

    public void OnLevelWon()
    {
        Session.OnLevelWon();
    }

    public override void OnUpdate()
    {
        if (!WasPressed(toggleKey.Value))
            return;

        showBetterGhost.Value = !showBetterGhost.Value;
        preferences.SaveToFile(false);
        Session.ShowBetterGhost = showBetterGhost.Value;
        LoggerInstance.Msg(showBetterGhost.Value ? "Better Ghosts ghost from the next run" : "Game's own ghost from the next run");
    }

    public override void OnLateUpdate()
    {
        Session.Update();
    }

    private static bool WasPressed(string keyName)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && Enum.TryParse(keyName, true, out Key key) && keyboard[key].wasPressedThisFrame;
    }
}
