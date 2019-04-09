using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EnhancedUI.EnhancedScroller;
using System.IO;

public class SpeechToTextController : MonoBehaviour, IEnhancedScrollerDelegate
{
    public Text textOutput;
    public GameObject xrImageContainer;
    public EnhancedScroller historyScroller;
    public TransCellView transCellViewPrefab;

    private const int TXT_OUTPUT_LIMIT = 7;

    private List<string> _sttTokens;
    private List<string> _sttHistory;

    private string _sttHistoryFilePath;
    private string _sttHistoryFilename;

    void Start()
    {
        _sttHistoryFilename = System.Guid.NewGuid().ToString() + ".trans.txt";
        _sttHistoryFilePath = Path.Combine(Application.persistentDataPath, _sttHistoryFilename);

        _sttTokens = new List<string>();
        _sttHistory = new List<string>();

        historyScroller.Delegate = this;
        historyScroller.ReloadData();
        ToggleTransHistoryPane();
    }

    /// <summary>
    /// Save a new transcript
    /// </summary>
    /// <param name="text">Text.</param>
    public void SaveTranscript(string[] text)
    {
        foreach (string ts in text)
        {
            if (ts.Length > 0)
            {
                SaveToFile(ts + "\n");
                _sttHistory.Add(ts);
                foreach (string t in ts.Split(' '))
                {
                    _sttTokens.Add(t);
                }
            }
        }
        historyScroller.ReloadData();
    }

    private void SaveToFile(string transcript)
    {
        File.AppendAllText(_sttHistoryFilePath, transcript);
    }

    /// <summary>
    /// The general update call
    /// </summary>
    public void UpdateVis()
    {
        if (textOutput != null)
        {
            UpdateTxtOutput();
        }
        if (xrImageContainer != null)
        {
            UpdateXrVis();
        }
    }

    /// <summary>
    /// Update the visualization in XR
    /// </summary>
    public void UpdateXrVis()
    {
        // Clear the container
        foreach (Transform child in xrImageContainer.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = _sttTokens.Count - 1, cnt = 0; i >= 0 && cnt <= TXT_OUTPUT_LIMIT; i--, cnt++)
        {
            var xrTxt = Instantiate(Resources.Load<GameObject>("XrTxtModel"));
            var textMesh = xrTxt.GetComponentInChildren<TextMesh>();
            textMesh.text = _sttTokens[i];

            // Random position
            xrTxt.transform.parent = xrImageContainer.transform;
            xrTxt.transform.localEulerAngles = new Vector3(90f, 0, 0);
            xrTxt.transform.localScale = new Vector3(5f, 1f, 0.1f);

            int span = 5;
            float posX = Random.Range(-span, span);
            float posY = Random.Range(-span, span);
            xrTxt.transform.localPosition = new Vector3(posX, 2.0f, posY);
        }
    }

    /// <summary>
    /// Update the transcription on the screen
    /// </summary>
    public void UpdateTxtOutput()
    {
        string output = "";
        output = _sttHistory[_sttHistory.Count - 1];
        textOutput.text = output;
    }


    /// <summary>
    /// Gets the number of cells for transcript history scroller.
    /// </summary>
    /// <returns>The number of cells.</returns>
    /// <param name="scroller">Scroller.</param>
    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return _sttHistory.Count;
    }

    /// <summary>
    /// Gets the size of the cell view for transcript history scroller.
    /// </summary>
    /// <returns>The cell view size.</returns>
    /// <param name="scroller">Scroller.</param>
    /// <param name="dataIndex">Data index.</param>
    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return 50f;
    }

    /// <summary>
    /// Gets the cell view for transcript history scroller.
    /// </summary>
    /// <returns>The cell view.</returns>
    /// <param name="scroller">Scroller.</param>
    /// <param name="dataIndex">Data index.</param>
    /// <param name="cellIndex">Cell index.</param>
    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        TransCellView cellView = scroller.GetCellView(transCellViewPrefab) as TransCellView;
        cellView.SetData(_sttHistory[dataIndex]);
        return cellView;
    }

    /// <summary>
    /// Show/Hide the transcript history scroller.
    /// </summary>
    public void ToggleTransHistoryPane()
    {
        historyScroller.gameObject.SetActive(!historyScroller.gameObject.activeSelf);
    }
}
