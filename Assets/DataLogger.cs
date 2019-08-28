using UnityEngine;

public enum LogTag
{
    CODING, SCRIPT, MAP,
    SCRIPT_ERROR, SCRIPT_WARNING,
    MAP_ERROR, MAP_WARNING,
    SYSTEM, SYSTEM_ERROR, SYSTEM_WARNING
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
            Debug.Log(string.Format(
                "{0}, {1}, {2}", self.name, tag.ToString(), msg
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
            Debug.Log(string.Format(
                "{0}, {1}, {2}", "Static", tag.ToString(), msg
            ));
        }
        else
        {
            Debug.LogError("The data logger is not set properly.");
        }
    }
}
