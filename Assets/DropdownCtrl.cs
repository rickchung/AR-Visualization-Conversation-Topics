using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DropdownCtrl : MonoBehaviour
{
    public TMP_Dropdown mapDropdown, scriptDropdown;
    public CodeInterpreter codeInterpreter;
    public GridController gridController;

    void Start()
    {
        mapDropdown.onValueChanged.AddListener((int value) =>
        {
            ReplaceMap(mapDropdown.options[value].text);
        });
        scriptDropdown.onValueChanged.AddListener((int value) =>
        {
            ReplaceScript(scriptDropdown.options[value].text);
        });
    }

    private void ReplaceScript(string scriptName)
    {
        codeInterpreter.LoadPredefinedScript(scriptName, broadcast: false);
    }
    private void ReplaceMap(string mapName)
    {
        gridController.LoadGridMap(mapName);
    }
}
