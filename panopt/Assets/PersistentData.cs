using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Hagring;

#pragma warning disable 0649
class PanoptJson
{
    const string FILE = "panopt.json";

    /*
     * empty strings means directory is not known
     */
    public string LastSourceDirectory = "";
    public string LastRecordingFile = "";

    static string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, FILE);
    }

    public static PanoptJson Load()
    {
        var filePath = GetFilePath();
        if (!File.Exists(filePath))
        {
            /* no persisted values found, use defaults */
            return new PanoptJson();
        }

        var jsonText = File.ReadAllText(filePath);

        return JsonConvert.DeserializeObject<PanoptJson>(jsonText);
    }

    public void Save()
    {
        using (var fileWriter = Utils.CreateFileWriter(GetFilePath()))
        {
            var jsonText = JsonConvert.SerializeObject(this, Formatting.Indented);
            fileWriter.Write(jsonText);
        }
    }
}
#pragma warning restore

public class Persisted
{
    static PanoptJson _json = null;
    static PanoptJson json
    {
        get
        {
            if (_json == null)
            {
                _json = PanoptJson.Load();
            }
            return _json;
        }
    }

    /* the directory where we last picked source video */
    public static string LastSourceDirectory
    {
        get
        {
            return json.LastSourceDirectory;
        }
        set
        {
            json.LastSourceDirectory = value;
            json.Save();
        }
    }

    /* the filename we lastly used to record to */
    public static string LastRecordingFile
    {
        get
        {
            return json.LastRecordingFile;
        }
        set
        {
            json.LastRecordingFile = value;
            json.Save();
        }
    }

}
