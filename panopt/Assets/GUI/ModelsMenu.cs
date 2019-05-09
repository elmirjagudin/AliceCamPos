using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModelsMenu : Menu
{
    public Models Models;
    public ModelMenu ModelMenu;
    public RectTransform Content;
    public ModelEntry ModelEntry;


    ///
    /// Sort models by:
    /// 1) visibilty
    /// 2) alphabetically on the models name
    ///
    IEnumerable<SceneModel> SortedModels()
    {
        var visible = new List<SceneModel>();
        var hidden = new List<SceneModel>();

        /* split up models into list of visible and hidden */
        foreach (var model in Models.SceneModels)
        {
            var lst = model.Hidden ? hidden : visible;
            lst.Add(model);
        }

        /* return first visible model, sorted alphabetically */
        foreach (var model in visible.OrderBy(o=>o.Name))
        {
            yield return model;
        }

        /* then return hidden model, sorted alphabetically */
        foreach (var model in hidden.OrderBy(o=>o.Name))
        {
            yield return model;
        }
    }

    void Start()
    {
        foreach (var model in SortedModels())
        {
            var entry = Instantiate(ModelEntry);
            entry.SetName(model.Name);
            entry.transform.SetParent(Content);

            var toggle = entry.GetComponentInChildren<Toggle>();
            toggle.isOn = !model.Hidden;
            toggle.onValueChanged.AddListener(delegate (bool on)
            {
                model.Hidden = !on;
            });

            var label = entry.GetComponentInChildren<Clickable>();
            label.Clicked += delegate()
            {
                if (ModelMenu.Visible())
                {
                    /*
                     * model menu already displayed,
                     * do nothing
                     */
                    return;
                }
                ModelMenu.Show(model);
            };
        }
    }
}
