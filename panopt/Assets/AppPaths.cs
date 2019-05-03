using System.IO;
using UnityEngine;
using Hagring;

public class AppPaths : MonoBehaviour
{
     // TODO: use proper version
    public const string MODEL_ASSET_VERSION = "2017.4.4f1";

    public static string ModelAssetsDir { get; private set; }
    public static string PersistedDataFile { get; private set; }

    void Awake()
    {
        var DataDirsRoot = Application.persistentDataPath;

        ModelAssetsDir = Path.Combine(DataDirsRoot, "assets", MODEL_ASSET_VERSION);
        PersistedDataFile = Path.Combine(DataDirsRoot, "panopt.json");
    }

    static string GetDataDirsRoot()
    {

        /*
         * the presistent data path is using / as dir seprator,
         * massage it to use windows \ dir separator
         *
         * we need to use windows \ separator, otherwise we run into
         * problems when comparing with paths returned by
         * System.IO.DirectoryInfo.GetFiles()
         */
        return Application.persistentDataPath.Replace('/', '\\');
    }
}
