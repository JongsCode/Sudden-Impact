using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    [Header("Flags")]
    [SerializeField] private Flag[] mapFlags;

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

    #region 외부 호출

    // -----------------------------------------------
    // 외부 호출 - 플레이어 사망 시 PlayerController에서 호출
    // -----------------------------------------------

    public void OnPlayerDied(PlayerController player)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 마스터 클라이언트만 승패 판단

        // 죽은 플레이어가 적 깃발을 들고 있다면?
        if (player.HasEnemyFlag)
        {
            int droppedFlagIndex = (player.MyTeam == 0) ? 1 : 0; // A팀이 죽었으면 B팀(1) 깃발을 떨어뜨림

            // 모든 클라이언트에게 깃발을 이 위치에 떨어뜨리라고 명령!
            photonView.RPC(nameof(DropFlagRPC), RpcTarget.All, droppedFlagIndex, player.transform.position);
        }

        CheckRoundEnd();
    }

    [PunRPC]
    private void DropFlagRPC(int _flagIndex, Vector3 _dropPos)
    {
        mapFlags[_flagIndex].DropFlag(_dropPos);
    }

    #endregion

    #region 라운드 체크

    // -----------------------------------------------
    // 라운드 체크
    // -----------------------------------------------


    // 상황 A: 빈손으로 적 깃발을 만짐 -> 획득!
    // 여기서 마스터 클라이언트에게 "나 이거 먹었어!" 라고 RPC를 쏴서 전 세계 동기화
    public void OnLocalPlayerTouchedFlag(int flagTeam, int flagIndex)
    {
        Debug.Log("[DebugGameManager] OnLocalPlayerTouchedFlag Calld");

        // 1. 레지스트리에서 '나'를 찾는다. 
        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        if (!playerRegistry.TryGetPlayerByActorNumber(myActorNumber, out PlayerController myPlayer)) return;

        if (myPlayer == null || myPlayer.GetPlayerState == PlayerController.PlayerState.Dead) return;

        if (!myPlayer.HasEnemyFlag && myPlayer.MyTeam != flagTeam)
        {
            photonView.RPC(nameof(ProcessFlagPickupRPC), RpcTarget.All, myActorNumber, flagTeam, flagIndex);
        }
    }

    // 상황 B: 적 깃발을 들고 우리 팀 깃발(베이스)을 만짐 -> 득점!
    // 득점 RPC 호출 (섬멸전 때 짰던 OnRoundEndRPC 재활용)
    public void OnLocalPlayerReachedGoal(int goalTeam)
    {
        Debug.Log("[DebugGameManager] OnLocalPlayerReachedGoal Calld");

        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        if (!playerRegistry.TryGetPlayerByActorNumber(myActorNumber, out PlayerController myPlayer)) return;

        if (myPlayer == null || myPlayer.GetPlayerState == PlayerController.PlayerState.Dead) return;

        if (myPlayer.HasEnemyFlag && myPlayer.MyTeam == goalTeam)
        {
            photonView.RPC(nameof(OnRoundEndRPC), RpcTarget.All, myPlayer.MyTeam);
        }
    }

    [PunRPC]
    private void ProcessFlagPickupRPC(int _myActorNumber, int _flagTeam, int _flagIndex)
    {
        if (!playerRegistry.TryGetPlayerByActorNumber(_myActorNumber, out PlayerController myPlayer)) return;

        myPlayer.GetFlag();

        if (_flagIndex >= 0 && _flagIndex < mapFlags.Length)
        {
            mapFlags[_flagIndex].HideFlag();
        }
    }

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

            // 3단계: 사망 확인
            // 내부 상태 값을 검사
            if (player.GetPlayerState != PlayerController.PlayerState.Dead)
            {
                return true; // 한 명이라도 켜져 있으면 팀은 살아있음
            }
        }
        return false;
    }

    #endregion

    #region 라운드 종료

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
        foreach(var player in playerRegistry.TeamA)
        {
            player.OnRoundEndReset();
        }
        foreach(var player in playerRegistry.TeamB)
        {
            player.OnRoundEndReset();
        }

        Debug.Log($"[GameManager] {roundStartDelay}초 후 다음 라운드 시작");
        yield return new WaitForSeconds(roundStartDelay);
        StartRound();
    }



    #endregion

    #region 라운드 시작

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
        for (int i = 0; i < mapFlags.Length; i++)
        {
            mapFlags[i].RespawnFlag();
        }

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

    #endregion

    #region 매치 종료

    // -----------------------------------------------
    // 매치 종료
    // -----------------------------------------------

    private void OnMatchEnd(int winTeam)
    {
        Debug.Log($"[GameManager] 매치 종료 | 승리팀: {(winTeam == 0 ? "A팀" : "B팀")}");
        playerRegistry.Clear();

        // TODO: 결과 화면 UI 호출
    }

    #endregion
}