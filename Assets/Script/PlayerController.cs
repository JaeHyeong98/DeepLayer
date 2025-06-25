using UnityEditor.Rendering;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerLook playerLook;
    private PlayerMove playerMove;

    private void Awake()
    {
        playerLook = GetComponent<PlayerLook>();
        playerMove = GetComponent<PlayerMove>();
    }

    private void Update()
    {
        if(GSC.inputCon.look != Vector2.zero)
            playerLook.CameraRotation();

        if (GSC.inputCon.move != Vector3.zero)
            playerMove.Move();
    }

}
