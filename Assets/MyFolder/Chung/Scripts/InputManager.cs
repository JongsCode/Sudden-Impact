using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset myInputAction;
    [SerializeField] private PlayerRegistry myPlayerRegistry;

    [Header("ForDebug")]
    [SerializeField] private PlayerController player;

    private InputActionMap playerMap;

    private InputAction onSprintAction;
    private InputAction onMoveAction;
    private InputAction onMousePosAction;
    private InputAction onRollAction;
    private InputAction onDropAction;
    private InputAction onFireAction;


    private void Awake()
    {
        playerMap = myInputAction.FindActionMap("Player");
        playerMap.Enable();

        onSprintAction = myInputAction.FindAction("Sprint");
        onMousePosAction = myInputAction.FindAction("MousePosition");
        onMoveAction = myInputAction.FindAction("Move");
        onRollAction = myInputAction.FindAction("Roll");
        onDropAction = myInputAction.FindAction("Drop");
        onFireAction = myInputAction.FindAction("Fire");


        myPlayerRegistry.OnPlayerRegistered += GetmyPlayer;
    }

    private void Update()
    {

    }


    private IEnumerator GetInputValue()
    {
        while (player)
        {
            yield return null;
            OnMove(onMoveAction.ReadValue<Vector2>());
            OnRotate(onMousePosAction.ReadValue<Vector2>());
        }

    }

    private void OnMove(Vector2 _moveAxis)
    {
        player.MovePlayer(new Vector3(_moveAxis.x, 0f, _moveAxis.y));
    }

    private void OnRotate(Vector2 _mouseScreenPos)
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 direction = _mouseScreenPos - screenCenter;
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        player.RotatePlayer(angle);
    }

    private void OnFire(InputAction.CallbackContext ctx)
    {
        player.Fire(onMousePosAction.ReadValue<Vector2>());
    }

    private void SetPlayerAction()
    {
        onSprintAction.performed += player.SprintStart;
        onSprintAction.canceled += player.SprintEnd;
        onRollAction.performed += player.TryRoll;
        onDropAction.performed += player.PickUpAndDrop;

        onFireAction.performed += OnFire;

    }

    private void GetmyPlayer(PlayerController _player)
    {
        player = _player;
        SetPlayerAction();


        StartCoroutine(GetInputValue());
    }
}
