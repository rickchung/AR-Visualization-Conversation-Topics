using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatChartController : MonoBehaviour
{
    public Transform panChartNumPhases;
    public Transform panChartNumWords;

    private Transform nPhaseUpdateLoc, nWordUpdateLoc;


    void Start()
    {
        nPhaseUpdateLoc = panChartNumPhases.Find("PhText");
        nWordUpdateLoc = panChartNumWords.Find("PhText");
    }

    public void UpdateNumPhaseChart(int localNum, int remoteNum = -1)
    {
        nPhaseUpdateLoc.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = localNum + " ps";

    }

    public void UpdateNumWordChart(int localNumWords, int localNumPhases, int remoteNumWords = -1, int remoteNumPhases = -1)
    {
        try
        {
            float localAvg = localNumWords / localNumPhases;
            nWordUpdateLoc.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = localAvg.ToString("0.0") + " ws/ps";
        }
        catch (System.DivideByZeroException)
        {

        }
    }
}
