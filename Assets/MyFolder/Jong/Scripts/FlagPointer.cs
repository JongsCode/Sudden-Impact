using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlagPointer : MonoBehaviour
{
    //public static FlagPointer Instance;
    
    private GameObject allyTarget;
    private GameObject enemyTarget;
    public GameObject Team1BaseCamp;
    public GameObject Team2BaseCamp;

    private Camera mainCam;

    public RectTransform pointerAllyTr;
    public RectTransform pointerEnemyTr;
    
    public RectTransform etcAllyTr;
    public RectTransform etcEnemyTr;
    public Vector3 offsetEtc = new Vector3(0f, -10f, 0f);
    
    public Image imageAllyArrow;
    public Image imageAllyFlag;
    public Image imageEnemyArrow;
    public Image imageEnemyFlag;

    public TextMeshProUGUI textAllyDis;
    public TextMeshProUGUI textEnemyDis;
    public float etcOffsetDistance = 60f;

    public float border = 50f;
    public float hideDistance = 5f;
    
    private bool hasAllyFlag = false;
    public bool HasAllyFlag
    {
        set { hasAllyFlag = value; }
        get { return hasAllyFlag; }
    }

    private bool hasEnemyFlag = false;
    public bool HasEnemyFlag
    {
        set { hasEnemyFlag = value; }
        get { return hasEnemyFlag; }
    }


    private void Awake()
    {
        //if(Instance == null)
        //{
        //    Instance = this;
        //}
        
        mainCam = Camera.main;
        if (imageAllyFlag != null)
            imageAllyFlag.enabled = false;
        if (imageAllyArrow != null)
            imageAllyArrow.enabled = false;
        if (imageEnemyFlag != null)
            imageEnemyFlag.enabled = false;
        if (textAllyDis != null)
            textAllyDis.enabled = false;
        if (textEnemyDis != null)
            textEnemyDis.enabled = false;
    }

    private void Start()
    {
        if(imageEnemyArrow != null)
        {
            imageEnemyArrow.enabled = true;
            // 목표 설정
        }
    }

    private void Update()
    {
        DisplayPointerToAllyTarget(allyTarget);
        DisplayPointerToEnemyTarget(enemyTarget);

        // FlagToGoal(hasFlag);
    }

    public void UpdateAllyObject(GameObject _target)
    {
        allyTarget = _target;
    }
    public void UpdateEnemyObject(GameObject _target)
    {
        enemyTarget = _target;
    }

    private void DisplayPointerToAllyTarget(GameObject _target)
    {
        if (_target == null)
        {
            if (imageAllyArrow != null) imageAllyArrow.enabled = false;
            if (imageAllyFlag != null) imageAllyFlag.enabled = false;
            if (textAllyDis != null) textAllyDis.enabled = false;
            return;
        }

        Vector3 targetScreenPosition = mainCam.WorldToScreenPoint(_target.transform.position);
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0f);

        Vector3 dir = (targetScreenPosition - screenCenter).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        pointerAllyTr.localEulerAngles = new Vector3(0f, 0f, angle + 90f);

        bool isOutScreen = targetScreenPosition.x <= border || targetScreenPosition.x >= Screen.width - border ||
                           targetScreenPosition.y <= border || targetScreenPosition.y >= Screen.height - border ||
                           targetScreenPosition.z < 0;
        Vector3 targetWorldPos = _target.transform.position;
        Vector3 cameraWorldPos = mainCam.transform.position;

        targetWorldPos.y = 0f;
        cameraWorldPos.y = 0f;

        float distance = Vector3.Distance(targetWorldPos, cameraWorldPos);
        textAllyDis.text = Mathf.RoundToInt(distance).ToString() + "M";

        if (isOutScreen)
        {
            imageAllyArrow.enabled = true;
            imageAllyFlag.enabled = hasAllyFlag;

            Vector3 centerToTarget = targetScreenPosition - screenCenter;

            float limitX = (Screen.width / 2f) - border;
            float limitY = (Screen.height / 2f) - border;

            float ratioX = centerToTarget.x != 0 ? Mathf.Abs(limitX / centerToTarget.x) : float.MaxValue;
            float ratioY = centerToTarget.y != 0 ? Mathf.Abs(limitY / centerToTarget.y) : float.MaxValue;

            float minRatio = Mathf.Min(ratioX, ratioY);

            Vector3 finalPosition = screenCenter + (centerToTarget * minRatio);
            finalPosition.z = 0f;

            pointerAllyTr.position = finalPosition;
            etcAllyTr.position = finalPosition - (dir * etcOffsetDistance);
            textAllyDis.enabled = true;
        }
        else
        {
            pointerAllyTr.position = targetScreenPosition;
            etcAllyTr.position = targetScreenPosition - (dir * etcOffsetDistance);

           

            if (distance < hideDistance)
            {
                imageAllyArrow.enabled = false;
                imageAllyFlag.enabled = false;
                textAllyDis.enabled = false;
            }
            else
            {
                imageAllyArrow.enabled = true;
                imageAllyFlag.enabled = hasAllyFlag;
                textAllyDis.enabled = true;
            }
        }
    }
    private void DisplayPointerToEnemyTarget(GameObject _target)
    {
        if (_target == null)
        {
            if (imageEnemyArrow != null) imageEnemyArrow.enabled = false;
            if (imageEnemyFlag != null) imageEnemyFlag.enabled = false;
            if (textEnemyDis != null) textEnemyDis.enabled = false;
            return;
        }

        Vector3 targetScreenPosition = mainCam.WorldToScreenPoint(_target.transform.position);
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0f);

        Vector3 dir = (targetScreenPosition - screenCenter).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        pointerEnemyTr.localEulerAngles = new Vector3(0f, 0f, angle + 90f);

        bool isOutScreen = targetScreenPosition.x <= border || targetScreenPosition.x >= Screen.width - border ||
                           targetScreenPosition.y <= border || targetScreenPosition.y >= Screen.height - border ||
                           targetScreenPosition.z < 0;
        Vector3 targetWorldPos = _target.transform.position;
        Vector3 cameraWorldPos = mainCam.transform.position;

        targetWorldPos.y = 0f;
        cameraWorldPos.y = 0f;

        float distance = Vector3.Distance(targetWorldPos, cameraWorldPos);
        textEnemyDis.text = Mathf.RoundToInt(distance).ToString() + "M";

        if (isOutScreen)
        {
            imageEnemyArrow.enabled = true;
            imageEnemyFlag.enabled = hasEnemyFlag;

            Vector3 centerToTarget = targetScreenPosition - screenCenter;

            float limitX = (Screen.width / 2f) - border;
            float limitY = (Screen.height / 2f) - border;

            float ratioX = centerToTarget.x != 0 ? Mathf.Abs(limitX / centerToTarget.x) : float.MaxValue;
            float ratioY = centerToTarget.y != 0 ? Mathf.Abs(limitY / centerToTarget.y) : float.MaxValue;

            float minRatio = Mathf.Min(ratioX, ratioY);

            Vector3 finalPosition = screenCenter + (centerToTarget * minRatio);
            finalPosition.z = 0f;

            pointerEnemyTr.position = finalPosition;
            etcEnemyTr.position = finalPosition - (dir * etcOffsetDistance);
            textEnemyDis.enabled = true;
        }
        else
        {
            pointerEnemyTr.position = targetScreenPosition;
            etcEnemyTr.position = targetScreenPosition - (dir * etcOffsetDistance);



            if (distance < hideDistance)
            {
                imageEnemyArrow.enabled = false;
                imageEnemyFlag.enabled = false;
                textEnemyDis.enabled = false;
            }
            else
            {
                imageEnemyArrow.enabled = true;
                imageEnemyFlag.enabled = hasEnemyFlag;
                textEnemyDis.enabled = true;
            }
        }
    }

    public void SetBaseCamp(int _teamNumber)
    {
        if (_teamNumber == 1)
        {
            enemyTarget = Team1BaseCamp;
        }
        if(_teamNumber == 2)
        {
            enemyTarget = Team2BaseCamp;
        }
    }

   


}