using UnityEngine;

public class GhostItem : MonoBehaviour
{
    public FieldofView fow;
    private Collider col;
    public bool isPicked = false;
    private bool doublecheck = false;

    private void Start()
    {
        col = GetComponent<Collider>();

    }

    private void Update()
    {
        if (fow == null)
        {
            fow = FindFirstObjectByType<FieldofView>();
        }
       
        if(isPicked && !doublecheck)
        {
            PickItem();
            doublecheck = true;
        }
    }

    public void PickItem()
    {
        if (col != null) col.enabled = false;
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

