using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Brab;

public class ModelMenu : MonoBehaviour
{
    static string[,] Projections = new string[,]
    {
        {"SWEREF 99 TM", "sweref_99_tm" },
        {"SWEREF 99 12 00", "sweref_99_12_00" },
        {"SWEREF 99 13 30", "sweref_99_13_30" },
        {"SWEREF 99 15 00", "sweref_99_15_00" },
        {"SWEREF 99 16 30", "sweref_99_16_30" },
        {"SWEREF 99 18 00", "sweref_99_18_00" },
        {"SWEREF 99 14 15", "sweref_99_14_15" },
        {"SWEREF 99 15 45", "sweref_99_15_45" },
        {"SWEREF 99 17 15", "sweref_99_17_15" },
        {"SWEREF 99 18 45", "sweref_99_18_45" },
        {"SWEREF 99 20 15", "sweref_99_20_15" },
        {"SWEREF 99 21 45", "sweref_99_21_45" },
        {"SWEREF 99 23 15", "sweref_99_23_15" },
// broken, TODO, possibly fix if panopt big in UK!
//        { "OSGB36", "osgb36" },
    };

    public Text Name;

    public Dropdown Projection;
    public Text NorthLabel;
    public Text EastLabel;
    public Text UpLabel;

    public InputField North;
    public InputField East;
    public InputField Up;

    SceneModel CurrentModel;

    void Awake()
    {
        /*
         * Populate Projections drop down
         */
        var ProjOptions = new List<Dropdown.OptionData>();

        for (int i = 0; i < Projections.GetLength(0); i += 1)
        {
            ProjOptions.Add(new Dropdown.OptionData(Projections[i, 0]));
        }

        Projection.options = ProjOptions;
        ProjectionChanged(Projection.value);
    }

    public void ProjectionChanged(int i)
    {
        var axis = GeodesyProjections.getAxisNames(Projections[i, 1]);
        NorthLabel.text = axis.North;
        EastLabel.text = axis.East;
        UpLabel.text = axis.Up;
    }

    void SetProjection(int idx)
    {
        Projection.value = idx;
        ProjectionChanged(idx);
    }

    void SetProjection(string proj)
    {
        for (int i = 0; i < Projections.GetLength(0); i += 1)
        {
            if (Projections[i, 1].Equals(proj))
            {
                SetProjection(i);
                return;
            }
        }
        Log.Wrn($"unexpected projection '{proj}'");
    }

    string PosValText(double posVal)
    {
        return string.Format("{0:0.0}", posVal);
    }

    public bool Visible()
    {
        return gameObject.activeSelf;
    }

    public void Show(SceneModel model)
    {
        gameObject.SetActive(true);
        Name.text = model.Name;
        var pos = model.Position;

        SetProjection(pos.Projection);
        North.text = PosValText(pos.North);
        East.text = PosValText(pos.East);
        Up.text = PosValText(pos.Altitude);

        CurrentModel = model;
        ListenForChanges();
    }

    void ListenForChanges()
    {
        North.onValueChanged.AddListener((_) => PositionChanged());
        East.onValueChanged.AddListener((_) => PositionChanged());
        Up.onValueChanged.AddListener((_) => PositionChanged());
        Projection.onValueChanged.AddListener((_) => PositionChanged());
    }

    void StopListeningForChanges()
    {
        Projection.onValueChanged.RemoveAllListeners();
        Up.onValueChanged.RemoveAllListeners();
        East.onValueChanged.RemoveAllListeners();
        North.onValueChanged.RemoveAllListeners();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        StopListeningForChanges();
    }

    object ParseInputField(InputField field)
    {
        field.image.color = Color.white;
        try
        {
            return Parse.Double(field.text);
        }
        catch (FormatException)
        {
            field.image.color = Color.red;
        }

        return null;
    }

    void PositionChanged()
    {
        var proj = Projections[Projection.value, 1];
        var north = ParseInputField(North);
        var east = ParseInputField(East);
        var up = ParseInputField(Up);

        if (north == null ||  east == null || up == null)
        {
            /* some values are unparsable, bail! */
            return;
        }

        CurrentModel.UpdatePosition(
            proj, (double)north, (double)east, (double)up);
    }
}
