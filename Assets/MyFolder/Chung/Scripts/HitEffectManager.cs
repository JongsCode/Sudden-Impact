using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [기능 2] 레이캐스트 피격 지점에 파티클 이펙트를 재생하는 싱글톤 매니저.
///
/// 사용법 (Gun 서브클래스의 FireProjectile 등 레이캐스트 히트 처리 부분):
///   if (Physics.Raycast(..., out RaycastHit hit))
///   {
///       HitEffectManager.Instance.PlayHitEffect(hit.point, hit.normal);
///   }
/// </summary>
public class HitEffectManager : MonoBehaviour
{
    public static HitEffectManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("씬 폴더에 저장된 피격 파티클 프리팹")]
    [SerializeField] private ParticleSystem hitEffectPrefab;

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 10;

    private readonly Queue<ParticleSystem> _pool = new Queue<ParticleSystem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 초기 풀 생성
        for (int i = 0; i < poolSize; i++)
        {
            var ps = Instantiate(hitEffectPrefab, transform);
            ps.gameObject.SetActive(false);
            _pool.Enqueue(ps);
        }
    }

    /// <summary>
    /// hit.point + hit.normal을 받아 이펙트를 재생하고 1초 후 풀에 반납합니다.
    /// </summary>
    public void PlayHitEffect(Vector3 position, Vector3 normal)
    {
        PlayHitEffect(position, Quaternion.LookRotation(normal));
    }

    /// <summary>
    /// 회전값을 직접 지정해서 이펙트를 재생합니다.
    /// </summary>
    public void PlayHitEffect(Vector3 position, Quaternion rotation)
    {
        var effect = GetOrCreate();
        effect.transform.SetPositionAndRotation(position, rotation);
        effect.gameObject.SetActive(true);
        effect.Play();
        StartCoroutine(ReturnToPoolAfter(effect, 1f));
    }

    // ─── 내부 ──────────────────────────────────────────────────────

    private ParticleSystem GetOrCreate()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        // 풀이 부족하면 임시 생성 (드문 케이스)
        return Instantiate(hitEffectPrefab, transform);
    }

    private IEnumerator ReturnToPoolAfter(ParticleSystem effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.gameObject.SetActive(false);
        _pool.Enqueue(effect);
    }
}
