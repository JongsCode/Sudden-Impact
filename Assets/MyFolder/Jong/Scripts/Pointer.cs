using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Pointer : MonoBehaviour
{
    public static Pointer Instance;
    
    public GameObject flagObject;
    public GameObject goalObject;
    private GameObject targetObject;
    private Camera mainCam;

    public RectTransform pointerTransform;
    public RectTransform etcTransform;
    public Vector3 offsetEtc = new Vector3(0f, -10f, 0f);
    public Image imageArrow;
    public Image imageFlag;
    public TextMeshProUGUI textDistance;

    public float border = 50f;
    public float hideDistance = 5f;
    private bool hasFlag = false;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        
        mainCam = Camera.main;
        
        if (imageArrow != null)
        {
            imageArrow.enabled = true;
            targetObject = flagObject;
        }
        if (imageFlag != null)
        {
            imageFlag.enabled = false;
        }
        
        if (textDistance != null)
        {
            textDistance.enabled = false;
        }
    }

    private void Update()
    {
        DisplayPointerToTarget(targetObject);
        FlagToGoal(hasFlag);
    }

    private void DisplayPointerToTarget(GameObject _target)
    {
        if (_target == null) return;

        Vector3 targetScreenPosition = mainCam.WorldToScreenPoint(_target.transform.position);
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0f);

        Vector3 dir = (targetScreenPosition - screenCenter).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        pointerTransform.localEulerAngles = new Vector3(0f, 0f, angle + 90f);

        bool isOutScreen = targetScreenPosition.x <= border || targetScreenPosition.x >= Screen.width - border ||
                           targetScreenPosition.y <= border || targetScreenPosition.y >= Screen.height - border ||
                           targetScreenPosition.z < 0;

        if (isOutScreen)
        {
            imageArrow.enabled = true;
            Vector3 outScreenPosition = targetScreenPosition;
            outScreenPosition.x = Mathf.Clamp(outScreenPosition.x, border, Screen.width - border);
            outScreenPosition.y = Mathf.Clamp(outScreenPosition.y, border, Screen.height - border);
            outScreenPosition.z = 0f;
            pointerTransform.position = outScreenPosition;
            etcTransform.position = outScreenPosition - offsetEtc;
        }
        else
        {
            pointerTransform.position = targetScreenPosition;
            etcTransform.position = targetScreenPosition - offsetEtc;

            Vector3 targetWorldPos = _target.transform.position;
            Vector3 cameraWorldPos = mainCam.transform.position;

            targetWorldPos.y = 0f;
            cameraWorldPos.y = 0f;

            if (Vector3.Distance(targetWorldPos, cameraWorldPos) < hideDistance)
            {
                imageArrow.enabled = false;
                imageFlag.enabled = false;
                textDistance.enabled = false;
            }
            else
            {
                imageArrow.enabled = true;
                if (hasFlag == true)
                {
                    imageFlag.enabled = true;
                    textDistance.enabled = true;
                }
            }
        }
    }

    public void FlagState(bool _hasFlag)
    {
        if (imageFlag == null || imageArrow == null) return;
        if (_hasFlag)
        {
            imageArrow.enabled = true;
            imageFlag.enabled = true;
            textDistance.enabled = true;
            hasFlag = true;
            targetObject = goalObject;
            
        }
        else
        {
            imageArrow.enabled = true;
            imageFlag.enabled = false;
            textDistance.enabled = false;
            hasFlag = false;
            targetObject = flagObject;
        }
    }
    

    private void FlagToGoal(bool _hasFlag)
    {
        if (_hasFlag == false) return;
        Vector3 flagPosition = flagObject.transform.position;
        Vector3 goalPosition = goalObject.transform.position;
        flagPosition.y = 0f;
        goalPosition.y = 0f;
        int distance = Mathf.RoundToInt(Vector3.Distance(flagPosition, goalPosition));
        textDistance.text = distance.ToString() + "M";
    }
}