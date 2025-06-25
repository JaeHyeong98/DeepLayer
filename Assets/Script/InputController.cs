using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    private void Awake()
    {
        GSC.inputCon = this;
    }

    public Vector3 move
    {
        get; private set;
    }

    public Vector2 look
    {
        get; private set;
    }

    private void OnMove(InputValue value)
    {
        Vector2 val = value.Get<Vector2>();
        move = new Vector3(val.x, 0, val.y);
    }

    private void OnJump(InputValue value)
    {

    }

    private void OnLook(InputValue value)
    {
        look = value.Get<Vector2>();
    }
}
