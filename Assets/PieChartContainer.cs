using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PieChartContainer : MonoBehaviour
{
    private RectTransform pieTemplate;
    private string[] colors;

    private void Start()
    {
        pieTemplate = transform.Find("PieChartTemplate").GetComponent<RectTransform>();

        colors = StatChartController.COLOR_PALETTE;

        // Testing the pie chart function
        // RenderPieChart(new float[] { 0.6f, 0.4f });
    }

    public void RenderPieChart(float[] values)
    {
        float sum = 0f;
        foreach (float v in values) sum += v;
        if (sum != 1) throw new System.Exception("The sum of values is not 1.0");

        float rotationAngle = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            // Copy the geom from the template
            RectTransform newSector = Instantiate(pieTemplate);
            newSector.name = "PieSector";
            newSector.SetParent(transform);
            newSector.anchorMin = pieTemplate.anchorMin;
            newSector.anchorMax = pieTemplate.anchorMax;
            newSector.anchoredPosition = pieTemplate.anchoredPosition;
            newSector.sizeDelta = pieTemplate.sizeDelta;
            newSector.localScale = pieTemplate.localScale;
            newSector.gameObject.SetActive(true);
            // Get the Image component and fill an amount
            Image sectorImage = newSector.GetComponent<Image>();
            sectorImage.fillAmount = values[i];
            // Set a new color
            Color newColor;
            ColorUtility.TryParseHtmlString(colors[i % colors.Length], out newColor);
            sectorImage.color = newColor;
            // Rotate the sector according to its portion
            newSector.Rotate(new Vector3(0, 0, rotationAngle), Space.Self);
            rotationAngle += values[i] * 360f;
        }
    }

    public void ClearPieChart()
    {
        foreach (Transform child in transform)
        {
            if (child.name == "PieSector")
            {
                GameObject.Destroy(child.gameObject);
            }
        }
    }
}
