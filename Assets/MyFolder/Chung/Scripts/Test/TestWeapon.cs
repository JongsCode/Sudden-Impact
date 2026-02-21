using UnityEngine;
using Photon.Pun;

public class TestWeapon : MonoBehaviourPun
{
    public bool IsEquit;

    private void Awake()
    {
        IsEquit = false;
    }
}
