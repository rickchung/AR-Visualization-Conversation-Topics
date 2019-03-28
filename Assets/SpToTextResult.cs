using UnityEngine;

[System.Serializable]
public class SpToTextResult 
{
    public string msg;
    public string[] transcript;

    public static SpToTextResult CreateFromJson(string jsonString)
    {
        return JsonUtility.FromJson<SpToTextResult>(jsonString);
    }
}
