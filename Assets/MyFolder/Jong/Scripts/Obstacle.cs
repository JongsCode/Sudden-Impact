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
