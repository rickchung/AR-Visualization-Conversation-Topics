using System.Collections.Generic;
using UnityEngine;

public class CodeEditor : MonoBehaviour
{
    public CodeInterpreter codeInterpreter;
    public OneCmdEditingArea oneCmdEditingArea;
    public LoopEditingArea loopEditingArea;
    private GameObject openedEditorArea;

    private void Start()
    {
        transform.gameObject.SetActive(false);
        oneCmdEditingArea.gameObject.SetActive(false);
        loopEditingArea.gameObject.SetActive(false);
    }

    public void DispatchEditor(CodeObjectOneCommand codeObject)
    {
        Debug.Log("Modifying code: " + codeObject.ToString());

        switch (codeObject.GetCommand())
        {
            case "MOVE":
                oneCmdEditingArea.AttachCodeObject(codeObject);
                oneCmdEditingArea.codeViewUpdateDelegate = UpdateCodeViewer;
                DispatchRoutine(oneCmdEditingArea.gameObject);
                break;
            case "LOOP":
                loopEditingArea.AttachCodeObject(codeObject);
                loopEditingArea.codeViewUpdateDelegate = UpdateCodeViewer;
                DispatchRoutine(loopEditingArea.gameObject);
                break;
        }
    }

    public void DismissEditor()
    {
        openedEditorArea = null;
        loopEditingArea.gameObject.SetActive(false);
        oneCmdEditingArea.gameObject.SetActive(false);
        transform.gameObject.SetActive(false);
        UpdateCodeViewer();
    }

    public void UpdateCodeViewer()
    {
        codeInterpreter.UpdateCodeViewer();
    }

    private void DispatchRoutine(GameObject toBeDispatch)
    {
        if (openedEditorArea != null)
        {
            openedEditorArea.SetActive(false);
        }
        toBeDispatch.SetActive(true);
        transform.gameObject.SetActive(true);
        openedEditorArea = toBeDispatch;
    }
}

