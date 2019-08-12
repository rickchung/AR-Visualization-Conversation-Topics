using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardRemoveTrigger : MonoBehaviour
{
    private Image image;

    [HideInInspector] public bool isTriggered;

    void Start()
    {
        image = GetComponent<Image>();
    }
    void OnTriggerEnter(Collider other)
    {
        image.color = Color.red;
        isTriggered = true;
    }

    void OnTriggerExit(Collider other)
    {
        TriggerExitFunc();
    }

    public void TriggerExitFunc()
    {
        image.color = Color.white;
        isTriggered = false;
    }
}
