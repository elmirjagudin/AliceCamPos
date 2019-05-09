using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Hagring;

public class SceneModel
{
    public delegate void PositionUpdated(GPSPosition NewPosition);
    public event PositionUpdated PositionUpdatedEvent;

    /* position or visibilty changed */
    public event Action UpdatedEvent;

    public string CloudID { get; private set; }
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
                Prefab.SetActive(!Hidden);
                _SceneObject = GameObject.Instantiate(Prefab);
            }

            return _SceneObject;
        }
    }

    bool _Hidden;
    public bool Hidden
    {
        set
        {
            _Hidden = value;
            if (_SceneObject != null)
            {
                SceneObject.SetActive(!_Hidden);
            }

            UpdatedEvent?.Invoke();
        }

        get { return _Hidden; }
    }

    public SceneModel(string ModelID, string Name, GameObject Prefab,
                      GPSPosition Position, bool Hidden = false)
    {
        this.CloudID = ModelID;
        this.Name = Name;
        this.Prefab = Prefab;
        this.Position = Position;
        this.Hidden = Hidden;
    }

    public void UpdatePosition(string Projection,
                               double North, double East, double Altitude)
    {
        Position = new GPSPosition(Projection, North, East, Altitude);
        PositionUpdatedEvent?.Invoke(Position);
        UpdatedEvent?.Invoke();
    }
}

///
/// Persists changes of model's metadata (position and visibility) per
/// video.
///
/// Acts as a proxy. If model is not mentioned in the local
/// json file, use the model's metadata from the cloud, overwise
/// use the local changes.
///
class ModelInstances
{
    const string JSON_FILE = "models.json";

    public List<CloudAPI.ModelInstance> models;

    string JsonFile;

    static string JsonFilePath(string VideoFile)
    {
        return Path.Combine(AppPaths.GetVideoDataDir(VideoFile), JSON_FILE);
    }

    public static ModelInstances Load(string VideoFile)
    {
        var file = JsonFilePath(VideoFile);
        if (!File.Exists(file))
        {
            /* no model instances saved yet */
            return new ModelInstances
            {
                JsonFile = file,
                models = new List<CloudAPI.ModelInstance>()
            };
        }

        var jsonText = File.ReadAllText(file);
        var mis = JsonConvert.DeserializeObject<ModelInstances>(jsonText);
        mis.JsonFile = file;

        return mis;
    }

    void Save()
    {
        var JsonText = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(JsonFile, JsonText);
    }

    CloudAPI.ModelInstance Find(string modelID)
    {
        foreach (var mod in models)
        {
            if (mod.modelId.Equals(modelID))
            {
                return mod;
            }
        }

        return null;
    }

    CloudAPI.ModelInstance Get(string modelID)
    {
        var model = Find(modelID);

        if (model == null)
        {
            /* not found, make a new one */
            model = new CloudAPI.ModelInstance();
            model.modelId = modelID;
            models.Add(model);
        }

        return model;
    }

    void ModelUpdated(SceneModel model)
    {
        var mi = Get(model.CloudID);
        mi.hidden = model.Hidden;
        mi.position = new CloudAPI.Position();
        mi.position.GpsPos = model.Position;

        Save();
    }

    public SceneModel GetModel(string ModelID, string Name,
                               GameObject Prefab, GPSPosition Position)
    {
        var modInst = Find(ModelID);

        var sceneMod = modInst == null ?
            /* instance for this video not found */
            new SceneModel(ModelID, Name, Prefab, Position) :
            /* instance found, override position */
            new SceneModel(ModelID, Name, Prefab, modInst.position.GpsPos, modInst.hidden);

        /* listen for this model's changes, so we can write 'em down */
        sceneMod.UpdatedEvent += () => ModelUpdated(sceneMod);

        return sceneMod;
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

    public List<SceneModel> SceneModels {private set; get;}

    public void Init(CloudAPI.Model[] models)
    {
        this.CloudModels = models;
        ModelAssets.Download(models);
    }

    public void Load(string VideoFile)
    {
        Utils.StartThread(() => LoadModels(VideoFile), "LoadModels");
    }

    void LoadModels(string VideoFile)
    {
        StartedLoadingPrefabsEvent?.Invoke();

        int total = CloudModels.Length;
        int cntr = 0;

        SceneModels = new List<SceneModel>();
        var modelInstances = ModelInstances.Load(VideoFile);

        foreach (var cloudModel in CloudModels)
        {
            cntr += 1;
            var model = CloudAPI.Instance.GetModel(cloudModel.model);
            if (model.defaultPosition == null)
            {
                Log.Wrn($"no position for {cloudModel}, ignoring");
                continue;
            }

            var pos = model.defaultPosition.GpsPos;
            var prefab = ModelAssets.LoadAsset(cloudModel.model);

            /* set trackable name to aid in debugging  */
            MainThreadRunner.Run(() => prefab.name = cloudModel.ToString());

            var sceneModel = modelInstances.GetModel(cloudModel.model, cloudModel.name, prefab, pos);
            SceneModels.Add(sceneModel);

            PrefabLoadedEvent?.Invoke(cntr, total);
        }

        FinishedLoadingPrefabsEvent?.Invoke();
    }
}
