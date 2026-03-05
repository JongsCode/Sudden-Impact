using UnityEngine;
using System.Collections.Generic;
public enum PopUpType
{
    SignUp
}
public class PopUpManager : MonoBehaviour
{
    public static PopUpManager Instance;
    public Transform canvas;
    public GameObject backgroundPanel;
    
    private Dictionary<PopUpType, GameObject> PopUpList = new Dictionary<PopUpType, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void Show(PopUpType _popUpType) // PopUpManager Instance에서 type에 해당하는 PopUp을 호출 및 생성(없는 경우)
    {
        backgroundPanel.SetActive(true);
        GameObject newPopUp = null;
        if(PopUpList.TryGetValue(_popUpType,out newPopUp))
        {
            newPopUp.SetActive(true);
        }
        else
        {
            newPopUp = Instantiate(Resources.Load<GameObject>(_popUpType.ToString()));
            if (newPopUp == null)
            {
                Debug.Log("PopUp 생성 실패!");
                return;
            }
            newPopUp.transform.SetParent(canvas.transform, false);
            PopUpList.Add(_popUpType, newPopUp);
        }
    }

    public void PanelOff() // Pop이 생성되면 뒷 배경이 지저분해 보일 수 있으므로 Panel로 가림
    {
        backgroundPanel.SetActive(false);
    }

}
