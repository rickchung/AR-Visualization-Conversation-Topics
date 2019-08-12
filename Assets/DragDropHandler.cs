using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragDropHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public CardRemoveTrigger removingArea;

    private Vector2? prevPointerPosition;
    private Vector2 xBound, yBound;
    public void OnBeginDrag(PointerEventData eventData)
    {
        xBound = new Vector2(-450, 450);
        yBound = new Vector2(-100, 200);
    }

    public void OnDrag(PointerEventData eventData)
    {
        GameObject hitGameObject = eventData.pointerPressRaycast.gameObject;
        if (hitGameObject != null && prevPointerPosition != null)
        {
            Vector2 currentPointerPosition = eventData.pointerCurrentRaycast.screenPosition;
            Vector2 diffPos = currentPointerPosition - (Vector2)prevPointerPosition;
            Vector3 diffPosVec3 = new Vector3(diffPos.x, diffPos.y, 0);
            Vector3 newPosition = transform.localPosition + diffPosVec3;
            if (newPosition.x >= xBound[0] && newPosition.x <= xBound[1] &&
                newPosition.y >= yBound[0] && newPosition.y <= yBound[1])
            {
                transform.Translate(diffPos, Space.Self);
            }
        }
        prevPointerPosition = eventData.pointerCurrentRaycast.screenPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        prevPointerPosition = null;
        if (removingArea.isTriggered)
        {
            removingArea.TriggerExitFunc();
            Destroy(gameObject);
        }
    }
}
