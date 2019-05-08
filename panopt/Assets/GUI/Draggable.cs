using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IDragHandler
{
    public void OnDrag(PointerEventData e)
    {
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.offsetMax += e.delta;
        rectTransform.offsetMin += e.delta;
    }
}
