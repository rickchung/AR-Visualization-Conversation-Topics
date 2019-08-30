using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class OnPointerDownEvent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UnityEvent onPointerDownEvent;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (onPointerDownEvent != null)
        {
            onPointerDownEvent.Invoke();
        }

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (onPointerDownEvent != null)
        {
            onPointerDownEvent.Invoke();
        }
    }
}
