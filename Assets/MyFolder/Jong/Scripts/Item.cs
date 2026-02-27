using Photon.Pun;
using UnityEngine;

public class Item : MonoBehaviour
{
    public GameObject ghostPrefab;
    public Shader equipShader;
    public Shader itemShader;
    public Shader originShader;
    public FieldofView fow;
    public GameObject ghostObject;
    public GameObject doorObject;
    private MeshRenderer[] mrs;
    private bool wasVisible = true;
    private void Awake()
    {
        mrs = GetComponentsInChildren<MeshRenderer>();

    }
    
    public void CheckVisible()
    {
        if (fow == null)
        {
            fow = FindFirstObjectByType<FieldofView>();
        }

        bool isVisible = fow.CheckVisible(transform);
        if (isVisible != wasVisible)
        {
            wasVisible = isVisible;
            Debug.Log("wasVisible : " + wasVisible);
            if (wasVisible)
            {
                SetRender(true);
                SetGhostItem(false);
            }
            else
            {
                SetRender(false);
                SetGhostItem(true);
            }
        }
    }      
    public void PickItem()
    {
        if (ghostPrefab == null) return;
        
        GameObject go = Instantiate(ghostPrefab, transform.position, transform.localRotation);
        Debug.Log("GhostObject");
        GhostItem ghostItem = go.GetComponent<GhostItem>();
        if(ghostItem != null)
        {
            ghostItem.CheckGhostItem();
        }
        SetShader(equipShader);
        
        
    }

    public void MakeGhostItem(Vector3 _position)
    {
        if (ghostPrefab == null) return;
        
        GameObject go = Instantiate(ghostPrefab, _position, transform.localRotation);
        GhostItem ghostItem = go.GetComponent<GhostItem>();
        if(ghostItem !=  null)
        {
            ghostItem.CheckGhostItem();
        }
    }

    private void SetGhostItem(bool _isActive)
    {
        if (ghostObject == null) return;
        if (_isActive == true)
        {
            ghostObject.gameObject.transform.position = doorObject.transform.position;
            ghostObject.gameObject.transform.rotation = doorObject.transform.rotation;
        }
        if (ghostObject.gameObject.activeSelf == _isActive) return;
        ghostObject.gameObject.SetActive(_isActive); 
        
    }
    private void SetRender(bool _isVisible)
    {
        foreach (MeshRenderer mr in mrs)
        {
            mr.enabled = _isVisible;
        }
    }
    public void SetShader(Shader _shader)
    {
        MeshRenderer[] mrs = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in mrs)
        {
            Material[] mats = mr.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null)
                {
                    mats[i].shader = _shader;
                }
            }
        }
    }
}
