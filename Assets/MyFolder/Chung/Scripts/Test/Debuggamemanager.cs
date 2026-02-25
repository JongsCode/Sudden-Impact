using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

/// <summary>
/// 디버그용 경기 흐름 관리
/// 실제 GameManager로 재활용 예정
/// </summary>
public class DebugGameManager : MonoBehaviourPunCallbacks
{
    public static DebugGameManager Instance { get; private set; }

    [Header("Registry")]
    [SerializeField] private PlayerRegistry playerRegistry;

    [Header("경기 설정")]
    [SerializeField] private int winScore = 5;          // 5선승
    [SerializeField] private float roundStartDelay = 3f;

    [Header("스폰 설정")]
    [SerializeField] private Transform[] teamASpawnPoints;
    [SerializeField] private Transform[] teamBSpawnPoints;

    [Header("ForDebug")]
    [SerializeField] private int teamAScore = 0;
    [SerializeField] private int teamBScore = 0;

    [Header("StartButton")]
    [SerializeField] private Button startButton;

    // -----------------------------------------------

    private void Awake()
    {
        Instance = this;
        startButton.onClick.AddListener(StartRound);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        playerRegistry.Clear();
    }

    // -----------------------------------------------
    // 외부 호출 - 플레이어 사망 시 PlayerController에서 호출
    // -----------------------------------------------

    public void OnPlayerDied(PlayerController player)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 마스터 클라이언트만 승패 판단

        CheckRoundEnd();
    }

    // -----------------------------------------------
    // 라운드 체크
    // 나중에 깃발 조건 추가 시 여기만 수정
    // -----------------------------------------------

    private void CheckRoundEnd()
    {
        bool teamAAlive = IsTeamAlive(playerRegistry.TeamA);
        bool teamBAlive = IsTeamAlive(playerRegistry.TeamB);

        Debug.Log($"[GameManager] Team A Alive : {teamAAlive}, Team B Alive : {teamBAlive}");

        if (!teamAAlive)
        {
            // B팀 승리
            photonView.RPC(nameof(OnRoundEndRPC), RpcTarget.All, 1);
        }
        else if (!teamBAlive)
        {
            // A팀 승리
            photonView.RPC(nameof(OnRoundEndRPC), RpcTarget.All, 0);
        }
    }

    private bool IsTeamAlive(List<PlayerController> team)
    {
        if (team == null) return false;

        foreach (var player in team)
        {
            // 1단계: 메모리 상에 객체가 존재하는지 (MissingReference 방지)
            if (player == null) continue;

            // 2단계: 유니티 객체로서 유효한지 확인
            if (!player.gameObject) continue;

            // 3단계: 꺼져있는지 확인 
            // 만약 Active를 끄는 방식이라, activeInHierarchy가 false인 것이 '죽은 상태'를 의미함
            if (player.gameObject.activeInHierarchy)
            {
                return true; // 한 명이라도 켜져 있으면 팀은 살아있음
            }
        }
        return false;
    }


    // -----------------------------------------------
    // 라운드 종료
    // -----------------------------------------------

    [PunRPC]
    private void OnRoundEndRPC(int winTeam)
    {
        if (winTeam == 0) teamAScore++;
        else teamBScore++;

        Debug.Log($"[GameManager] 라운드 종료 | A팀: {teamAScore} / B팀: {teamBScore}");

        if (teamAScore >= winScore || teamBScore >= winScore)
        {
            OnMatchEnd(winTeam);
            return;
        }

        StartCoroutine(NextRoundCoroutine());
    }

    private IEnumerator NextRoundCoroutine()
    {
        Debug.Log($"[GameManager] {roundStartDelay}초 후 다음 라운드 시작");
        yield return new WaitForSeconds(roundStartDelay);
        StartRound();
    }

    // -----------------------------------------------
    // 라운드 시작
    // -----------------------------------------------

    public void StartRound()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(StartRoundRPC), RpcTarget.All);
    }

    [PunRPC]
    private void StartRoundRPC()
    {
        RespawnTeam(playerRegistry.TeamA, teamASpawnPoints);
        RespawnTeam(playerRegistry.TeamB, teamBSpawnPoints);
        Debug.Log("[GameManager] 라운드 시작");
    }

    private void RespawnTeam(List<PlayerController> team, Transform[] spawnPoints)
    {
        for (int i = 0; i < team.Count; i++)
        {
            if (team[i] == null) continue;

            Vector3 spawnPos = spawnPoints[i % spawnPoints.Length].position;
            team[i].gameObject.SetActive(true);
            team[i].Respawn(spawnPos);
        }
    }

    // -----------------------------------------------
    // 매치 종료
    // -----------------------------------------------

    private void OnMatchEnd(int winTeam)
    {
        Debug.Log($"[GameManager] 매치 종료 | 승리팀: {(winTeam == 0 ? "A팀" : "B팀")}");
        playerRegistry.Clear();

        // TODO: 결과 화면 UI 호출
    }
}