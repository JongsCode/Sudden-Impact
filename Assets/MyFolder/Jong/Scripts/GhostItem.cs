using UnityEngine;

public class GhostItem : MonoBehaviour
{
    public FieldofView fow;
    public void SetFOW(GameObject _gameObject)
    {
        fow = _gameObject.GetComponent<FieldofView>();
    }
    public void CheckGhostItem()
    {
        
       
        if(fow.CheckVisible(transform))
        {
            gameObject.SetActive(false);
        }
        else
        {
            fow.RegisterGhostItems(this);
        }
       
    }

    public void DeleteItem()
    {
        gameObject.SetActive(false);
    }
}

