using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Hagring;

#pragma warning disable 0649
class PanoptJson
{
    const string FILE = "panopt.json";

    /* empty string mean no 'last used directory' known */
    public string LastUsedDirectory = "";

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

    public static string LastUsedDirectory
    {
        get
        {
            return json.LastUsedDirectory;
        }
        set
        {
            json.LastUsedDirectory = value;
            json.Save();
        }
    }
}
