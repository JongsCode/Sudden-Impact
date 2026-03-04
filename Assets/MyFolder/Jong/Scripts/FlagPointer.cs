using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlagPointer : MonoBehaviour
{
    //public static FlagPointer Instance;
    
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
    public bool HasFlag
    {
        set { hasFlag = value; }
        get { return hasFlag; }
    }

    private void Awake()
    {
        //if(Instance == null)
        //{
        //    Instance = this;
        //}
        
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
       // FlagToGoal(hasFlag);
    }

    public void UpdateFlagObject(GameObject _target)
    {
        targetObject = _target;
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
            if (hasFlag)
            {
                imageFlag.enabled = true;
                textDistance.enabled = true;
            }

            // 1. 타겟이 화면 중심에서 얼마나 떨어져 있는지(방향과 거리)를 구합니다.
            Vector3 centerToTarget = targetScreenPosition - screenCenter;

            // 2. 화면의 절반 크기에서 테두리 여백(border)을 뺀 '실제 허용 공간'을 구합니다.
            float limitX = (Screen.width / 2f) - border;
            float limitY = (Screen.height / 2f) - border;

            // 3. X축과 Y축 중, 어느 쪽 테두리에 먼저 부딪히는지 '비율'을 계산합니다.
            // (0으로 나누는 오류를 방지하기 위해 0일 때는 무한대 값을 줍니다)
            float ratioX = centerToTarget.x != 0 ? Mathf.Abs(limitX / centerToTarget.x) : float.MaxValue;
            float ratioY = centerToTarget.y != 0 ? Mathf.Abs(limitY / centerToTarget.y) : float.MaxValue;

            // 4. 둘 중 더 빨리 테두리에 닿는 쪽(더 작은 비율)을 선택합니다.
            float minRatio = Mathf.Min(ratioX, ratioY);

            // 5. 중심점에서 그 비율(minRatio)만큼만 딱! 곱해서 이동시킵니다.
            // 이렇게 하면 각도(비율)가 전혀 찌그러지지 않고 테두리에 완벽하게 안착합니다!
            Vector3 finalPosition = screenCenter + (centerToTarget * minRatio);
            finalPosition.z = 0f;

            pointerTransform.position = finalPosition;
            etcTransform.position = finalPosition - offsetEtc;
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
                if (hasFlag == true)
                {
                    imageFlag.enabled = false;
                    textDistance.enabled = false;
                }
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
            //targetObject = goalObject;
            
        }
        else
        {
            imageArrow.enabled = true;
            imageFlag.enabled = false;
            textDistance.enabled = false;
            hasFlag = false;
            //targetObject = flagObject;
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