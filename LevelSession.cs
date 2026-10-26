using System.IO;
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
    private Transform[] parts;
    private GhostRecorder recorder;
    private ShapedGhost ghost;
    private bool ghostAttempted;

    public LevelSession(MelonLogger.Instance logger)
    {
        this.logger = logger;
    }

    private static string RecordingFolder => Path.Combine(MelonEnvironment.UserDataDirectory, "BetterGhosts", "ghosts");

    public void OnGameplayStarted(GhostCarManager manager)
    {
        EndRun();
        if (manager == null)
            return;

        this.manager = manager;
        StartRun();
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

        if (!levelWon)
            recorder?.Update();

        if (ghost == null && !ghostAttempted)
            TryCreateGhost();

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

        recorder.Recording.Save(Path.Combine(RecordingFolder, info.instanceId + ".bgh"));
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
        body = playerVehicle._mainRigidbody.transform;
        parts = new Transform[foundSetup.PartPaths.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = TransformPaths.Find(vehicle, foundSetup.PartPaths[i]);
            if (parts[i] != null)
                continue;

            logger.Warning($"{vehicle.name} has no {foundSetup.PartPaths[i]}");
            return;
        }

        setup = foundSetup;
        if (parts.Length > 0)
            recorder = new GhostRecorder(manager, body, parts, setup);
    }

    private void TryCreateGhost()
    {
        if (manager.ghostTransform == null || manager.existingGhostData == null)
            return;

        ghostAttempted = true;
        PartRecording recording = parts.Length > 0 ? LoadRecording() : null;
        ghost = new ShapedGhost(manager, vehicle, body, parts, setup, recording);
    }

    private PartRecording LoadRecording()
    {
        string path = Path.Combine(RecordingFolder, manager.existingGhostInfo.instanceId + ".bgh");
        if (!File.Exists(path))
            return null;

        PartRecording recording = PartRecording.Load(path);
        if (recording == null || !recording.Matches(setup))
            return null;

        return recording;
    }

    private void EndRun()
    {
        ghost?.Destroy();
        ghost = null;
        ghostAttempted = false;
        recorder = null;
        setup = null;
        parts = null;
        manager = null;
        levelWon = false;
    }
}
