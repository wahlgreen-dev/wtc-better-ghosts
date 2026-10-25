using Il2CppSpeed.GhostCar;

namespace BetterGhosts;

public class LevelSession
{
    GhostCarManager manager;
    bool levelWon;

    public void OnGameplayStarted(GhostCarManager manager)
    {
        EndRun();
        this.manager = manager;
    }

    public void OnLevelWon()
    {
        levelWon = true;
    }

    public void Update()
    {
        if (manager == null || levelWon)
            return;
    }

    void EndRun()
    {
        manager = null;
        levelWon = false;
    }
}
