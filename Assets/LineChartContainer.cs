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

    private void Awake()
    {
        // Get the size of this chart container
        chartContainer = GetComponent<RectTransform>();
        chartHeight = chartContainer.sizeDelta.y;
        chartWidth = chartContainer.sizeDelta.x;
        yPadding = chartHeight * 0.10f;
        xPadding = chartWidth * 0.10f;
        yLimUpper = 100f;

        // Get chart components
        yGuidelineTemplate = transform.Find("yGuidelineTemplate").GetComponent<RectTransform>();

        // Testing values
        List<float> data = new List<float> { 1, 21, 51, 61, 71, 91, 61, 31, };
        RenderValues(data, chartType: "bar");
        RenderYAxisLine(50);
    }

    private void RenderValues(List<float> values, string chartType = "dot", int visibleNumValues = -1)
    {
        // Only show "visibleNumValues" values in the list
        if (visibleNumValues < 0)
            visibleNumValues = values.Count;

        // Adjust the interval between data points on the x-axis
        float xNormalizedInterval = (chartWidth - xPadding) / visibleNumValues;

        for (int i = values.Count - visibleNumValues; i < values.Count; i++)
        {
            Vector2 position = new Vector2(
                xPadding + i * xNormalizedInterval,
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

    private void RenderYAxisLine(float yValue)
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

        float yNormalizedPosition = (yValue / yLimUpper) * chartHeight;
        currentAvgLine.anchoredPosition = new Vector2(0f, yNormalizedPosition);
    }

    private GameObject RenderOneDot(Vector2 anchoredPosition)
    {
        // Instantiate a new GameObject as a dot
        GameObject newDot = new GameObject("dot", typeof(Image));
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

    private void RenderOneBar(Vector2 anchoredPosition)
    {
        GameObject bar = RenderOneDot(anchoredPosition);
        RectTransform barRectTransform = bar.GetComponent<RectTransform>();
        Vector2 barSize = new Vector2(24f, anchoredPosition.y);
        Vector2 barNewPosition = new Vector2(
            barRectTransform.anchoredPosition.x,
            barRectTransform.anchoredPosition.y - barSize.y
        );
        barRectTransform.sizeDelta = barSize;
        barRectTransform.anchoredPosition = barNewPosition;
    }
}
