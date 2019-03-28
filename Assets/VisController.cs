using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VisController : MonoBehaviour
{
    public Text textOutput;
    public GameObject xrVisContainer;

    private const int TXT_OUTPUT_LIMIT = 7;

    private List<string> speechToTexts;

    void Start()
    {
        speechToTexts = new List<string>();
    }

    public void AddNewText(string[] text)
    {
        foreach (string ts in text)
        {
            foreach (string t in ts.Split(' '))
            {
                speechToTexts.Add(t);
            }
        }
    }

    public void UpdateVis()
    {
        if (textOutput != null)
        {
            UpdateTxtOutput();
        }
        if (xrVisContainer != null)
        {
            UpdateXrVis();
        }
    }

    public void UpdateXrVis()
    {
        // Clear the container
        foreach (Transform child in xrVisContainer.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = speechToTexts.Count - 1, cnt = 0; i >= 0 && cnt <= TXT_OUTPUT_LIMIT; i--, cnt++)
        {
            var xrTxt = Instantiate(Resources.Load<GameObject>("XrTxtModel"));
            var textMesh = xrTxt.GetComponentInChildren<TextMesh>();
            textMesh.text = speechToTexts[i];

            // Random position
            xrTxt.transform.parent = xrVisContainer.transform;
            xrTxt.transform.localEulerAngles = new Vector3(90f, 0, 0);
            xrTxt.transform.localScale = new Vector3(5f, 1f, 0.1f);

            int span = 5;
            float posX = Random.Range(-span, span);
            float posY = Random.Range(-span, span);
            xrTxt.transform.localPosition = new Vector3(posX, 2.0f, posY);
        }
    }

    public void UpdateTxtOutput()
    {
        string output = "";


        int tail = speechToTexts.Count;
        if (tail < TXT_OUTPUT_LIMIT)
        {
            foreach (string s in speechToTexts)
            {
                output += s + " ";
            }
        }
        else
        {
            for (int i = tail - TXT_OUTPUT_LIMIT; i < tail; i++)
            {
                output += speechToTexts[i] + " ";
            }
        }

        textOutput.text = output;
    }
}
