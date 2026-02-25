using UnityEngine;
using Photon.Pun;
using System.Collections;

public class Door : Furniture, IInteractable
{
    [Header("Door Controls")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 5f; // 문 열리는 속도
    [SerializeField] private Collider collider;

    private bool isOpen = false;
    private Coroutine doorCoroutine; // 중복 실행 방지용

    public void Interact(PlayerController player)
    {
        // 내 위치가 아닌 플레이어의 위치를 전송하여 모든 클라이언트에서 같은 방향으로 열리게 함
        photonView.RPC(nameof(RPC_ToggleDoor), RpcTarget.All, player.transform.position);
    }

    [PunRPC]
    private void RPC_ToggleDoor(Vector3 _playerPos)
    {
        if (isDestroyed) return;

        isOpen = !isOpen;

        // 기존에 움직이고 있었다면 멈추고 새로 시작
        if (doorCoroutine != null) StopCoroutine(doorCoroutine);

        float targetAngle = 0f;
        if (isOpen)
        {
            Vector3 dirToPlayer = (_playerPos - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToPlayer);
            targetAngle = dot > 0 ? openAngle : -openAngle;
        }

        doorCoroutine = StartCoroutine(AnimateDoor(targetAngle));
    }

    private IEnumerator AnimateDoor(float targetAngle)
    {
        Quaternion targetRot = Quaternion.Euler(0, targetAngle, 0);

       collider.isTrigger = true;
        while (Quaternion.Angle(doorPivot.localRotation, targetRot) > 0.1f)
        {
            doorPivot.localRotation = Quaternion.Slerp(
                doorPivot.localRotation,
                targetRot,
                Time.deltaTime * openSpeed
            );
            yield return null;
        }
        collider.isTrigger = false; 

        doorPivot.localRotation = targetRot;
        doorCoroutine = null;
    }
}