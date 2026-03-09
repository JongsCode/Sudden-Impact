using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("CameraRference")]
    [SerializeField] private PlayerRegistry playerRegistry;
    [SerializeField] private CinemachineCamera virtualCam;
    [SerializeField] private CinemachineTargetGroup targetGroup; 
    [SerializeField] private Transform mouseAimTarget;       



    [Header("ForDebug")]
    [SerializeField] private GameObject player;

    private void Awake()
    {
        playerRegistry.OnPlayerRegistered += SetCameraTarget;
    }

    // 기존 업데이트에서 플레이어를 따라 가던 로직
    //private void Update()
    //{
    //    if (player == null) return;
    //    transform.position = player.transform.position + (Vector3.up * camHight);
    //}

    private void SetCameraTarget(PlayerController _player)
    {
        if (_player == null || targetGroup == null) return;

        //  플레이어를 그룹 멤버 0번으로 추가 (가중치 1.0)
        targetGroup.AddMember(_player.transform, 1f, 0f);

        // 에임 타겟을 그룹 멤버 1번으로 추가 (가중치 0.3 ~ 0.5)
        // 이 가중치가 높을수록 카메라가 마우스 쪽으로 더 많이 쏠림
        targetGroup.AddMember(mouseAimTarget, 0.3f, 0f);

        // 카메라가 그룹 전체를 추적하게 설정
        virtualCam.Follow = targetGroup.transform;

        Debug.Log($"[Camera] {_player.photonView.Owner.NickName}를 카메라 타겟으로 설정 완료!");
    }

}
