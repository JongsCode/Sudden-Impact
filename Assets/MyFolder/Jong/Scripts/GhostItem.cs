using UnityEngine;

public class GhostItem : MonoBehaviour
{
    public FieldofView fow;

    public void CheckGhostItem()
    {
        if (fow == null)
        {
            fow = FindFirstObjectByType<FieldofView>();
        }
       
        if(fow.CheckVisible(transform))
        {
            Destroy(gameObject);
        }
        else
        {
            fow.RegisterGhostItems(this);
        }
       
    }

    public void DeleteItem()
    {
        Destroy(gameObject);
    }
}

