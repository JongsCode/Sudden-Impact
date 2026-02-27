using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private PlayerRegistry playerRegistry;

    [Header("Parameter")]
    [SerializeField] private float camHight = 20f;

    private void Awake()
    {
        playerRegistry.OnPlayerRegistered += SetPlayer;
    }

    private void Update()
    {
        if (player == null) return;
        transform.position = player.transform.position + (Vector3.up * camHight);
    }

    private void SetPlayer(PlayerController _player)
    {
        player = _player.gameObject;
    }

}
