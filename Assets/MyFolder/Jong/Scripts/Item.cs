using UnityEngine;

public class Item : MonoBehaviour
{
    public GameObject ghostPrefab;
    public Shader equipShader;
    public Shader itemShader;
    public Shader originShader;
    public FieldofView fow;
    public GameObject ghostObject;
    public GameObject itemObject;
    public GameObject brokenObject;
    public GameObject originPivot;
    public GameObject ghostPivot;
    private MeshRenderer[] mrs;
    private bool isBroken = false;
    private bool seenBroken = false;
    private bool currentVisible = false;
    public bool IsBroken
    {
        set { isBroken = value; }
        get { return isBroken; }
    }
    private void Awake()
    {
        mrs = GetComponentsInChildren<MeshRenderer>();

    }
    private void Start()
    {
        SetVisible(false);
    }


    public void PickItem()
    {
        if (ghostPrefab == null) return;

        GameObject go = Instantiate(ghostPrefab, transform.position, transform.localRotation);
        Debug.Log("GhostObject");
        GhostItem ghostItem = go.GetComponent<GhostItem>();
        if (ghostItem != null)
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
        if (ghostItem != null)
        {
            ghostItem.CheckGhostItem();
        }
    }


    public void SetVisible(bool _isVisible)
    {
        currentVisible = _isVisible;

        if (_isVisible)
        {
            if (isBroken)
            {
                seenBroken = true;
            }
            SetRender(true);
            SetGhostItem(false);
        }
        else
        {
            SetRender(false);

            if (!isBroken || (isBroken && !seenBroken))
            {
                SetGhostItem(true);
            }
            else
            {
                SetGhostItem(false);
            }
        }

    }
    private void SetRender(bool _isVisible)
    {
        if (itemObject == null || brokenObject == null) return;
        if (!_isVisible)
        {
            itemObject.GetComponent<MeshRenderer>().enabled = false;
            brokenObject.GetComponent<MeshRenderer>().enabled = false;
            return;
        }

        if(isBroken)
        {
            itemObject.SetActive(false);
            brokenObject.SetActive(false);
        }
        else
        {
            itemObject.GetComponent<MeshRenderer>().enabled = true;
            brokenObject.GetComponent<MeshRenderer>().enabled = false;
        }
    }
    private void SetGhostItem(bool _isActive)
    {
        if (ghostObject == null) return;
        if (_isActive == true)
        {
            ghostPivot.gameObject.transform.position = originPivot.transform.position;
            ghostPivot.gameObject.transform.rotation = originPivot.transform.rotation;
        }
        if (ghostObject.gameObject.activeSelf == _isActive) return;
        ghostObject.gameObject.SetActive(_isActive);

    }
    public void SetShader(Shader _shader)
    {
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

    public void SetBrokenState(bool _isBroken)
    {
        isBroken = _isBroken;
        if(currentVisible)
        {
            seenBroken = true;
            SetRender(true);
        }
    }
}
