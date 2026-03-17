using UnityEngine;

public class Crumble : MonoBehaviour
{
    // 조각들이 무너져 내린 뒤, 이 미세한 힘을 무작위 방향으로 주어 흐트러뜨립니다.
    public float minScatterForce = 5f;
    public float maxScatterForce = 15f;

    public float destroyDelay = 5f; // 조각들이 화면에 남아있는 시간

    // 이번에는 폭발 힘이나 반경을 입력받지 않습니다.
    public void SetCrumble()
    {
        foreach (Transform t in transform)
        {
            Rigidbody rb = t.GetComponent<Rigidbody>();

            if (rb != null)
            {
                // 1. [핵심] 조각들에게 "중력의 힘을 받아 무너져 내리게 해라"는 명령입니다.
                // 2단계에서 리지드바디를 달아줬으므로, 이 순간 조각들은 중력에 의해 아래로 떨어집니다.
                rb.isKinematic = false; // 혹시 움직이지 않게 고정해뒀다면, 고정을 풀어줍니다.

                //// 2. 흐트러뜨리기: 중력으로만 떨어지면 너무 일직선으로 무너집니다. 
                //// 아주 미세한 무작위 힘을 각 조각에 주어 자연스럽게 흐트러뜨립니다.
                //Vector3 randomScatterDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                //float randomForce = Random.Range(minScatterForce, maxScatterForce);
                //rb.AddForce(randomScatterDirection * randomForce, ForceMode.Impulse);
            }
        }
        Destroy(gameObject, destroyDelay);
    }
}
