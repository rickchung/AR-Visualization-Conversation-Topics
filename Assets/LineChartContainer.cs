// LineChartContainer.cs
// This class is only responsible for rendering charts.
//
// Reference: https://www.youtube.com/watch?v=CmU5-v-v1Qo&list=PLzDRvYVwl53v5ur4GluoabyckImZz3TVQ&index=3

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LineChartContainer : MonoBehaviour
{
    public Sprite dotImage;
    private RectTransform chartContainer;
    private RectTransform yGuidelineTemplate;
    private RectTransform currentAvgLine;
    private float chartHeight, chartWidth, yPadding, yLimUpper, xPadding;

    private void Start()
    {
        // Get the size of this chart container
        chartContainer = GetComponent<RectTransform>();
        chartHeight = chartContainer.sizeDelta.y;
        chartWidth = chartContainer.sizeDelta.x;
        yPadding = chartHeight * 0.10f;
        xPadding = chartWidth * 0.10f;
        yLimUpper = 10f;

        // Get chart components
        yGuidelineTemplate = transform.Find("yGuidelineTemplate").GetComponent<RectTransform>();

        // Testing values
        // List<float> data = new List<float> { 1, 21, 51, 61, 71, 91, 61, 31, };
        // RenderValues(data, chartType: "bar", visibleNumValues: 5);
        // RenderAvgLine(50);
    }

    public void RenderValues(float[] values, string chartType = "dot", int visibleNumValues = -1)
    {
        // Only show "visibleNumValues" values in the list
        if (visibleNumValues < 0)
            visibleNumValues = values.Length;

        // Adjust the interval between data points on the x-axis
        float xNormalizedInterval = (chartWidth - xPadding) / visibleNumValues;

        for (int i = values.Length - visibleNumValues, j = 0; i < values.Length; i++, j++)
        {
            Vector2 position = new Vector2(
                xPadding + j * xNormalizedInterval,
                yPadding + (values[i] / yLimUpper) * chartHeight
            );

            if (chartType == "dot")
                RenderOneDot(position);
            else if (chartType == "bar")
                RenderOneBar(position);
            else
                Debug.LogError("Unsupported chartType: " + chartType);
        }
    }

    public void RenderAvgLine(float avgValue)
    {
        if (currentAvgLine == null)
        {
            currentAvgLine = Instantiate(yGuidelineTemplate);
            currentAvgLine.SetParent(transform, false);
            currentAvgLine.pivot = Vector2.zero;
            currentAvgLine.anchorMin = Vector2.zero;
            currentAvgLine.anchorMax = Vector2.zero;
            currentAvgLine.gameObject.SetActive(true);
        }

        RenderYAxisLine(avgValue, currentAvgLine);
    }

    public void RenderYAxisLine(float yValue, RectTransform targetLine = null)
    {
        if (targetLine == null)
        {
            targetLine = Instantiate(yGuidelineTemplate);
            targetLine.SetParent(transform, false);
            targetLine.pivot = Vector2.zero;
            targetLine.anchorMin = Vector2.zero;
            targetLine.anchorMax = Vector2.zero;
            targetLine.gameObject.SetActive(true);
        }
        float yNormalizedPosition = (yValue / yLimUpper) * chartHeight;
        targetLine.anchoredPosition = new Vector2(0f, yNormalizedPosition);
    }

    private GameObject RenderOneDot(Vector2 anchoredPosition)
    {
        // Instantiate a new GameObject as a dot
        GameObject newDot = new GameObject("DataPoint", typeof(Image));
        // Move the new dot into the chart container
        newDot.transform.SetParent(chartContainer);
        // Add an image to the new dot
        newDot.GetComponent<Image>().sprite = dotImage;

        RectTransform dotRectTransform = newDot.GetComponent<RectTransform>();
        dotRectTransform.pivot = Vector2.zero;
        // Set the reference anchor
        dotRectTransform.anchorMin = new Vector2(0, 0);
        dotRectTransform.anchorMax = new Vector2(0, 0);
        // Move and resize the dot
        dotRectTransform.anchoredPosition = anchoredPosition;
        dotRectTransform.sizeDelta = new Vector2(12, 12);

        return newDot;
    }

    private void RenderOneBar(Vector2 anchoredPosition, float barWidth = -1)
    {
        GameObject bar = RenderOneDot(anchoredPosition);
        RectTransform barRectTransform = bar.GetComponent<RectTransform>();

        if (barWidth < 0)
            barWidth = 48f;

        Vector2 barSize = new Vector2(barWidth, anchoredPosition.y);
        Vector2 barNewPosition = new Vector2(
            barRectTransform.anchoredPosition.x,
            barRectTransform.anchoredPosition.y - barSize.y
        );
        barRectTransform.sizeDelta = barSize;
        barRectTransform.anchoredPosition = barNewPosition;
    }

    public void ClearChart()
    {
        foreach (Transform child in transform)
            if (child.name == "DataPoint")
                GameObject.Destroy(child.gameObject);
    }
}
