using UnityEngine;

public class PlayCrumble : MonoBehaviour
{
    public GameObject originalObject;
    public GameObject fracturedPrefab;
    public GameObject dustParticlePrefab;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject fractured = Instantiate(fracturedPrefab, originalObject.transform.position, originalObject.transform.rotation);

            // 부서진 가구에 방향을 전달하며 터뜨립니다.
            fractured.GetComponent<Crumble>().SetCrumble();

            Destroy(originalObject);
        }
    }
}
