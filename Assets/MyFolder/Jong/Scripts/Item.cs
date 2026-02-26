using UnityEngine;

public class Item : MonoBehaviour
{
    public GameObject ghostPrefab;
    public Shader equipShader;
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
        MeshRenderer[] mrs = GetComponentsInChildren<MeshRenderer>();
        foreach(MeshRenderer mr in mrs)
        {
            Material[] mats = mr.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null)
                {
                    mats[i].shader = equipShader;
                }
            }
        }
        
    }
}
