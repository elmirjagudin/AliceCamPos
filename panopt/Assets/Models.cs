using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hagring;

public class Models : MonoBehaviour
{
    public delegate void StartedLoadingPrefabs();
    public event StartedLoadingPrefabs StartedLoadingPrefabsEvent;

    public delegate void PrefabLoaded(int done, int total);
    public event PrefabLoaded PrefabLoadedEvent;

    public delegate void FinishedLoadingPrefabs();
    public event FinishedLoadingPrefabs FinishedLoadingPrefabsEvent;

    CloudAPI.Model[] allModels;

    public List<(GPSPosition pos, GameObject prefab)>
        Prefabs { get; private set;} =
            new List<(GPSPosition pos, GameObject prefab)>();

    public void Init(CloudAPI.Model[] models)
    {
        this.allModels = models;
        ModelAssets.Download(models);
    }

    public void Load()
    {
        Utils.StartThread(LoadModels, "LoadModels");
    }

    void LoadModels()
    {
        StartedLoadingPrefabsEvent?.Invoke();

        int total = allModels.Length;
        int cntr = 0;

        foreach (var m in allModels)
        {
            cntr += 1;
            var model = CloudAPI.Instance.GetModel(m.model);
            if (model.defaultPosition == null)
            {
                Log.Wrn($"no position for {m}, ignoring");
                continue;
            }

            var pos = model.defaultPosition.GpsPos;
            var prefab = ModelAssets.LoadAsset(m.model);
            MainThreadRunner.Run(() => prefab.name = m.ToString());
            Prefabs.Add((pos, prefab));

            PrefabLoadedEvent?.Invoke(cntr, total);
        }

        FinishedLoadingPrefabsEvent?.Invoke();
    }
}
