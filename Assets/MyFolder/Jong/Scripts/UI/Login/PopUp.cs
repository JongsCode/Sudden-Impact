using UnityEngine;
using Photon.Pun;

public abstract class PopUp : MonoBehaviourPunCallbacks
{
    public abstract void OnConfirm();
    public abstract void OnCancel();
}
