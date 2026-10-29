using Il2CppSpeed.GhostCar;

namespace BetterGhosts;

public class GhostExit
{
    private const float ShrinkTime = 0.5f;

    private readonly GhostCarManager manager;
    private readonly GhostCarTagFollower tag;
    private readonly float finishTime;
    private readonly bool scaleDownOnLevelWon;
    private bool finished;
    private bool tagHidden;

    public GhostExit(GhostCarManager manager)
    {
        this.manager = manager;
        tag = manager.ghostTransform.parent.GetComponentInChildren<GhostCarTagFollower>(true);
        finishTime = manager.existingGhostInfo.ghostWinTime;
        scaleDownOnLevelWon = manager.scaleDownOnLevelWon;
    }

    public void Update(float time, bool levelWon)
    {
        if (!finished && !levelWon && time >= finishTime)
            Finish();
        if (finished && time >= finishTime + ShrinkTime)
            HideTag();
    }

    public void Reset()
    {
        finished = false;
        if (manager != null)
            manager.scaleDownOnLevelWon = scaleDownOnLevelWon;
        if (!tagHidden || tag == null)
            return;

        tagHidden = false;
        tag.canvasWorldFollow.enabled = true;
        if (tag.canvasScreenBehind != null)
            tag.canvasScreenBehind.enabled = true;
    }

    private void Finish()
    {
        finished = true;
        if (!scaleDownOnLevelWon)
            return;

        manager.scaleDownOnLevelWon = false;
        float scale = manager.ghostTransform.localScale.x;
        manager.StartCoroutine(manager.ScaleGhostCo(scale, 0f, ShrinkTime, 0f, true));
        if (tag != null)
            tag.StartCoroutine(tag.ScaleTagCR(false, ShrinkTime));
    }

    private void HideTag()
    {
        if (!scaleDownOnLevelWon || tag == null)
            return;

        tag.canvasWorldFollow.enabled = false;
        tagHidden = true;
        if (tag.canvasScreenBehind != null)
            tag.canvasScreenBehind.enabled = false;
    }
}
