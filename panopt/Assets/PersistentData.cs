using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Hagring;

class PanoptJson
{
    const string FILE = "panopt.json";

    /*
     * empty strings means directory is not known
     */
    public string LastSourceDirectory = "";
    public string LastRecordingFile = "";

    public string Username = null;
    public string Password = null;

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

    public void Set(string PropertyName, string Vaue)
    {
        GetType().GetField(PropertyName).SetValue(this, Vaue);
        /* make sure we write new value to the disk */
        using (var fileWriter = Utils.CreateFileWriter(GetFilePath()))
        {
            var jsonText = JsonConvert.SerializeObject(this, Formatting.Indented);
            fileWriter.Write(jsonText);
        }
    }
}

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
        get { return json.LastSourceDirectory; }
        set { json.Set("LastSourceDirectory", value); }
    }

    /* the filename we lastly used to record to */
    public static string LastRecordingFile
    {
        get { return json.LastRecordingFile; }
        set { json.Set("LastRecordingFile", value); }
    }

    public static string Username
    {
        get { return json.Username; }
        set { json.Set("Username", value); }
    }

    public static string Password
    {
        get { return json.Password; }
        set { json.Set("Password", value); }
    }

}
