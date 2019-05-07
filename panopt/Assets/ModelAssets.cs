using System.IO;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Hagring;

class AssetLoader
{
    string AssetFile;
    GameObject Asset;

    AutoResetEvent AssetLoaded = new AutoResetEvent(false);

    public AssetLoader(string AssetFile)
    {
        this.AssetFile = AssetFile;
    }

    void LoadAsset()
    {
        var assBundle = AssetBundle.LoadFromFile(AssetFile);
        Asset = assBundle.LoadAsset<GameObject>("ifc");
        assBundle.Unload(false);

        /* signal waiting thread that load is complete */
        AssetLoaded.Set();
    }

    public GameObject Load()
    {
        /* shedule asset loading on main thread */
        MainThreadRunner.Run(LoadAsset);

        /* block until it's done */
        AssetLoaded.WaitOne();
        return Asset;
    }
}

public class ModelAssets
{
    public delegate void StartedDownloading(int AssetsCount);
    public static event StartedDownloading StartedDownloadingEvent;

    public delegate void AssetDownloaded(int Downloaded, int AssetsCount);
    public static event AssetDownloaded AssetDownloadedEvent;

    public delegate void FinishedDownloading();
    public static event FinishedDownloading FinishedDownloadingEvent;

    public delegate void AbortedDownloading();
    public static event AbortedDownloading AbortedDownloadingEvent;

    static bool StopDownloading;

    public static void Download(CloudAPI.Model[] models)
    {
        StopDownloading = false;
        Utils.StartThread(() =>
            DownloadAssets(AppPaths.ModelAssetsDir, models), "DownloadAssets");
    }

    public static void AbortDownloading()
    {
        StopDownloading = true;
    }

    static List<CloudAPI.Model> GetDownloadableModels(CloudAPI.Model[] models)
    {
        var done = new List<CloudAPI.Model>();

        foreach (var model in models)
        {
            if (!model.importStatus.Equals("done"))
            {
                /* import in progress or import failed, skip this model */
                continue;
            }

            if (AssetAlreadyDownloaded(model.model))
            {
                continue;
            }

            done.Add(model);
        }

        return done;
    }

    static bool AssetAlreadyDownloaded(string AssetID)
    {
        return File.Exists(GetAssetFileName(AssetID));
    }

    static void DownloadAssets(string AssetsDirectory, CloudAPI.Model[] models)
    {
        var toDownloadModels = GetDownloadableModels(models);

        var AssetsCount = toDownloadModels.Count;
        StartedDownloadingEvent?.Invoke(AssetsCount);

        DeleteOrphanedAssets(AssetsDirectory, models);

        int cntr = 1;
        foreach (var model in toDownloadModels)
        {
            if (StopDownloading)
            {
                /* a request is made to abort downloading */
                AbortedDownloadingEvent?.Invoke();
                return;
            }

            var assetFile = GetAssetFileName(model.model);
            DownloadAsset(AssetsDirectory, model.model);
            AssetDownloadedEvent?.Invoke(cntr++, AssetsCount);
            System.Threading.Thread.Sleep(1299);
        }

        FinishedDownloadingEvent?.Invoke();
    }

    static string GetAssetFileName(string modelID)
    {
        return Path.Combine(AppPaths.ModelAssetsDir, "Asset_" + modelID);
    }

    public static GameObject LoadAsset(string assetID)
    {
        var loader = new AssetLoader(GetAssetFileName(assetID));
        return loader.Load();
    }

    ///
    /// Delete all files from models assets directory that don't
    /// belong to any of the provided models.
    ///
    /// Use to get rid of asset files that are no longer used.
    ///
    static public void DeleteOrphanedAssets(string AssetsDirectory, CloudAPI.Model[] models)
    {
        if (!Directory.Exists(AppPaths.ModelAssetsDir))
        {
            /*
             * handle the case when model assets directory have not
             * been created yet, for example if no models
             * have been downloaded yet
             *
             * no assets dir, nothing to clean-up
             */
            return;
        }

        /* create a set of asset files that should not be removed */
        var assetsInUse = new HashSet<string>();
        foreach (var model in models)
        {
            var fname = GetAssetFileName(model.model);
            assetsInUse.Add(fname);
        }

        /* delete assets without models */
        var RootAssetsDir = new DirectoryInfo(AppPaths.ModelAssetsDir).Parent;
        foreach (var file in RootAssetsDir.GetFiles("*", SearchOption.AllDirectories))
        {
            var assetFilePath = file.ToString();
            if (!assetsInUse.Contains(assetFilePath))
            {
                Log.Msg("Deleting orphaned asset file {0}", assetFilePath);
                file.Delete();
            }
        }

        /* delete empty directories, to tidy up */
        Utils.DeleteEmptySubDirs(RootAssetsDir);
    }

    static void DownloadAsset(string assetsDirectory, string assetId)
    {
        try
        {
            var destFile = GetAssetFileName(assetId);
            var partFile = destFile + ".part";

            Log.Msg("downloading asset {0} to {1}", assetId, destFile);
            CloudAPI.Instance.DownloadModel(assetId, AppPaths.MODEL_ASSET_VERSION, partFile);
            File.Move(partFile, destFile);
        }
        catch (ModelDownloadError e)
        {
            /*
             * TODO: implement proper error handling,
             * we should probably retry downloading this model
             */
            Log.Err("Failed to download asset {0}: {1}", assetId, e);
        }
    }

}
