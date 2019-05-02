using UnityEngine;
using UnityEngine.UI;

public class ModelsMenu : Menu
{
    public RectTransform Content;
    public ModelEntry ModelEntry;

    void Start()
    {
        /*
         * temporary code, just for testing UI elements
         */
        var models = CloudAPI.Instance.GetModels();
        foreach (var m in models)
        {
            var entry = Instantiate(ModelEntry);
            entry.SetName(m.name);
            entry.transform.SetParent(Content);
        }
    }
}
