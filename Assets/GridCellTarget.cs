using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellTarget : MonoBehaviour
{
    private GridCellType cellType = GridCellType.REWARD;
    private Transform flagPole;
    private GridCellUpdateDelegate updateDelegate;

    void Start()
    {
        flagPole = transform.Find("FlagPole");
    }

    public void SetUpdateDelegate(GridCellUpdateDelegate d)
    {
        updateDelegate = d;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (flagPole.gameObject.activeSelf)
        {
            Debug.Log(string.Format("GRID, {0}, Target Collected", other.name));
            flagPole.gameObject.SetActive(false);
            if (updateDelegate != null)
            {
                updateDelegate(this.cellType, other);
            }
        }
    }

    public void Reset()
    {
        flagPole.gameObject.SetActive(true);
    }
}
