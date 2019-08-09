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
    public Text transTextOutput;
    public TMPro.TextMeshPro xrTextContainer;
    public TMPro.TextMeshPro xrTopicContainer;
    // The following two variables should be set at the same time.
    public EnhancedScroller transHistoryScroller;
    public TransCellView transHistoryCellviewPrefab;
    public TopicScroller topicHistoryScroller;
    public StatChartController statChartController;

    private const int XR_TRANSCRIPTS_OUTPUT_LIMIT = 30;  // Chars
    private const int LIMIT_NUM_TOPIC = 3;  // Chars
    private int mWordCount;
    private const int TOPIC_LIST_LIMIT = 3;

    private List<string> _sttHistory;
    private List<string[]> _sttTopics;
    private int numOfPhases = 0;
    private int numOfSpokenWords = 0;
    private int numOfRemotePhases = 0;
    private int numOfRemoteSpokenWords = 0;

    private string _sttHistoryFilePath;
    private string _sttHistoryFilename;

    // ======================================================================
    // Unity lifecycle

    void Start()
    {
        _sttHistoryFilename = System.Guid.NewGuid().ToString() + ".trans.txt";
        _sttHistoryFilePath = Path.Combine(Application.persistentDataPath, _sttHistoryFilename);

        _sttHistory = new List<string>();
        _sttTopics = new List<string[]>();

        // Enable the scroller of transaction history
        if (transHistoryScroller != null)
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
        string[] topics = stt.topics;
        SaveTranscript(text, isLocal);
        SaveTopics(topics, isLocal);
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
                    SaveToFile(timestamp + ",TRANS," + ts + "\n");
                    _sttHistory.Add(ts);
                }

                // Statistics for charts
                if (isLocal)
                {
                    numOfPhases++;
                    numOfSpokenWords += ts.Split(' ').Length;
                }
                else
                {
                    numOfRemotePhases++;
                    numOfRemoteSpokenWords += ts.Split(' ').Length;
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
            SaveToFile(timestamp + ",TOPIC," + string.Join(";", topics) + "\n");

            if (_sttTopics.Count >= TOPIC_LIST_LIMIT)
            {
                _sttTopics.RemoveAt(0);
            }
            _sttTopics.Add(topics);
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
        if (_sttTopics.Count > 0)
        {
            // Make a single string of the latest topics
            var selected = new List<string>();
            string content = "";
            int contentCount = 0;

            for (int j = _sttTopics.Count - 1; j >= 0; j--)
            {
                string[] topicArray = _sttTopics[j];
                for (int i = 0; i < topicArray.Length; i++)
                {
                    // content is the text shown in AR, which should not be too long
                    if (contentCount < LIMIT_NUM_TOPIC)
                    {
                        content += "Topic: " + topicArray[i] + "\n";
                        contentCount++;
                    }
                    // "selected" is the topic array containing all the topics so far.
                    // This will be rendered on the screen scroller.
                    string topicUpper = topicArray[i].ToUpper();
                    if (selected.IndexOf(topicUpper) < 0)
                        selected.Add(topicUpper);
                }
            }

            // Set the AR text
            if (xrTopicContainer != null)
                xrTopicContainer.SetText(content);
            // Update the 2D scroller
            if (topicHistoryScroller != null)
                topicHistoryScroller.ReplaceTopicsAndRefresh(selected);
        }
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
            statChartController.UpdateNumPhaseChart(numOfPhases, numOfRemotePhases);
            statChartController.UpdateNumWordChart(numOfSpokenWords, numOfPhases, numOfRemoteSpokenWords, numOfRemotePhases);
        }
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
