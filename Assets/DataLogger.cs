﻿using UnityEngine;

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

    private void Awake()
    {
        dataLogger = this;
    }

    public static void Log(GameObject self, LogTag tag, string msg)
    {
        if (dataLogger != null)
        {
            var timestamp = System.DateTime.Now.ToString("MM/dd/HH:mm:ss");
            Debug.Log(string.Format(
                "{3}, {0}, {1}, {2}", self.name, tag.ToString(), msg, timestamp.ToString()
            ));
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
            Debug.Log(string.Format(
                "{3}, {0}, {1}, {2}", "Static", tag.ToString(), msg, timestamp.ToString()
            ));
        }
        else
        {
            Debug.LogError("The data logger is not set properly.");
        }
    }
}
