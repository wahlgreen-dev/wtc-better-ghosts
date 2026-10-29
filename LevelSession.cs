using System.IO;
using System.Linq;
using Il2CppSpeed.GhostCar;
using Il2CppSpeed.Level;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace BetterGhosts;

public class LevelSession
{
    private readonly MelonLogger.Instance logger;

    private GhostCarManager manager;
    private LevelTimer levelTimer;
    private bool levelWon;
    private VehicleSetup setup;
    private Transform vehicle;
    private Transform body;
    private Transform frame;
    private Transform[] parts;
    private Transform[] goalObjects;
    private GhostRecorder recorder;
    private ShapedGhost ghost;
    private GhostExit exit;
    private bool ghostAttempted;
    private bool warmedUp;
    private bool gameplayStarted;

    public LevelSession(MelonLogger.Instance logger)
    {
        this.logger = logger;
    }

    private static string RecordingFolder => Path.Combine(MelonEnvironment.UserDataDirectory, "BetterGhosts", "ghosts");

    public void OnLevelIntro(GhostCarManager manager)
    {
        EndRun();
        if (manager == null)
            return;

        this.manager = manager;
        StartRun();
    }

    public void OnGameplayStarted(GhostCarManager manager)
    {
        if (manager == null || manager != this.manager || setup == null)
            OnLevelIntro(manager);
        if (setup == null)
            return;

        exit?.Restore();
        exit = null;
        gameplayStarted = true;
        levelWon = false;
        if (RecordsAnything)
            recorder = new GhostRecorder(manager, body, parts, goalObjects, setup);
    }

    public void OnLevelWon()
    {
        levelWon = true;
    }

    public void Update()
    {
        if (setup == null)
            return;

        if (manager == null)
        {
            EndRun();
            return;
        }

        if (!gameplayStarted)
        {
            if (!warmedUp)
                WarmUp();
            return;
        }

        if (!levelWon)
            recorder?.Update();

        if (ghost == null && !ghostAttempted)
            TryCreateGhost();

        if (ghost != null && exit == null)
            exit = new GhostExit(manager);
        exit?.Update(levelTimer.time, levelWon);

        ghost?.Update(levelTimer.time, Time.deltaTime);
    }

    public void OnGhostSaved(GhostCarInfo info, GhostCarData data)
    {
        if (recorder == null)
            return;

        recorder.Update();
        int gameSampleCount = data.ghostData.Count;
        if (recorder.Recording.SampleCount != gameSampleCount)
        {
            logger.Warning($"Not saving ghost parts: {recorder.Recording.SampleCount} samples, the game has {gameSampleCount}");
            return;
        }

        RecordingFile.Save(recorder.Recording, Path.Combine(RecordingFolder, info.instanceId + ".bgh"));
    }

    private void StartRun()
    {
        levelTimer = manager.levelTimer != null ? manager.levelTimer : Object.FindObjectOfType<LevelTimer>();

        ThisIsThePlayerVehicle playerVehicle = manager.car != null ? manager.car : Object.FindObjectOfType<ThisIsThePlayerVehicle>();
        if (playerVehicle == null)
            return;

        VehicleSetup foundSetup = VehicleSetups.Find(playerVehicle.transform);
        if (foundSetup == null)
            return;

        vehicle = playerVehicle.transform;
        frame = foundSetup.HasFrame ? TransformPaths.Find(vehicle, foundSetup.FramePath) : null;
        body = frame != null ? frame : playerVehicle._mainRigidbody.transform;
        parts = new Transform[foundSetup.RecordedPartPaths.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = TransformPaths.Find(vehicle, foundSetup.RecordedPartPaths[i]);
            if (parts[i] != null)
                continue;

            logger.Warning($"{vehicle.name} has no {foundSetup.RecordedPartPaths[i]}");
            return;
        }

        goalObjects = GoalObjects.Find(vehicle);
        setup = foundSetup;
    }

    private bool RecordsAnything => parts.Length > 0 || goalObjects.Length > 0;

    private bool GhostDataReady => manager.ghostTransform != null && manager.existingGhostData != null;

    private bool PartsActive => frame == null || body.gameObject.activeInHierarchy && parts.All(part => part.gameObject.activeInHierarchy);

    private void WarmUp()
    {
        if (!GhostDataReady)
            return;

        warmedUp = true;
        ShapedGhost warmUpGhost = new ShapedGhost(manager, vehicle, body, parts, goalObjects, setup, null);
        warmUpGhost.Update(0f, 0f);
        warmUpGhost.Destroy();
    }

    private void TryCreateGhost()
    {
        if (!GhostDataReady || !PartsActive)
            return;

        ghostAttempted = true;
        PartRecording recording = RecordsAnything ? LoadRecording() : null;
        ghost = new ShapedGhost(manager, vehicle, body, parts, goalObjects, setup, recording);
    }

    private PartRecording LoadRecording()
    {
        string path = Path.Combine(RecordingFolder, manager.existingGhostInfo.instanceId + ".bgh");
        if (!File.Exists(path))
            return null;

        PartRecording recording = RecordingFile.Load(path);
        if (recording == null || !recording.Matches(setup) || recording.SampleCount != manager.existingGhostData.ghostData.Count)
            return null;

        return recording;
    }

    private void EndRun()
    {
        exit?.Restore();
        exit = null;
        ghost?.Destroy();
        ghost = null;
        ghostAttempted = false;
        warmedUp = false;
        gameplayStarted = false;
        recorder = null;
        setup = null;
        parts = null;
        goalObjects = null;
        manager = null;
        levelWon = false;
    }
}
