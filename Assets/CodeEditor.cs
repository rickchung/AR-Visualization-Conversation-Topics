using System.Collections.Generic;
using UnityEngine;

public class CodeEditor : MonoBehaviour
{
    public CodeInterpreter codeInterpreter;
    public OneCmdEditingArea oneCmdEditingArea;
    public LoopEditingArea loopEditingArea;
    public CmdWithNumberEditingArea cmdWithNumberEditingArea;
    private GameObject openedEditorArea;
    private CodeObjectOneCommand currentBeingEdited;

    private void Start()
    {
        transform.gameObject.SetActive(false);
        oneCmdEditingArea.gameObject.SetActive(false);
        loopEditingArea.gameObject.SetActive(false);
        cmdWithNumberEditingArea.gameObject.SetActive(false);
    }

    public void DispatchEditor(CodeObjectOneCommand codeObject)
    {
        DataLogger.Log(
          this.gameObject, LogTag.CODING,
          "The code editor is open for : " + codeObject.ToString()
        );

        // Remove the previous highlight and set a new one
        if (currentBeingEdited != null)
            currentBeingEdited.IsBeingEdited = false;
        codeObject.IsBeingEdited = true;
        currentBeingEdited = codeObject;

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
            default:
                cmdWithNumberEditingArea.AttachCodeObject(codeObject);
                cmdWithNumberEditingArea.codeViewUpdateDelegate = UpdateCodeViewer;
                DispatchRoutine(cmdWithNumberEditingArea.gameObject);
                break;
        }

        UpdateCodeViewer();
    }

    public void DismissEditor()
    {
        if (currentBeingEdited != null)
        {
            currentBeingEdited.IsBeingEdited = false;
            currentBeingEdited = null;
        }

        openedEditorArea = null;
        loopEditingArea.gameObject.SetActive(false);
        oneCmdEditingArea.gameObject.SetActive(false);
        cmdWithNumberEditingArea.gameObject.SetActive(false);
        transform.gameObject.SetActive(false);
        UpdateCodeViewer();
    }

    public void UpdateCodeViewer()
    {
        codeInterpreter.UpdateCodeViewer(scrollToTop: false);
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

