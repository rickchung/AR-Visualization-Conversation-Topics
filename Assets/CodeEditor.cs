using System.Collections.Generic;
using UnityEngine;

public class CodeEditor : MonoBehaviour
{
    public CodeInterpreter codeInterpreter;
    public OneCmdEditingArea oneCmdEditingArea;
    public LoopEditingArea loopEditingArea;
    public CmdArgEditingArea cmdWithNumberEditingArea;
    private GameObject openedEditorArea;
    private CodeObjectOneCommand currentBeingEdited;

    private void Start()
    {
        transform.gameObject.SetActive(false);
        oneCmdEditingArea.gameObject.SetActive(false);
        loopEditingArea.gameObject.SetActive(false);
        cmdWithNumberEditingArea.gameObject.SetActive(false);
    }

    public void LoadModifiableCommands(AvatarController avatar)
    {
        cmdWithNumberEditingArea.SetModifiableCmdList(
            avatar.GetModifiableCmds(),
            avatar.GetModifiableCmdsWithArgs()
        );
    }

    public void DispatchEditor(CodeObjectOneCommand codeObject)
    {
        // Remove the previous highlight and set a new one
        if (currentBeingEdited != null)
            currentBeingEdited.IsBeingEdited = false;
        codeObject.IsBeingEdited = true;
        currentBeingEdited = codeObject;

        switch (codeObject.GetCommand())
        {
            // TODO: oneCmdEditingArea is a legacy editer. Use cmdWithNumberEditingArea instead.
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

        DataLogger.Log(
            this.gameObject, LogTag.CODING,
            "The code editor is open for : " + codeObject.ToString());
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

    public string[] GetArgOptions(string cmd)
    {
        return codeInterpreter.GetArgOptions(cmd);
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

