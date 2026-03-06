using System;

// 순수 static 클래스.
public static class GameEvents
{
    // 이벤트 정의
    public static event Action<float> OnHpChanged;
    public static event Action<string, bool> OnWeaponChanged;
    public static event Action<int, int> OnAmmoChanged;
    public static event Action<int, int> OnScoreChanged;
    // 킬로그 생성용 죽인 플레이어 이름과 죽은 플레이어 이름을 받음
    public static event Action<string, string> OnKillLog;
    // 플레이어가 스폰(등록)될 때 UI 아이콘을 생성하라는 이벤트 (액터번호, 이름, 팀 번호)
    public static event Action<int, string, int> OnPlayerUIInit;
    // 플레이어가 죽었을 때 UI 아이콘을 끄라는 이벤트 (액터번호)
    public static event Action<int> OnPlayerUIDead;
    


    // 호출용 래퍼 (PlayerController나 Weapon에서 이거 한 줄만 부르면 됨)
    public static void HpChanged(float _hp) => OnHpChanged?.Invoke(_hp);
    public static void WeaponChanged(string _name, bool _isGun) => OnWeaponChanged?.Invoke(_name, _isGun);
    public static void AmmoChanged(int _curAmmo, int _maxAmmo) => OnAmmoChanged?.Invoke(_curAmmo, _maxAmmo);
    public static void ScoreChanged(int _aTeam, int _bTeam) => OnScoreChanged?.Invoke(_aTeam, _bTeam);
    public static void Kill(string _killName, string _dieName) => OnKillLog?.Invoke(_killName, _dieName);
    public static void PlayerUIInit(int _actorNumber, string _name, int _team) => OnPlayerUIInit?.Invoke(_actorNumber, _name, _team);
    public static void PlayerUIDead(int _actorNumber) => OnPlayerUIDead?.Invoke(_actorNumber);
}