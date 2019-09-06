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
    // These variables are assigned in the Unity inspector. They can be None
    // if you don't need the corresponding outputs.
    [HideInInspector] public Text transTextOutput;
    [HideInInspector] public TMPro.TextMeshPro xrTextContainer;
    [HideInInspector] public TMPro.TextMeshPro xrTopicContainer;
    // The following two variables should be set at the same time.
    public EnhancedScroller transHistoryScroller;
    public TransCellView transHistoryCellviewPrefab;
    public TopicScroller topicHistoryScroller;
    public StatChartController statChartController;
    public bool enableSocialVis;

    private const int XR_TRANSCRIPTS_OUTPUT_LIMIT = 30;  // Chars
    private int mWordCount;

    private List<string> _sttHistory;

    private string _sttHistoryFilePath;
    private string _sttHistoryFilename;

    // ======================================================================
    // Unity lifecycle

    void Start()
    {
        _sttHistoryFilename = System.Guid.NewGuid().ToString() + ".trans.txt";
        _sttHistoryFilePath = Path.Combine(Application.persistentDataPath, _sttHistoryFilename);

        _sttHistory = new List<string>();

        // Enable the scroller of transaction history
        if (transHistoryScroller != null && transHistoryScroller.gameObject.activeSelf)
        {
            transHistoryScroller.Delegate = this;
            transHistoryScroller.ReloadData();
            ToggleTransHistoryPane();
        }
    }

    // ======================================================================
    // Saving functions used as interfaces

    /// <summary>
    /// This wrapper method accepts an SpToTextResult object and save the transcript
    /// and keywords inside the object. Note it is different from the methods
    /// SaveTranscript and SaveTopics which are used to perform the actual
    /// data saving tasks.
    /// </summary>
    /// <param name="stt"></param>
    public void SaveTransResponse(SpToTextResult stt, bool isLocal)
    {
        string[] text = stt.transcript;
        // string[] topics = stt.topics;
        // string[] keywords = stt.keywords;
        SaveTranscript(text, isLocal);
        // SaveTopics(keywords, isLocal);  // Use keywords as topics
        SaveTopics(text, isLocal);  // Use text as topics
    }

    /// <summary>
    /// Save a new transcript
    /// </summary>
    /// <param name="text">Text.</param>
    public void SaveTranscript(string[] text, bool isLocal)
    {
        if (text.Length > 0)
        {
            foreach (string ts in text)
            {
                if (ts.Length > 0)
                {
                    var timestamp = System.DateTime.Now.ToString("MM/dd/HH:mm:ss");
                    var typestamp = "";
                    if (isLocal)
                        typestamp = ",TRANS,LOCAL,";
                    else
                        typestamp = ",TRANS,REMOTE,";
                    SaveToFile(timestamp + typestamp + ts + "\n");
                    _sttHistory.Add(ts);
                }
            }
            if (transHistoryScroller != null)
                transHistoryScroller.ReloadData();
        }
    }

    /// <summary>
    /// Saves the given topics into the transcript file.
    /// </summary>
    /// <param name="topics">Topics.</param>
    public void SaveTopics(string[] topics, bool isLocal)
    {
        if (topics.Length > 0)
        {
            var timestamp = System.DateTime.Now.ToString("MM/dd/HH:mm:ss");
            var joinedTopics = string.Join(", ", topics);
            var speaker = (isLocal ? StatChartController.USERNAME_LOCAL :
                StatChartController.USERNAME_REMOTE);
            var typestamp = ",TOPIC," + speaker + ",";

            SaveToFile(timestamp + typestamp + string.Join(";", topics) + "\n");

            // Statistics for charts
            statChartController.UpdateStat(1, topics.Length, isLocal);
            topicHistoryScroller.AddTopic(joinedTopics, speaker);
        }
    }

    /// <summary>
    /// Append a string to the transcript file.
    /// </summary>
    /// <param name="transcript">Transcript.</param>
    private void SaveToFile(string transcript)
    {
        File.AppendAllText(_sttHistoryFilePath, transcript);
    }


    // ======================================================================
    // Uploading functions used as interfaces

    /// <summary>
    /// The general update call
    /// </summary>
    public void UpdateVis()
    {
        UpdateTransUIText();
        UpdateTransVis();
        UpdateTopicVis();
        UpdateStatCharts();
    }

    /// <summary>
    /// Update the view of identified topics.
    /// </summary>
    private void UpdateTopicVis()
    {
        if (topicHistoryScroller != null)
            topicHistoryScroller.Refresh();
    }

    /// <summary>
    /// Update the visualization in XR
    /// </summary>
    private void UpdateTransVis()
    {
        mWordCount = 0;
        string textContent = "";
        for (int i = _sttHistory.Count - 1; i >= 0; i--)
        {
            mWordCount += _sttHistory[i].Length;
            textContent = _sttHistory[i] + "\n" + textContent + "\n";

            if (mWordCount > XR_TRANSCRIPTS_OUTPUT_LIMIT)
                break;
        }
        if (xrTextContainer != null)
            xrTextContainer.SetText(textContent);
    }

    /// <summary>
    /// Show the latest transcript on the screen
    /// </summary>
    private void UpdateTransUIText()
    {
        string output = "";
        if (_sttHistory.Count > 0)
        {
            output = _sttHistory[_sttHistory.Count - 1];
        }
        if (transTextOutput != null)
            transTextOutput.text = output;
    }

    private void UpdateStatCharts()
    {
        if (statChartController != null)
        {
            statChartController.UpdateNumPhaseChart(useSocialVis: enableSocialVis);
            statChartController.UpdateNumWordChart(useSocialVis: enableSocialVis);
        }
    }

    public void EnableSocialVis(bool value)
    {
        enableSocialVis = value;
    }

    // ======================================================================
    // Interfaces of the enhanced scroller for history of transcription

    /// <summary>
    /// Gets the number of cells for transcript history scroller.
    /// </summary>
    /// <returns>The number of cells.</returns>
    /// <param name="scroller">Scroller.</param>
    int IEnhancedScrollerDelegate.GetNumberOfCells(EnhancedScroller scroller)
    {
        return _sttHistory.Count;
    }

    /// <summary>
    /// Gets the size of the cell view for transcript history scroller.
    /// </summary>
    /// <returns>The cell view size.</returns>
    /// <param name="scroller">Scroller.</param>
    /// <param name="dataIndex">Data index.</param>
    float IEnhancedScrollerDelegate.GetCellViewSize(
        EnhancedScroller scroller, int dataIndex)
    {
        return 75f;
    }

    /// <summary>
    /// Gets the cell view for transcript history scroller.
    /// </summary>
    /// <returns>The cell view.</returns>
    /// <param name="scroller">Scroller.</param>
    /// <param name="dataIndex">Data index.</param>
    /// <param name="cellIndex">Cell index.</param>
    EnhancedScrollerCellView IEnhancedScrollerDelegate.GetCellView(
        EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        TransCellView cellView = scroller.GetCellView(transHistoryCellviewPrefab)
            as TransCellView;

        //if (_sttHistory[dataIndex].Contains("P:"))
        //{
        //    cellView.GetComponent<Image>().color = new Color(0xff, 0xff, 0xff, 180);
        //}

        cellView.SetData(_sttHistory[dataIndex]);
        return cellView;
    }


    // ======================================================================
    // UI Controllers

    /// <summary>
    /// Show/Hide the transcript history scroller. Used by buttons.
    /// </summary>
    public void ToggleTransHistoryPane()
    {
        transHistoryScroller.gameObject.SetActive(!transHistoryScroller.gameObject.activeSelf);
    }
}
