// LineChartContainer.cs
// Reference: https://www.youtube.com/watch?v=CmU5-v-v1Qo&list=PLzDRvYVwl53v5ur4GluoabyckImZz3TVQ&index=3&t=75s

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LineChartContainer : MonoBehaviour
{
    public Sprite dotImage;
    private RectTransform chartContainer;
    private Vector2 dotSize = new Vector2(12, 12);
    private float xSize = 22f;
    private float yLimMax = 100f;
    private float chartHeight;

    private void Awake()
    {
        chartContainer = GetComponent<RectTransform>();
        chartHeight = chartContainer.sizeDelta.y;
        Debug.Log(chartHeight);

        List<float> data = new List<float> { 1, 21, 51, 61, 71, 91, 61, 31, };
        RenderData(data);
    }

    private void RenderData(List<float> data)
    {
        for (int i = 0; i < data.Count; i++)
        {
            Vector2 position = new Vector2(
                xSize + i * xSize,
                (data[i] / yLimMax) * chartHeight
            );

            RenderDot(position);
        }
    }

    private void RenderDot(Vector2 anchoredPosition)
    {
        GameObject newDot = new GameObject("dot", typeof(Image));
        newDot.transform.SetParent(chartContainer);
        newDot.GetComponent<Image>().sprite = dotImage;
        RectTransform dotRectTransform = newDot.GetComponent<RectTransform>();
        dotRectTransform.anchoredPosition = anchoredPosition;
        dotRectTransform.sizeDelta = dotSize;
        dotRectTransform.anchorMin = new Vector2(0, 0);
        dotRectTransform.anchorMax = new Vector2(0, 0);
    }
}
