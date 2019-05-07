using UnityEngine;
using UnityEngine.UI;

public class ModelsMenu : Menu
{
    public Models Models;
    public RectTransform Content;
    public ModelEntry ModelEntry;

    void Start()
    {
        foreach (var model in Models.SceneModels)
        {
            var entry = Instantiate(ModelEntry);
            entry.SetName(model.Name);
            entry.transform.SetParent(Content);

            var toggle = entry.GetComponentInChildren<Toggle>();
            toggle.onValueChanged.AddListener(delegate (bool on)
            {
                model.Hidden = !on;
            });
        }
    }
}