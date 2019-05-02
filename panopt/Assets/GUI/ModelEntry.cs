using UnityEngine;
using UnityEngine.UI;

public class ModelEntry : MonoBehaviour
{
    public Text Label;

    public void SetName(string name)
    {
        Label.text = name;
    }
}
