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
    public static string userFolderName;
    private static string userCamImgFolder;
    public static string username;
    private static string[] randWordList = {
        "Cheetah","Red","Citrus","Poppy","Julianne","Winnie","Alyson","Peaches","Mango","Blaze","Ginger","Apricot","Nicky","Marmalade","Orangey","Apricat","Buttercup","Pumpkin","Blizzard","Arlene","Tiffany","Coral","Autumn","Jess","Isla","Lioness","Treasure","Daisy","Carrots","Flame","Dana","Auburn","Tabasco","Nala","Salmon","Butterscotch","Copper","Stripes","Patchwork","Tangerine","Saffron","Cinnamon","Merlot","Paprika","Top Cat","Miss Natural","Scarlet","Amber","Tigger","OJ","Millie","Lauren","Ruth","Maria","Scarlett","Katherine","Emily","Jennifer","Hollie","Isabella",
    };

    private void Awake()
    {
        dataLogger = this;

        // Random name
        var rand = new System.Random();
        var randName = "";
        for (var i = 0; i < 2; i++)
        {
            randName += randWordList[rand.Next(randWordList.Length)];
            randName += "-";
        }
        // Add timestamp
        var tnow = DateTime.Now.ToString().Replace('/', '-').Replace(':', '-').Replace(' ', '-');
        randName += tnow;
        // Create a folder
        username = randName;
        userFolderName = Path.Combine(Application.persistentDataPath, randName);
        userCamImgFolder = Path.Combine(userFolderName, "Cam-Images");
        Directory.CreateDirectory(userFolderName);
        Directory.CreateDirectory(userCamImgFolder);
        // Log file name
        logFilePath = Path.Combine(userFolderName, tnow) + "-Log.txt";

        Debug.Log("Log data will be saved to " + logFilePath);
    }

    public static void Log(GameObject self, LogTag tag, string msg)
    {
        if (dataLogger != null)
        {
            var timestamp = System.DateTime.Now.ToString("MM/dd/HH:mm:ss:fffffff");
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
            var timestamp = System.DateTime.Now.ToString("MM/dd/HH:mm:ss:fffffff");
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

    public static void DumpWholeScript(ScriptObject script, string fnamePrefix = "")
    {
        var timestamp = System.DateTime.Now.ToString("MM-dd-HH-mm-ss");
        var filename = Path.Combine(
            userFolderName, fnamePrefix + "ScriptSnapshot-" + timestamp + ".txt"
        );
        using (var writer = new StreamWriter(filename))
        {
            writer.Write(script.ToString(richtext: false));
        }
    }

    public static void LogImage(byte[] image, string cam)
    {
        var timestamp = System.DateTime.Now.ToString("MM-dd-HH-mm-ss");
        var filename = Path.Combine(
            userCamImgFolder, cam + "-" + timestamp + ".jpg"
        );

        using (var writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
        {
            writer.Write(image);
        }
    }
}
