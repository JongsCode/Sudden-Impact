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

        if (fow != null && fow.CheckVisible(this.transform))
        {
            // 시야 안: 고스트 불필요, 바로 콜라이더/레이어 정리
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            gameObject.layer = 0;
            return;
        }

        // 시야 밖: 콜라이더/레이어는 유지한 채로 고스트 먼저 등록
        // (등록 전에 콜라이더를 끄면 CheckGhostItem 레이캐스트가 문을 통과해
        //  IsVisible = true 로 오판하여 고스트가 즉시 꺼지는 버그 방지)
        if (ghostPrefab != null)
        {
            GameObject go = Instantiate(ghostPrefab, transform.position, transform.rotation);
            GhostItem ghostItem = go.GetComponent<GhostItem>();
            if (ghostItem != null)
            {
                ghostItem.SetFOW(fow.gameObject);
                ghostItem.CheckGhostItem();
            }
        }

        // 고스트 등록 완료 후 콜라이더/레이어 정리
        Collider col2 = GetComponent<Collider>();
        if (col2 != null) col2.enabled = false;
        gameObject.layer = 0;
    }

    //public void Broken()
    //{
    //    originObject.SetActive(false);
    //    brokenObject.SetActive(true);

    //    if (fow != null)
    //    {
    //        if (fow.CheckVisible(this.transform))
    //            return;
    //    }

    //    gameObject.layer = 0;
    //    if (ghostPrefab != null)

    //    {
    //        GameObject go = Instantiate(ghostPrefab, transform.position, transform.rotation);
    //        Debug.Log("GhostObject");
    //        GhostItem ghostItem = go.GetComponent<GhostItem>();
    //        if (ghostItem != null)
    //        {
    //            ghostItem.SetFOW(fow.gameObject);
    //            ghostItem.CheckGhostItem();
    //        }

    //    }
    //}
}
