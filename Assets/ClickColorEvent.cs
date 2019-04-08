using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickColorEvent : MonoBehaviour, IPointerClickHandler
{

    public Image image;
    public Color clickedColor;
    private Color originalColor;
    private bool isClicked;

    void Start()
    {
        originalColor = image.color;
        isClicked = false;
    }

    public void OnPointerClick(PointerEventData eventData)
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
