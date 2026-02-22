using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerRegistry", menuName = "SuddenImpact/PlayerRegistry")]
public class PlayerRegistry : ScriptableObject
{
    [SerializeField] private List<PlayerController> teamA = new List<PlayerController>(); // 팀별 플레이어가 담기는 곳
    [SerializeField] private List<PlayerController> teamB = new List<PlayerController>();

    [Header("LocalData")]
    [SerializeField] private PlayerController localPlayer; // 로컬 플레이어를 담는 곳
    [SerializeField] private int myTeam; 

    public List<PlayerController> TeamA { get { return teamA; } }
    public List<PlayerController> TeamB { get{ return teamB; } }
    public PlayerController LocalPlayer { get { return localPlayer; } }
    public int MyTeam { get { return myTeam; } }

    public event Action<PlayerController> OnPlayerRegistered;

    public void RegisterLocalPlayer(PlayerController player)
    {
        localPlayer = player;
        OnPlayerRegistered?.Invoke(player);
    }

    public void RegisterMyTeam(int _team)
    {
        myTeam = _team;
    }

    public void RegisterPlayerTeam(PlayerController player, int team)
    {
        if (team == 0) teamA.Add(player);
        else teamB.Add(player);
    }

    // 게임 종료 시 반드시 초기화
    public void Clear()
    {
        localPlayer = null;
        teamA.Clear();
        teamB.Clear();
    }
}