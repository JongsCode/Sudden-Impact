using UnityEngine;
using TMPro;

/// <summary>
/// 전체 HUD 관리 매니저.
/// </summary>
public class UIManager : MonoBehaviour
{
    //
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject ammoPanel;

    // 팀 생존 슬롯 UI 
    [Header("Team Status UI (Feature 4-A)")]
    [Tooltip("화면 좌측 최상단에 배치된 A팀 슬롯들 (4개)")]
    [SerializeField] private PlayerSlotUI[] teamASlots;

    [Tooltip("화면 우측 최상단에 배치된 B팀 슬롯들 (4개)")]
    [SerializeField] private PlayerSlotUI[] teamBSlots;

    // 킬 로그 UI
    [Header("Kill Log UI (Feature 4-B)")]
    [Tooltip("Vertical Layout Group 부모 Transform")]
    [SerializeField] private Transform killLogParent;

    [Tooltip("KillLogEntry 컴포넌트가 붙은 UI 텍스트 프리팹")]
    [SerializeField] private KillLogEntry killLogEntryPrefab;

    // 라이프사이클

    private void OnEnable()
    {
        // 기존 이벤트
        GameEvents.OnHpChanged += HandleHpChanged;
        GameEvents.OnWeaponChanged += HandleWeaponChanged;
        GameEvents.OnAmmoChanged += HandleAmmoChanged;
        GameEvents.OnScoreChanged += HandleScoreChanged;

        // [기능 4] 신규 이벤트
        GameEvents.OnPlayerUIInit += HandlePlayerUIInit;
        GameEvents.OnPlayerUIDead += HandlePlayerUIDead;
        GameEvents.OnKillLog += HandleKillLog;
    }

    private void OnDisable()
    {
        GameEvents.OnHpChanged -= HandleHpChanged;
        GameEvents.OnWeaponChanged -= HandleWeaponChanged;
        GameEvents.OnAmmoChanged -= HandleAmmoChanged;
        GameEvents.OnScoreChanged -= HandleScoreChanged;

        GameEvents.OnPlayerUIInit -= HandlePlayerUIInit;
        GameEvents.OnPlayerUIDead -= HandlePlayerUIDead;
        GameEvents.OnKillLog -= HandleKillLog;
    }

    private void Awake()
    {
        // 1. A팀 슬롯 전원 비활성화
        if (teamASlots != null)
        {
            foreach (var slot in teamASlots)
            {
                if (slot != null) slot.gameObject.SetActive(false);
            }
        }

        // 2. B팀 슬롯 전원 비활성화
        if (teamBSlots != null)
        {
            foreach (var slot in teamBSlots)
            {
                if (slot != null) slot.gameObject.SetActive(false);
            }
        }
    }

    private void HandleHpChanged(float _hp)
        => hpText.text = $"HP: {_hp} / 100";

    private void HandleAmmoChanged(int _cur, int _max)
        => ammoText.text = $"{_cur} / {_max}";

    private void HandleWeaponChanged(string _name, bool _isGun)
    {
        weaponNameText.text = _name;
        ammoPanel.SetActive(_isGun);
    }

    private void HandleScoreChanged(int _aTeam, int _bTeam)
        => scoreText.text = $"{_aTeam} : {_bTeam}";

    // 팀 슬롯 핸들러
    /// <summary>
    /// 플레이어 입장 시 빈 슬롯에 이름·팀 정보를 채우고 활성화
    /// </summary>
    private void HandlePlayerUIInit(int actorNumber, string playerName, int team)
    {
        // 이미 들어온 사람인지 확인 (재접속 방지)
        PlayerSlotUI slot = FindSlotByActor(actorNumber);

        // 처음 들어온 사람이면 팀에 맞는 빈 슬롯 찾기
        if (slot == null)
        {
            slot = FindEmptySlot(team);
        }

        if (slot == null)
        {
            Debug.LogWarning($"[UIManager] {team}팀의 남은 플레이어 슬롯이 없습니다!");
            return;
        }

        slot.gameObject.SetActive(true);
        slot.Init(actorNumber, playerName, team);
    }

    /// <summary>
    /// 플레이어 사망 시 해당 슬롯을 흑백/어둡게 처리 (SetActive false 아님)
    /// </summary>
    private void HandlePlayerUIDead(int actorNumber)
    {
        PlayerSlotUI slot = FindSlotByActor(actorNumber);
        slot?.SetDead();
    }
    // 킬 로그 핸들러
    /// <summary>
    /// 킬 발생 시 killLogParent 아래에 새 엔트리를 Instantiate.
    /// KillLogEntry 자체가 3초 표시 후 페이드 아웃 → 자기 자신을 Destroy.
    /// </summary>
    private void HandleKillLog(string killer, string victim)
    {
        if (killLogEntryPrefab == null || killLogParent == null) return;

        KillLogEntry entry = Instantiate(killLogEntryPrefab, killLogParent);
        entry.Show(killer, victim);
    }

    // 유틸리티
    private PlayerSlotUI FindSlotByActor(int actorNumber)
    {
        // A팀 슬롯
        foreach (var slot in teamASlots)
        {
            if (slot.gameObject.activeSelf && slot.ActorNumber == actorNumber)
                return slot;
        }
        // 없으면 B팀 슬롯 
        foreach (var slot in teamBSlots)
        {
            if (slot.gameObject.activeSelf && slot.ActorNumber == actorNumber)
                return slot;
        }
        return null;
    }

    // 팀 번호에 따라 올바른 배열에서 빈자리를 찾아주는 함수
    private PlayerSlotUI FindEmptySlot(int team)
    {
        PlayerSlotUI[] targetSlots = (team == 0) ? teamASlots : teamBSlots; // 0이면 A팀, 아니면 B팀

        foreach (var slot in targetSlots)
        {
            if (!slot.gameObject.activeSelf)
                return slot;
        }
        return null;
    }
}
