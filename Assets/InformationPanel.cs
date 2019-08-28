using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InformationPanel : MonoBehaviour
{
    public GameObject infoPanel;
    public TextMeshProUGUI textContent;

    public void ShowInfoPanel(bool show)
    {
        infoPanel.SetActive(show);
    }

    public void ReplaceContent(string content)
    {
        textContent.text = content;
    }
}
