using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatChartController : MonoBehaviour
{
    public Transform panChartNumPhases;
    public Transform panChartNumWords;

    private TMPro.TextMeshProUGUI valTxtNumPhases, valTxtNumWords;

    void Start()
    {
        // Get the placeholders to show values
        valTxtNumPhases = panChartNumPhases.Find("PhText").GetComponent<TMPro.TextMeshProUGUI>();
        valTxtNumWords = panChartNumWords.Find("PhText").GetComponent<TMPro.TextMeshProUGUI>();
    }

    public void UpdateNumPhaseChart(int localNum, int remoteNum = -1)
    {
        valTxtNumPhases.text = localNum + " ps";
    }

    public void UpdateNumWordChart(int localNumWords, int localNumPhases, int remoteNumWords = -1, int remoteNumPhases = -1)
    {
        try
        {
            float localAvg = localNumWords / localNumPhases;
            valTxtNumWords.text = localAvg.ToString("0.0") + " ws/ps";
        }
        catch (System.DivideByZeroException) { }
    }
}
