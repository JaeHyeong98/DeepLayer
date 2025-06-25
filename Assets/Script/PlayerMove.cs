using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private GameObject camTarget;
    public float moveSpeed = 4f;
    public float rotateSpeed = 4f;

    private void Awake()
    {
        camTarget = transform.Find("CamTarget").gameObject;
    }

    public void Move()
    {
        if (GSC.inputCon.move != Vector3.zero)
        {
            transform.localRotation = new Quaternion(0, camTarget.transform.localRotation.y, 0, camTarget.transform.localRotation.w);
            camTarget.transform.localRotation = new Quaternion(camTarget.transform.localRotation.x, 0, 0, camTarget.transform.localRotation.w);
            transform.Translate(GSC.inputCon.move * moveSpeed * Time.deltaTime);
        }

    }
}
