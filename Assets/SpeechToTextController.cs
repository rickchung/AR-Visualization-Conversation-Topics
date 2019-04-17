using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EnhancedUI.EnhancedScroller;
using System.IO;

/// <summary>
/// Speech to text controller handles the transcription rendering on the screen,
/// for both 2D and AR GUI. The scrollers of transcripts are handled by 
/// EnhancedScroller attached to this object.
/// </summary>
public class SpeechToTextController : MonoBehaviour, IEnhancedScrollerDelegate
{
    public Text textOutput;
    public TMPro.TextMeshPro xrTextContainer;
    public EnhancedScroller historyScroller;
    public TransCellView transCellViewPrefab;
    public bool render3DTranscripts;

    private const int XR_TRANSCRIPTS_OUTPUT_LIMIT = 30;  // Chars
    private int mWordCount;

    private List<string> _sttHistory;

    private string _sttHistoryFilePath;
    private string _sttHistoryFilename;

    void Start()
    {
        _sttHistoryFilename = System.Guid.NewGuid().ToString() + ".trans.txt";
        _sttHistoryFilePath = Path.Combine(Application.persistentDataPath, _sttHistoryFilename);

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
                var timestamp = System.DateTime.Now.ToString("MM/dd/HH:mm:ss");
                SaveToFile(timestamp + "," + ts + "\n");
                _sttHistory.Add(ts);
            }
        }
        historyScroller.ReloadData();
        //if (historyScroller.gameObject.activeSelf)
            //historyScroller.JumpToDataIndex(_sttHistory.Count - 1);
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
        if (render3DTranscripts && xrTextContainer != null)
        {
            UpdateXrVis();
        }
    }

    /// <summary>
    /// Update the visualization in XR
    /// </summary>
    private void UpdateXrVis()
    {
        mWordCount = 0;
        string textContent = "";
        for (int i = _sttHistory.Count - 1; i >= 0; i--)
        {
            mWordCount += _sttHistory[i].Length;
            textContent = _sttHistory[i] + "  " + textContent;

            if (mWordCount > XR_TRANSCRIPTS_OUTPUT_LIMIT)
                break;
        }
        xrTextContainer.SetText(textContent);
    }

    /// <summary>
    /// Show the latest transcript on the screen
    /// </summary>
    private void UpdateTxtOutput()
    {
        string output = "";
        if (_sttHistory.Count > 0)
        {
            output = _sttHistory[_sttHistory.Count - 1];
        }
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
        //if (_sttHistory[dataIndex].Contains("P:"))
        //{
        //    cellView.GetComponent<Image>().color = new Color(0xff, 0xff, 0xff, 180);
        //}
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

    private string[] _testScripts = {
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. ",
        "Nulla efficitur nisi at sem porta finibus. Vestibulum ",
        "lacinia ultrices purus, eu maximus mauris maximus in. ",
        "In enim sapien, dignissim non maximus in, convallis at eros."
    };
    private int testScriptCount = 0;
    public void _TestAddTextToSttHistory()
    {
        SaveTranscript(new string[] { _testScripts[testScriptCount++] });
        testScriptCount %= _testScripts.Length;
        UpdateVis();
    }
}
