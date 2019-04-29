using UnityEngine;
using UnityEngine.UI;

public class ErrorMessage : MonoBehaviour
{
    public delegate void Closed();
    public static event Closed ClosedEvent;

    public Text Title;
    public Text Message;

    public void Show(string title, string message)
    {
        Title.text = title;
        Message.text = message;

        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        ClosedEvent?.Invoke();
    }
}
