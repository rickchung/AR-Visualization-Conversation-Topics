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
    public TMPro.TextMeshPro xrTopicContainer;
    public EnhancedScroller historyScroller;
    public TransCellView transCellViewPrefab;
    public TopicScroller mTopicScroller;
    public bool render3DTranscripts;

    private const int XR_TRANSCRIPTS_OUTPUT_LIMIT = 30;  // Chars
    private const int LIMIT_NUM_TOPIC = 3;  // Chars
    private int mWordCount;
    private const int TOPIC_LIST_LIMIT = 3;

    private List<string> _sttHistory;
    private List<string[]> _sttTopics;

    private string _sttHistoryFilePath;
    private string _sttHistoryFilename;

    void Start()
    {
        _sttHistoryFilename = System.Guid.NewGuid().ToString() + ".trans.txt";
        _sttHistoryFilePath = Path.Combine(Application.persistentDataPath, _sttHistoryFilename);

        _sttHistory = new List<string>();
        _sttTopics = new List<string[]>();

        historyScroller.Delegate = this;
        historyScroller.ReloadData();
        ToggleTransHistoryPane();
    }

    // ======================================================================

    public void SaveTransResponse(SpToTextResult stt)
    {
        string[] text = stt.transcript;
        string[] topics = stt.topics;
        SaveTranscript(text);
        SaveTopics(topics);
    }

    /// <summary>
    /// Save a new transcript
    /// </summary>
    /// <param name="text">Text.</param>
    public void SaveTranscript(string[] text)
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
            }
            historyScroller.ReloadData();
        }
    }

    /// <summary>
    /// Saves the given topics into the transcript file.
    /// </summary>
    /// <param name="topics">Topics.</param>
    public void SaveTopics(string[] topics)
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
            UpdateTransVis();
        }
        if (xrTopicContainer != null)
        {
            UpdateTopicVis();
        }
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
            xrTopicContainer.SetText(content);
            // Update the 2D scroller
            mTopicScroller.ReplaceTopicsAndRefresh(selected);
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

    // ======================================================================

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
        TransCellView cellView = scroller.GetCellView(transCellViewPrefab)
            as TransCellView;

        //if (_sttHistory[dataIndex].Contains("P:"))
        //{
        //    cellView.GetComponent<Image>().color = new Color(0xff, 0xff, 0xff, 180);
        //}

        cellView.SetData(_sttHistory[dataIndex]);
        return cellView;
    }

    // ======================================================================

    /// <summary>
    /// Show/Hide the transcript history scroller. Used by buttons.
    /// </summary>
    public void ToggleTransHistoryPane()
    {
        historyScroller.gameObject.SetActive(!historyScroller.gameObject.activeSelf);
    }

    // ======================================================================

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
