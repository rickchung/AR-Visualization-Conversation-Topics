using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatChartController : MonoBehaviour
{
    public static string[] COLOR_PALETTE = { "#24A0FF", "#FF7600", "#CC4505" };

    public Transform panChartNumPhases;
    public Transform panChartNumWords;
    private TMPro.TextMeshProUGUI valTxtNumPhases, valTxtNumWords;
    private LineChartContainer chartSpokenWords;
    private PieChartContainer chartNumPhases;

    private float numOfPhases, numOfRemotePhases, numOfSpokenWords, numOfRemoteSpokenWords;
    private Queue<float> bufferNumSpokenWords, bufferRemoteNumSpokenWords;
    private int bufferSize = 12;

    private Color localColor, remoteColor;

    void Start()
    {
        // Get the placeholders to show values
        valTxtNumPhases = panChartNumPhases.Find("PhText").GetComponent<TMPro.TextMeshProUGUI>();
        valTxtNumWords = panChartNumWords.Find("PhText").GetComponent<TMPro.TextMeshProUGUI>();
        chartSpokenWords = panChartNumWords.GetComponentInChildren<LineChartContainer>();
        chartNumPhases = panChartNumPhases.GetComponentInChildren<PieChartContainer>();

        bufferNumSpokenWords = new Queue<float>(bufferSize);
        bufferRemoteNumSpokenWords = new Queue<float>(bufferSize);

        ColorUtility.TryParseHtmlString(COLOR_PALETTE[0], out localColor);
        localColor.a = 0.5f;
        ColorUtility.TryParseHtmlString(COLOR_PALETTE[1], out remoteColor);
        remoteColor.a = 0.5f;
    }

    public void UpdateStat(int numPhases, int numWords, bool isLocal)
    {
        if (isLocal)
        {
            numOfPhases += numPhases;
            numOfSpokenWords += numWords;
            if (bufferNumSpokenWords.Count >= bufferSize)
                bufferNumSpokenWords.Dequeue();
            bufferNumSpokenWords.Enqueue(numWords);
        }
        else
        {
            numOfRemotePhases += numPhases;
            numOfRemoteSpokenWords += numWords;
            if (bufferRemoteNumSpokenWords.Count >= bufferSize)
                bufferRemoteNumSpokenWords.Dequeue();
            bufferRemoteNumSpokenWords.Enqueue(numWords);
        }
    }

    public void UpdateNumPhaseChart(bool useSocialVis = false)
    {
        valTxtNumPhases.text = numOfPhases + " ps";

        if (useSocialVis)
        {
            chartNumPhases.ClearPieChart();
            float localRatio = numOfPhases / (numOfPhases + numOfRemotePhases);
            float remoteRatio = numOfRemotePhases / (numOfPhases + numOfRemotePhases);
            chartNumPhases.RenderPieChart(new float[] { localRatio, remoteRatio });
        }
    }

    public void UpdateNumWordChart(bool useSocialVis = false)
    {
        try
        {
            float localAvg = numOfSpokenWords / numOfPhases;
            valTxtNumWords.text = localAvg.ToString("0.0") + " ws/ps";
            // Render charts
            chartSpokenWords.ClearChart();
            chartSpokenWords.RenderYAxisLine(localAvg, localColor);
            chartSpokenWords.RenderValues(
                bufferNumSpokenWords.ToArray(), localColor, chartType: "bar"
            );

            if (useSocialVis)
            {
                float remoteAvg = numOfRemoteSpokenWords / numOfRemotePhases;
                chartSpokenWords.RenderYAxisLine(remoteAvg, remoteColor);
                chartSpokenWords.RenderValues(
                    bufferRemoteNumSpokenWords.ToArray(), remoteColor, chartType: "bar"
                );
            }
        }
        catch (System.DivideByZeroException) { }
    }
}
