using System.Collections.Generic;
using UnityEngine;

public class CodeEditor : MonoBehaviour
{
    public CodeInterpreter codeInterpreter;
    public OneCmdEditingArea oneCmdEditingArea;
    public LoopEditingArea loopEditingArea;
    private GameObject openedEditorArea;

    private static List<string> availableMoveDirections = new List<string>() {
        GridController.Direction.NORTH.ToString(),
        GridController.Direction.SOUTH.ToString(),
        GridController.Direction.EAST.ToString(),
        GridController.Direction.WEST.ToString()
    };

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
                Debug.Log("Showing one command editor");
                oneCmdEditingArea.AttachCodeObject(codeObject, availableMoveDirections);
                DispatchRoutine(oneCmdEditingArea.gameObject);
                break;
            case "LOOP":
                Debug.Log("Showing loop editor");
                loopEditingArea.AttachCodeObject(codeObject, null);
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

