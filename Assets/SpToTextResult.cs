using UnityEngine;

[System.Serializable]
public class SpToTextResult 
{
    public string msg;
    public string[] transcript;
    public string[] keywords;
    public string[] topics;
    public string[][] subtopics;
    public string[][] examples;

    public static SpToTextResult CreateFromJson(string jsonString)
    {
        return JsonUtility.FromJson<SpToTextResult>(jsonString);
    }
}
