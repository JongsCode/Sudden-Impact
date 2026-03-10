using UnityEngine;
using TMPro;
using System.Collections;

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
    [Header("Team Status UI")]
    [Tooltip("화면 좌측 최상단에 배치된 A팀 슬롯들 (4개)")]
    [SerializeField] private PlayerSlotUI[] teamASlots;

    [Tooltip("화면 우측 최상단에 배치된 B팀 슬롯들 (4개)")]
    [SerializeField] private PlayerSlotUI[] teamBSlots;

    // 킬 로그 UI
    [Header("Kill Log UI")]
    [Tooltip("Vertical Layout Group 부모 Transform")]
    [SerializeField] private Transform killLogParent;

    [Tooltip("KillLogEntry 컴포넌트가 붙은 UI 텍스트 프리팹")]
    [SerializeField] private KillLogEntry killLogEntryPrefab;

    [Header("Crosshair Settings")]
    [SerializeField] private float baseSize = 60f;        // 기본 에임 크기 (픽셀)
    [SerializeField] private float spreadSensitivity = 5f; // 1도당 벌어질 픽셀 거리 (중요!)
    [SerializeField] private RectTransform crosshairUI;
    [SerializeField] private Transform targetTransform; // InputManager의 mouseAimTarget

    [Header("Result UI")]
    [SerializeField] private GameObject resultPanel; // 승리/패배 패널
    [SerializeField] private TextMeshProUGUI resultText;



    private Camera mainCam;
    private Coroutine crosshairCoroutine;
    private float currentSpread = 1f;   // UI가 현재 보여주는 부드러운 값
    private float lastRecRate = 15f;    // 가장 최근 총기에서 받은 회복 속도
    private float lastBaseSpread = 1f;  // 가장 최근 총기에서 받은 기본 크기


    // 라이프사이클
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

        mainCam = Camera.main;
        // 이벤트 버스 구독: 무기가 발사될 때 크로스헤어 키우기

        //마우스의 비주얼을 끔
        Cursor.visible = false;

        // 마우스가 게임 창 밖으로 나가지 못하게 
        Cursor.lockState = CursorLockMode.Confined;

    }

    private void Update()
    {
        // 회복 로직: 총기(AutomaticGun)의 MoveTowards와 동일한 수식 사용
        if (currentSpread > lastBaseSpread)
        {
            currentSpread = Mathf.MoveTowards(currentSpread, lastBaseSpread, lastRecRate * Time.deltaTime);
        }

        // 9-슬라이스 스케일 적용
        if (crosshairUI != null)
        {
            // 수정: 배율이 아니라 '더하기' 방식으로 거리 조절
            // 예: 탄퍼짐이 5도 늘어나면 에임 크기는 5 * 5 = 25픽셀만 더 커짐
            float addedSize = (currentSpread - lastBaseSpread) * spreadSensitivity;
            float targetUISize = baseSize + addedSize;

            // 9슬라이스가 작동하도록 sizeDelta를 수정
            crosshairUI.sizeDelta = new Vector2(targetUISize, targetUISize);

            // localScale은 무조건 (1,1,1)이어야 선이 안 굵어집니다!
            crosshairUI.localScale = Vector3.one;
        }
    }

    private void LateUpdate()
    {
        if (targetTransform == null || crosshairUI == null) return;

        // 월드 좌표 -> 스크린 좌표 변환 후 UI 배치
        crosshairUI.position = mainCam.WorldToScreenPoint(targetTransform.position);
    }



    private void OnEnable()
    {
    
        GameEvents.OnHpChanged += HandleHpChanged;
        GameEvents.OnWeaponChanged += HandleWeaponChanged;
        GameEvents.OnAmmoChanged += HandleAmmoChanged;
        GameEvents.OnScoreChanged += HandleScoreChanged;
        GameEvents.OnSpreadUpdated += HandleSpreadUpdate;
        GameEvents.OnPlayerUIInit += HandlePlayerUIInit;
        GameEvents.OnPlayerUIDead += HandlePlayerUIDead;
        GameEvents.OnKillLog += HandleKillLog;
        GameEvents.OnMatchEnd += HandleMatchEnd;
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
        GameEvents.OnMatchEnd -= HandleMatchEnd;
    }

    private void HandleSpreadUpdate(float _cur, float _rec, float _baseVal)
    {
        // 총을 쏘는 순간, 현재 UI의 퍼짐 정도를 총기의 실제 탄퍼짐 값으로 점프시킴
        currentSpread = _cur;
        lastRecRate = _rec;
        lastBaseSpread = _baseVal;
    }

    public void SetAimTarget(Transform _target)
    {
        targetTransform = _target;
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

    private void HandleMatchEnd(int winTeam)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            // 내 팀 정보를 레지스트리나 로컬 속성에서 가져와 승패 판단
            resultText.text = (winTeam == 1) ? "TEAM A WIN" : "TEAM B WIN";
        }
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
        PlayerSlotUI[] targetSlots = (team == 1) ? teamASlots : teamBSlots; // 0이면 A팀, 아니면 B팀

        foreach (var slot in targetSlots)
        {
            if (!slot.gameObject.activeSelf)
                return slot;
        }
        return null;
    }
}
