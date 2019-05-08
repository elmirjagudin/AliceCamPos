using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hagring;

public class SceneModel
{
    public delegate void PositionUpdated(GPSPosition NewPosition);
    public event PositionUpdated PositionUpdatedEvent;

    public string Name { get; private set; }
    public GameObject Prefab { get; private set; }
    public GPSPosition Position { get; private set; }

    GameObject _SceneObject = null;
    public GameObject SceneObject
    {
        /*
         * instantiate Prefab lazily
         */
        get
        {
            if (_SceneObject == null)
            {
                _SceneObject = GameObject.Instantiate(Prefab);
            }

            return _SceneObject;
        }
    }

    public bool Hidden { set { SceneObject.SetActive(!value);} }

    public SceneModel(string Name, GameObject Prefab, GPSPosition Position)
    {
        this.Name = Name;
        this.Prefab = Prefab;
        this.Position = Position;
    }

    public void UpdatePosition(string Projection,
                               double North, double East, double Altitude)
    {
        Position = new GPSPosition(Projection, North, East, Altitude);
        PositionUpdatedEvent?.Invoke(Position);
    }
}

public class Models : MonoBehaviour
{
    public delegate void StartedLoadingPrefabs();
    public event StartedLoadingPrefabs StartedLoadingPrefabsEvent;

    public delegate void PrefabLoaded(int done, int total);
    public event PrefabLoaded PrefabLoadedEvent;

    public delegate void FinishedLoadingPrefabs();
    public event FinishedLoadingPrefabs FinishedLoadingPrefabsEvent;

    CloudAPI.Model[] CloudModels;

    public List<SceneModel> SceneModels { get; private set;} =
        new List<SceneModel>();

    public void Init(CloudAPI.Model[] models)
    {
        this.CloudModels = models;
        ModelAssets.Download(models);
    }

    public void Load()
    {
        Utils.StartThread(LoadModels, "LoadModels");
    }

    void LoadModels()
    {
        StartedLoadingPrefabsEvent?.Invoke();

        int total = CloudModels.Length;
        int cntr = 0;

        foreach (var m in CloudModels)
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

            SceneModels.Add(new SceneModel(m.name, prefab, pos));

            PrefabLoadedEvent?.Invoke(cntr, total);
        }

        FinishedLoadingPrefabsEvent?.Invoke();
    }
}
