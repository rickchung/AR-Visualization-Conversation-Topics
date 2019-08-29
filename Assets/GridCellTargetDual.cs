using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellTargetDual : MonoBehaviour
{
    private GridCellType cellType = GridCellType.REWARD_DUAL;
    private Transform flagPoleLocal, flagPoleRemote;
    private GridCellUpdateDelegate updateDelegate;

    void Start()
    {
        flagPoleLocal = transform.Find("FlagPoleLocal");
        flagPoleRemote = transform.Find("FlagPoleRemote");
    }

    public void SetUpdateDelegate(GridCellUpdateDelegate d)
    {
        updateDelegate = d;
    }

    private void OnTriggerEnter(Collider other)
    {
        var otherAvatar = other.GetComponent<AvatarController>();
        if (otherAvatar != null)
        {
            var flagToTrigger = otherAvatar.isRival ? flagPoleRemote : flagPoleLocal;
            if (flagToTrigger.gameObject.activeSelf)
            {
                DataLogger.Log(
                    this.gameObject, LogTag.MAP,
                    string.Format("{0}, A dual target is collected isRival={1}",
                        other.name, otherAvatar.isRival
                    )
                );

                flagToTrigger.gameObject.SetActive(false);
                if (updateDelegate != null)
                {
                    updateDelegate(this.cellType, other);
                }
            }
        }
    }

    public void Reset()
    {
        if (flagPoleLocal != null)
            flagPoleLocal.gameObject.SetActive(true);
        if (flagPoleRemote != null)
            flagPoleRemote.gameObject.SetActive(true);
    }
}
