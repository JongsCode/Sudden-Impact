using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject ammoPanel;

    private void OnEnable()
    {
        // 이벤트 구독
        GameEvents.OnHpChanged += HandleHpChanged;
        GameEvents.OnWeaponChanged += HandleWeaponChanged;
        GameEvents.OnAmmoChanged += HandleAmmoChanged;
        GameEvents.OnScoreChanged += HandleScoreChanged;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지용 구독 해제
        GameEvents.OnHpChanged -= HandleHpChanged;
        GameEvents.OnWeaponChanged -= HandleWeaponChanged;
        GameEvents.OnAmmoChanged -= HandleAmmoChanged;
        GameEvents.OnScoreChanged -= HandleScoreChanged;
    }

    private void HandleHpChanged(float _hp) => hpText.text = $"HP: {_hp} / 100";
    private void HandleAmmoChanged(int _curAmmo, int _maxAmmo) => ammoText.text = $"{_curAmmo} / {_maxAmmo}";
    private void HandleWeaponChanged(string _name, bool _isGun)
    {
        weaponNameText.text = _name;
        if(_isGun)
        {
            ammoPanel.SetActive(true);
        }
        else
        {
            ammoPanel.SetActive(false);
        }
    }
    private void HandleScoreChanged(int _aTeam, int _bTeam) => scoreText.text = $"{_aTeam} : {_bTeam}";

    private void HandlePlayerUIInit(int _actorNumber, string _name, int _team)
    {
        // 자신의 상태는 자신이 변경해라 
    }
}