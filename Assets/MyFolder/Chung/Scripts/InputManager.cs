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
    private InputAction onSwapAction;
    private InputAction onInteractAction;

    private Camera myMainCamera;
    private Plane aimPlane;
    private Vector3 worldAimPosition;


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
        onSwapAction = myInputAction.FindAction("Swap");
        onInteractAction = myInputAction.FindAction("Interact");


        myPlayerRegistry.OnPlayerRegistered += GetmyPlayer;
        myMainCamera = Camera.main;

    }

    private void Update()
    {

    }

    private void SetConnectActionMap(bool _isConnect)
    {
        if (_isConnect)
            playerMap.Enable();
        else
            playerMap.Disable();
    }

    private IEnumerator GetInputValue()
    {
        while (player != null)
        {
            yield return new WaitForFixedUpdate();

            if (!playerMap.enabled) continue;

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
        Ray ray = myMainCamera.ScreenPointToRay(_mouseScreenPos);

        if (aimPlane.Raycast(ray, out float enter))
        {
            worldAimPosition = ray.GetPoint(enter);
        }

        player.RotatePlayer(worldAimPosition);
    }

    private void OnFire(InputAction.CallbackContext ctx)
    {
        player.TryAttack(worldAimPosition);
    }

    private void SetPlayerAction()
    {
        onSprintAction.performed += player.SprintStart;
        onSprintAction.canceled += player.SprintEnd;
        onRollAction.performed += player.TryRoll;
        onDropAction.performed += player.PickUpAndDrop;
        onSwapAction.performed += player.TrySwapWeapon;
        onInteractAction.performed += player.TryInteract;

        onFireAction.performed += OnFire;

    }

    private void GetmyPlayer(PlayerController _player)
    {
        player = _player;
        SetPlayerAction();

        player.StunCallback = SetConnectActionMap;

        aimPlane = new Plane(Vector3.up, new Vector3(0, player.transform.position.y, 0));

        StartCoroutine(GetInputValue());
    }
}
