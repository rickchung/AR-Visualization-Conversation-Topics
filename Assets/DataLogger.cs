using System.IO;
using System;
using UnityEngine;

public enum LogTag
{
    CODING, SCRIPT, MAP,
    SCRIPT_ERROR, SCRIPT_WARNING,
    MAP_ERROR, MAP_WARNING,
    SYSTEM, SYSTEM_ERROR, SYSTEM_WARNING,
    AUDIO_CTRL
};

public class DataLogger : MonoBehaviour
{
    public static DataLogger dataLogger;
    private static string logFilePath;

    private void Awake()
    {
        dataLogger = this;
        var tnow = DateTime.Now.ToString().Replace('/', '-').Replace(':', '-').Replace(' ', '-');
        logFilePath = Path.Combine(Application.persistentDataPath, tnow) + "-Log.txt";

        Debug.Log("Log data will be saved to " + logFilePath);
    }

    public static void Log(GameObject self, LogTag tag, string msg)
    {
        if (dataLogger != null)
        {
            var timestamp = System.DateTime.Now.ToString("MM/dd/HH:mm:ss");
            var logOutput = string.Format(
                "{3}, {0}, {1}, {2}", self.name, tag.ToString(), msg, timestamp.ToString()
            );
            Debug.Log(logOutput);

            using (var writer = new StreamWriter(logFilePath, append: true))
            {
                writer.Write(logOutput + "\n");
            }
        }
        else
        {
            Debug.LogError("The data logger is not set properly.");
        }
    }

    public static void Log(LogTag tag, string msg)
    {
        if (dataLogger != null)
        {
            var timestamp = System.DateTime.Now.ToString("MM/dd/HH:mm:ss");
            var logOutput = string.Format(
                "{3}, {0}, {1}, {2}", "Static", tag.ToString(), msg, timestamp.ToString()
            );
            Debug.Log(logOutput);

            using (var writer = new StreamWriter(logFilePath, append: true))
            {
                writer.Write(logOutput + "\n");
            }
        }
        else
        {
            Debug.LogError("The data logger is not set properly.");
        }
    }
}
