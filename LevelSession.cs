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
    bool levelWon;
    GhostRecorder recorder;

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
        var playerVehicle = manager.car != null ? manager.car : Object.FindObjectOfType<ThisIsThePlayerVehicle>();
        if (playerVehicle == null || playerVehicle._mainRigidbody == null)
            return;

        var vehicleName = playerVehicle.gameObject.name;
        var setup = VehicleSetups.Find(vehicleName);
        if (setup == null)
            return;

        var vehicle = playerVehicle.transform;
        var parts = new Transform[setup.PartPaths.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = TransformPaths.Find(vehicle, setup.PartPaths[i]);
            if (parts[i] == null)
            {
                logger.Warning($"{vehicleName} has no {setup.PartPaths[i]}");
                return;
            }
        }

        recorder = new GhostRecorder(manager, playerVehicle._mainRigidbody.transform, parts, setup);
    }

    void EndRun()
    {
        recorder = null;
        manager = null;
        levelWon = false;
    }
}
