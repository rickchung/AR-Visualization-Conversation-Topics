using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellTarget : MonoBehaviour
{
    public GridController gridController;
    private Transform flagPole;

    void Start()
    {
        flagPole = transform.Find("FlagPole");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (flagPole.gameObject.activeSelf)
        {
            Debug.Log(string.Format("GRID, {0}, Target Collected", other.name));
            flagPole.gameObject.SetActive(false);
        }
    }

    public void Reset()
    {
        flagPole.gameObject.SetActive(true);
    }
}
