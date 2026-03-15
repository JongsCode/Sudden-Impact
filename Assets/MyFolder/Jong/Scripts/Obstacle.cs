using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public GameObject ghostPrefab;
    public GameObject brokenObject;
    public GameObject originObject;
    public FieldofView fow;
    
    private void Awake()
    {
        originObject.SetActive(true);
        brokenObject.SetActive(false);
    }
    public void SetFOW(GameObject _gameObject)
    {
        fow = _gameObject.GetComponent<FieldofView>();
    }
    public void Broken()
    {
        originObject.SetActive(false);
        brokenObject.SetActive(true);

        // 비주얼이 자식으로 분리된 구조에서 루트 콜라이더가 남아
        // FOW 레이캐스트를 막아 고스트 감지 실패하는 문제 수정
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (fow != null)
        {
            if (fow.CheckVisible(this.transform))
                return;
        }

        gameObject.layer = 0;
        if (ghostPrefab != null)

        {
            GameObject go = Instantiate(ghostPrefab, transform.position, transform.rotation);
            Debug.Log("GhostObject");
            GhostItem ghostItem = go.GetComponent<GhostItem>();
            if (ghostItem != null)
            {
                ghostItem.SetFOW(fow.gameObject);
                ghostItem.CheckGhostItem();
            }

        }
    }
}
