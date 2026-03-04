using UnityEngine;
public class TempVision : MonoBehaviour
{
    public float lifeTime = 1.5f; // 파동이 퍼지는 시간

    private float timer = 0f;
    private Material myMaterial;

    private void Start()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            myMaterial = mr.material; 
        }
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float progress = timer / lifeTime;

        if (myMaterial != null)
        {
            myMaterial.SetFloat("_Progress", progress);
        }
    }
}