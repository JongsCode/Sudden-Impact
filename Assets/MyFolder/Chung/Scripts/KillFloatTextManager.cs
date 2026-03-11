using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 킬 팝업 텍스트 풀 매니저 (씬에 하나만 배치)
///
/// - Awake 시 poolSize 개수만큼 KillFloatText 프리팹 풀 생성
/// - OnEnable / OnDisable 페어로 GameEvents.OnKillFloat 구독 → 메모리 누수 방지
/// - Camera.main 한 번만 캐싱 (매 프레임 FindWithTag 비용 차단)
/// </summary>
public class KillFloatTextManager : MonoBehaviour
{
    public static KillFloatTextManager Instance { get; private set; }

    [SerializeField] private KillFloatText prefab;
    [SerializeField] private int poolSize = 8;
    [SerializeField] private string displayText = "KILL!";

    private readonly List<KillFloatText> _pool = new List<KillFloatText>();
    private Camera _cam;

    private void Awake()
    {
        Instance = this;
        _cam = Camera.main;

        for (int i = 0; i < poolSize; i++)
        {
            KillFloatText obj = Instantiate(prefab, transform);
            obj.gameObject.SetActive(false);
            _pool.Add(obj);
        }
    }

    // 이벤트 구독 / 해지 — 반드시 쌍으로 유지
    private void OnEnable() => GameEvents.OnKillFloat += HandleKillFloat;
    private void OnDisable() => GameEvents.OnKillFloat -= HandleKillFloat;

    private void HandleKillFloat(Vector3 position)
    {
        if (!TryGetFromPool(out KillFloatText text)) return;
        text.gameObject.SetActive(true);
        text.Play(position, displayText, _cam);
    }

    private bool TryGetFromPool(out KillFloatText text)
    {
        foreach (KillFloatText item in _pool)
        {
            if (!item.gameObject.activeSelf)
            {
                text = item;
                return true;
            }
        }
        text = null;
        return false;
    }
}
