using System.IO;
using Il2CppSpeed.GhostCar;
using Il2CppSpeed.Level;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace BetterGhosts;

public class LevelSession
{
    readonly MelonLogger.Instance logger;

    GhostCarManager manager;
    LevelTimer levelTimer;
    bool levelWon;
    VehicleSetup setup;
    Transform vehicle;
    Transform body;
    Transform[] parts;
    GhostRecorder recorder;
    ShapedGhost ghost;
    bool ghostGaveUp;

    public LevelSession(MelonLogger.Instance logger)
    {
        this.logger = logger;
    }

    static string RecordingFolder => Path.Combine(MelonEnvironment.UserDataDirectory, "BetterGhosts", "ghosts");

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
        if (recorder == null)
            return;

        if (manager == null)
        {
            EndRun();
            return;
        }

        if (!levelWon)
            recorder.Update();

        if (ghost == null && !ghostGaveUp)
            TryCreateGhost();

        ghost?.Update(levelTimer != null ? levelTimer.time : 0f);
    }

    public void OnGhostSaved(GhostCarInfo info, GhostCarData data)
    {
        if (recorder == null || manager == null || info == null)
            return;

        var levelId = info.levelId;
        var instanceId = info.instanceId;
        if (levelId != manager.levelId || string.IsNullOrEmpty(instanceId))
            return;

        recorder.Update();
        var gameSampleCount = data != null && data.ghostData != null ? data.ghostData.Count : -1;
        if (recorder.Recording.SampleCount != gameSampleCount)
        {
            logger.Warning($"Not saving ghost parts: {recorder.Recording.SampleCount} samples, the game has {gameSampleCount}");
            return;
        }

        try
        {
            recorder.Recording.Save(Path.Combine(RecordingFolder, instanceId + ".bgh"));
        }
        catch (System.Exception e)
        {
            logger.Error($"Couldn't save ghost parts: {e}");
        }
    }

    void StartRun()
    {
        levelTimer = manager.levelTimer != null ? manager.levelTimer : Object.FindObjectOfType<LevelTimer>();

        var playerVehicle = manager.car != null ? manager.car : Object.FindObjectOfType<ThisIsThePlayerVehicle>();
        if (playerVehicle == null || playerVehicle._mainRigidbody == null)
            return;

        var vehicleName = playerVehicle.gameObject.name;
        var foundSetup = VehicleSetups.Find(vehicleName);
        if (foundSetup == null)
            return;

        vehicle = playerVehicle.transform;
        body = playerVehicle._mainRigidbody.transform;
        parts = new Transform[foundSetup.PartPaths.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = TransformPaths.Find(vehicle, foundSetup.PartPaths[i]);
            if (parts[i] == null)
            {
                logger.Warning($"{vehicleName} has no {foundSetup.PartPaths[i]}");
                return;
            }
        }

        setup = foundSetup;
        recorder = new GhostRecorder(manager, body, parts, setup);
    }

    void TryCreateGhost()
    {
        if (manager.ghostTransform == null || manager.existingGhostData == null)
            return;

        ghostGaveUp = true;
        try
        {
            ghost = new ShapedGhost(manager, vehicle, body, parts, LoadRecordingForExistingGhost());
        }
        catch (System.Exception e)
        {
            logger.Error($"Couldn't build ghost: {e}");
        }
    }

    PartRecording LoadRecordingForExistingGhost()
    {
        var info = manager.existingGhostInfo;
        var instanceId = info != null ? info.instanceId : null;
        if (string.IsNullOrEmpty(instanceId))
            return null;

        var path = Path.Combine(RecordingFolder, instanceId + ".bgh");
        if (!File.Exists(path))
            return null;

        PartRecording recording;
        try
        {
            recording = PartRecording.Load(path);
        }
        catch (System.Exception e)
        {
            logger.Warning($"Couldn't read {Path.GetFileName(path)}: {e.Message}");
            return null;
        }

        if (recording.VehicleName != setup.VehicleName || !SamePaths(recording.PartPaths, setup.PartPaths))
            return null;
        return recording;
    }

    void EndRun()
    {
        ghost?.Destroy();
        ghost = null;
        ghostGaveUp = false;
        recorder = null;
        setup = null;
        parts = null;
        manager = null;
        levelWon = false;
    }

    static bool SamePaths(string[] a, string[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }
}
