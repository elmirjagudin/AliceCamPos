using UnityEngine;

public class Menu : MonoBehaviour
{
    public void ToggleOnScreen()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
