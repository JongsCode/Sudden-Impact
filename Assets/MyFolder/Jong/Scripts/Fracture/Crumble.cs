using UnityEngine;
using System.Collections;

public class Crumble : MonoBehaviour
{
    [Header("파편 설정")]
    public float scatterForce = 15f;
    public float destroyDelay = 3f;

    public void SetCrumble()
    {
        // 일반 실행 대신, 시간차 공격(코루틴)을 실행하라고 명령합니다.
        StartCoroutine(CrumbleRoutine());

        Destroy(gameObject, destroyDelay);
    }

    // 시간차를 두고 조각들을 떨어뜨리는 마법의 공간
    private IEnumerator CrumbleRoutine()
    {
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rbs)
        {
            if (rb != null)
            {
                rb.isKinematic = false; // 조각 하나를 떨어뜨립니다.
                rb.AddForce(Random.insideUnitSphere * scatterForce, ForceMode.Impulse);
            }

           
            yield return null;
        }
    }
}
