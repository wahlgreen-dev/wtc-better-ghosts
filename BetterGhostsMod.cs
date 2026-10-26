using Il2CppSpeed.GhostCar;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(BetterGhosts.BetterGhostsMod), "Better Ghosts", "0.1.0", "wahlgreen-dev")]
[assembly: MelonGame("Triband", "WHATTHECAR")]

namespace BetterGhosts;

public class BetterGhostsMod : MelonMod
{
    public static BetterGhostsMod Instance { get; private set; }

    public LevelSession Session { get; private set; }

    public override void OnInitializeMelon()
    {
        Instance = this;
        Session = new LevelSession(LoggerInstance);
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

    public override void OnLateUpdate()
    {
        Session.Update();
    }
}
