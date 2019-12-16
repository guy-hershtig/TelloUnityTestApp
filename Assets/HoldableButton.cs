using UnityEngine;
using UnityEngine.EventSystems;

public class HoldableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool buttonPressed { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        buttonPressed = enabled;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        buttonPressed = false;
    }
}
