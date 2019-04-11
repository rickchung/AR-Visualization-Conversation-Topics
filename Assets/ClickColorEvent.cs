using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickColorEvent : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{

    public Image image;
    public Color clickedColor;
    public bool useClick;
    public bool usePointerUpDown;

    private Color originalColor;
    private bool isClicked;

    void Start()
    {
        originalColor = image.color;
        isClicked = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (useClick)
        {
            if (isClicked)
            {
                isClicked = false;
                image.color = originalColor;
            }
            else
            {
                isClicked = true;
                image.color = clickedColor;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (usePointerUpDown)
            image.color = clickedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (usePointerUpDown)
            image.color = originalColor;
    }
}
