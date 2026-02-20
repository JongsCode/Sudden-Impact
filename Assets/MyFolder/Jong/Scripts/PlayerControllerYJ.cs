using UnityEngine;

public class PlayerControllerYJ : MonoBehaviour
{
    public float moveSpeed = 6f;
    private Rigidbody rigidbody;
    private Camera mainCam;
    Vector3 velocity;
    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        mainCam = Camera.main;

    }

    private void Update()
    {
        PlayerControl();
    }

    private void FixedUpdate()
    {
        rigidbody.MovePosition(rigidbody.position + velocity * Time.fixedDeltaTime);
    }

    private void PlayerControl()
    {
        Vector3 mousePos = mainCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCam.transform.position.y));
        Vector3 dir = mousePos - transform.position;
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
        velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized * moveSpeed;
    }
}
