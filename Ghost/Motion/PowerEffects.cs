using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class PowerEffects : IGhostMotion
{
    private const float ReferenceFlameLength = 0.57f;

    private readonly Transform flameTemplate;
    private readonly List<GameObject> flames = new();
    private readonly List<Transform> keptRoots = new();
    private bool active;

    public PowerEffects(Transform flameTemplate)
    {
        this.flameTemplate = flameTemplate;
    }

    public bool Keeps(Transform transform)
    {
        foreach (Transform keptRoot in keptRoots)
        {
            if (transform.IsChildOf(keptRoot))
                return true;
        }
        return false;
    }

    public void AddFromCopy(GameObject copy, GameObject source)
    {
        var copyFires = copy.GetComponentsInChildren<Fire>(true);
        var sourceFires = source.GetComponentsInChildren<Fire>(true);
        for (int i = 0; i < copyFires.Length; i++)
        {
            Transform copyFire = copyFires[i].transform;
            if (sourceFires[i].gameObject.activeInHierarchy)
            {
                Transform holder = new GameObject(copyFire.name).transform;
                holder.SetParent(copyFire.parent, false);
                holder.localPosition = copyFire.localPosition;
                holder.localRotation = copyFire.localRotation;
                holder.localScale = copyFire.localScale;
                keptRoots.Add(holder);
                AddFlame(holder, sourceFires[i]);
            }
            Object.DestroyImmediate(copyFire.gameObject);
        }
    }

    public void AddDetachedFire(Fire source, GhostPivots pivots)
    {
        Transform holder = new GameObject(source.name).transform;
        holder.gameObject.layer = pivots.Layer;
        pivots.Place(holder, source.transform);
        AddFlame(holder, source);
    }

    private void AddFlame(Transform holder, Fire source)
    {
        Transform flame = Object.Instantiate(flameTemplate, holder);
        flame.name = flameTemplate.name;
        flame.localPosition = Vector3.zero;
        flame.localRotation = flameTemplate.localRotation;
        flame.localScale = Divide(flameTemplate.lossyScale * (FullFlameLength(source) / ReferenceFlameLength), source.transform.lossyScale);
        flame.gameObject.SetActive(source.startOn);
        if (!source.startOn)
            flames.Add(flame.gameObject);
    }

    private static float FullFlameLength(Fire fire)
    {
        float length = fire.transform.lossyScale.y * fire.containerScale.y;
        for (Transform t = fire.flames.transform; t != fire.flamesContainer && t != fire.transform; t = t.parent)
            length *= t.localScale.y;
        return length;
    }

    private static Vector3 Divide(Vector3 scale, Vector3 by)
    {
        return new Vector3(scale.x / by.x, scale.y / by.y, scale.z / by.z);
    }

    public void Update(GhostFrame frame)
    {
        SetActive(frame.ActionPressed);
    }

    private void SetActive(bool active)
    {
        if (active == this.active)
            return;

        this.active = active;
        foreach (GameObject flame in flames)
            flame.SetActive(active);
    }
}
