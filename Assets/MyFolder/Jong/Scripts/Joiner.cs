using Photon.Pun;
using UnityEngine;

public class Joiner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject playerPrefab; // 내가 조종할 캐릭터 프리팹
    [SerializeField] private Transform[] spawnPoints; // 맵에 배치해 둔 스폰 위치들

    private void Start()
    {
        // 씬이 로드되자마자, 각자 자기 컴퓨터에서 자기 캐릭터를 소환합니다.
        SpawnPlayer();
    }
    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = GetSpawnPosition();
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = (PhotonNetwork.CurrentRoom.PlayerCount - 1) % spawnPoints.Length;
            return spawnPoints[index].position;
        }

        return new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
    }
}
