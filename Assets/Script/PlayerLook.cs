using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    private GameObject camTarget;
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    private const float _threshold = 0.01f;
    public float TopClamp = 70.0f;
    public float BottomClamp = -10.0f;

    private void Awake()
    {
        camTarget = transform.Find("CamTarget").gameObject;
    }

    public void CameraRotation()
    {

        //Debug.Log("CamRotation start");
        // if there is an input and camera position is not fixed
        if (GSC.inputCon.look.sqrMagnitude >= _threshold)
        {
            float deltaTimeMultiplier = 1.0f;

            _cinemachineTargetYaw += Input.GetAxis("Mouse X") * deltaTimeMultiplier;
            _cinemachineTargetPitch += Input.GetAxis("Mouse Y") * deltaTimeMultiplier;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        camTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
    }


    private float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}
