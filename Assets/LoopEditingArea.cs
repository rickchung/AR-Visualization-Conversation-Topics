using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoopEditingArea : MonoBehaviour
{
    public CodeEditor editorDispatcher;
    public TextMeshProUGUI loopTimesLabel;
    public Button loopTimesPlusBtn, loopTimesMinusBtn;
    public Button loopSubmitBtn;

    private CodeObject attachedCodeObject;

    public void AttachCodeObject(CodeObject codeObject)
    {
        attachedCodeObject = codeObject;
    }


    public void ApplyChangeToCodeObject()
    {
        if (attachedCodeObject != null)
        {
            // Clean up
            attachedCodeObject = null;
            editorDispatcher.DismissEditor();
        }
    }
}
