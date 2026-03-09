using System;

// 게임 전역 이벤트 버스 (static 클래스).
public static class GameEvents
{
    public static event Action<float> OnHpChanged;
    public static event Action<string, bool> OnWeaponChanged;
    public static event Action<int, int> OnAmmoChanged;
    public static event Action<int, int> OnScoreChanged;
    public static Action<float, float, float> OnSpreadUpdated;
    // 킬로그 (공격자 이름, 사망자 이름)
    public static event Action<string, string> OnKillLog;

    // 플레이어 UI 초기화 (액터번호, 이름, 팀번호)
    public static event Action<int, string, int> OnPlayerUIInit;

    // 플레이어 사망 UI (액터번호)
    public static event Action<int> OnPlayerUIDead;

    // 라운드 시작 / 종료
    public static event Action OnRoundStart;
    public static event Action OnRoundEnd;


    //  호출 메서드
    public static void HpChanged(float _hp)                             => OnHpChanged?.Invoke(_hp);
    public static void WeaponChanged(string _name, bool _isGun)         => OnWeaponChanged?.Invoke(_name, _isGun);
    public static void AmmoChanged(int _curAmmo, int _maxAmmo)          => OnAmmoChanged?.Invoke(_curAmmo, _maxAmmo);
    public static void SpreadUpdate(float _curAimSpread, float _RecoveryRate, float baseSpread)                       => OnSpreadUpdated?.Invoke(_curAimSpread, _RecoveryRate, baseSpread);
    public static void ScoreChanged(int _aTeam, int _bTeam)             => OnScoreChanged?.Invoke(_aTeam, _bTeam);
    public static void Kill(string _killName, string _dieName)          => OnKillLog?.Invoke(_killName, _dieName);
    public static void PlayerUIInit(int _no, string _name, int _team)   => OnPlayerUIInit?.Invoke(_no, _name, _team);
    public static void PlayerUIDead(int _actorNumber)                   => OnPlayerUIDead?.Invoke(_actorNumber);
    public static void RoundStart()                                     => OnRoundStart?.Invoke();
    public static void RoundEnd()                                       => OnRoundEnd?.Invoke();
}