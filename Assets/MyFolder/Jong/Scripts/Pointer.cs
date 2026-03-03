using UnityEngine;

public class Pointer : MonoBehaviour
{
    public GameObject target;
    public RectTransform pointerTransform;
    private Vector3 targetPosition;

    private void Awake()
    {
        
    }

    private void Update()
    {
        targetPosition = target.transform.position;
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0f);
        Vector3 dir = (targetPosition - screenCenter).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        pointerTransform.localEulerAngles = new Vector3(0f, 0f, angle);

        Vector3 targetPositionScreenPoint = Camera.main.WorldToScreenPoint(targetPosition);
        bool isOutScreen = targetPositionScreenPoint.x <= 0 || targetPositionScreenPoint.x >= Screen.width 
                         || targetPositionScreenPoint.y <= 0 || targetPositionScreenPoint.y >= Screen.height;
        Debug.Log("isOutScreen" + isOutScreen);
    }
}
