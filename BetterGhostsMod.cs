using Il2CppSpeed.GhostCar;
using MelonLoader;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(BetterGhosts.BetterGhostsMod), "Better Ghosts", "0.1.0", "wahlgreen-dev")]
[assembly: MelonGame("Triband", "WHATTHECAR")]

namespace BetterGhosts;

public class BetterGhostsMod : MelonMod
{
    private MelonPreferences_Category preferences;
    private MelonPreferences_Entry<bool> showBetterGhost;

    public static BetterGhostsMod Instance { get; private set; }

    public LevelSession Session { get; private set; }

    public bool ShowBetterGhost
    {
        get => showBetterGhost.Value;
        set
        {
            showBetterGhost.Value = value;
            preferences.SaveToFile(false);
            Session.ShowBetterGhost = value;
        }
    }

    public override void OnInitializeMelon()
    {
        Instance = this;
        preferences = MelonPreferences.CreateCategory("BetterGhosts");
        showBetterGhost = preferences.CreateEntry("ShowBetterGhost", true, description: "Show the ghost with the vehicle's shape instead of the game's own ghost");
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

    public override void OnLateUpdate()
    {
        Session.Update();
    }
}
