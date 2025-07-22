using System.Collections;
using StarterAssets;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.InputSystem;

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */
[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif
public class PlayerController : NetworkBehaviour
{
    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    public CinemachineCamera virtualCam;

    public LayerMask layerMask;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    private PlayerInput _playerInput;
    private PlayerInfo _playerInfo;
    private Animator _animator;
    private CharacterController _controller;
    private StarterAssetsInputs _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;

    private bool isGathering;
    private bool _hasAnimator;
    private bool isInit;
    private Coroutine gatheringCo;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
        }
    }


    private void Awake()
    {
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        if (virtualCam == null)
        {
            virtualCam = transform.Find("PlayerFollowCamera").GetComponent<CinemachineCamera>();
        }
    }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        if (isInit) return;
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();
        _playerInput = GetComponent<PlayerInput>();
        _playerInfo = GetComponent<PlayerInfo>();

        AssignAnimationIDs();

        StarterAssetsInputs.OnActionKey += Gathering;

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;

        if (IsOwner)
        {
            virtualCam.Priority = 10; // ������ ���� Ȱ��ȭ
            _playerInput.enabled = true;
        }
        else
        {
            virtualCam.Priority = 0;  // �ٸ� �÷��̾�� ��ȿȭ
            _playerInput.enabled = false;
        }
        isInit = true;
    }

    private void Update()
    {
        if (IsOwner)
        {
            GroundedCheck();
            CheckObjectInFront();
            HandleLocalPlayerInput();
            JumpAndGravity();
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner || !IsSpawned) return;
        CameraRotation();
    }

    // NetworkBehaviour�� ��ӹ޾����Ƿ� OnNetworkSpawn�� ���
    public override void OnNetworkSpawn()
    {
        Init();
        // OnNetworkSpawn�� ��Ʈ��ũ ������Ʈ�� ������ �� ȣ��˴ϴ�.
        if (!IsOwner)
        {
            // �� ������Ʈ�� �����ڰ� �ƴ� ��� (�ٸ� �÷��̾��� ĳ����)
            // ���� �����ϴ� ��ũ��Ʈ�� ī�޶� ��Ȱ��ȭ�մϴ�.
            enabled = false; // �� ��ũ��Ʈ ��ü ��Ȱ��ȭ
            if (TryGetComponent<StarterAssetsInputs>(out var playerInput))
            {
                playerInput.enabled = false;
            }
            // ���� Cinemachine Virtual Camera�� �÷��̾� ������ ���ο� �ִٸ� ��Ȱ��ȭ
            if (virtualCam != null)
            {
                virtualCam.gameObject.SetActive(false); // ���� ī�޶� GameObject ��Ȱ��ȭ
            }
        }
        else
        {
            // �� ������Ʈ�� ���� �÷��̾��� �������� ���
            // ���⼭ �ʿ��� �ʱ�ȭ (��: Cinemachine ī�޶� Ÿ�� ����)�� �����մϴ�.
            if (virtualCam != null)
            {
                virtualCam.Follow = transform.Find("PlayerCameraRoot");
                virtualCam.LookAt = transform.Find("PlayerCameraRoot");
            }
        }
        base.OnNetworkSpawn(); // �θ� OnNetworkSpawn ȣ�� (�߿�)
    }

    private void HandleLocalPlayerInput()
    {
        if (_controller == null || _mainCamera == null) return;

        // ===== 1. �ӵ� �� �ִϸ��̼� ���� �� ��� =====
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }
        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // ===== 2. �̵� ���� �� ��ǥ ȸ�� �� ��� (ī�޶� ����) =====
        Vector2 inputMove = _input.move;
        Vector3 finalMoveDirection = Vector3.zero;
        float targetRotationY = CinemachineCameraTarget.transform.eulerAngles.y;

        if (inputMove != Vector2.zero)
        {
            Vector3 camForward = _mainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = _mainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 moveDirWorld = camForward * inputMove.y + camRight * inputMove.x;

            // **���⼭ �߿�: moveDirWorld�� 0 ���Ͱ� �� �� �ִ� ��Ȳ�� ����**
            if (moveDirWorld.sqrMagnitude > 0.001f) // ���� ���� ������ ũ�� ��ȿ�� �������� ����
            {
                finalMoveDirection = moveDirWorld.normalized;
            }
            else
            {
                finalMoveDirection = Vector3.zero; // ��ȿ�� ������ �ƴϸ� �������� ����
            }

            // ĳ���� ȸ�� ��ǥ�� ���
            _targetRotation = Mathf.Atan2(finalMoveDirection.x, finalMoveDirection.z) * Mathf.Rad2Deg;
            targetRotationY = _targetRotation;
        }

        // ===== 3. ���ÿ��� ��� ĳ���� ȸ�� �� �̵� (Ŭ���̾�Ʈ ����) =====
        if (finalMoveDirection != Vector3.zero) // �̵� �Է��� ���� ���� ȸ�� ����
        {
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotationY, ref _rotationVelocity, RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        // CharacterController.Move�� FixedUpdate���� �ϴ°� ���� ������ �� �� ������,
        // Starter Assets�� Update���� �ϴ� ��쵵 �����Ƿ� �ϴ� Update ����
        Vector3 move = finalMoveDirection * (_speed * Time.deltaTime) +
                       new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;

        

        _controller.Move(move);

        // ===== 4. ������ ��û ���� (Ŭ���̾�Ʈ�� ���� ���¸� �˸�) =====
        // ������ �� ������ �޾� �����ϰ� �ڽ��� ĳ���͸� �����Դϴ�.
        RequestCharacterMoveServerRpc(transform.position, transform.rotation, finalMoveDirection, _speed, _verticalVelocity, _animationBlend, inputMagnitude, _controller.isGrounded, _input.jump);
    }


    // �������� ���� ĳ���� �̵� �� ȸ�� ������ �����ϴ� RPC
    [ServerRpc]
    private void RequestCharacterMoveServerRpc(
        Vector3 clientPredictedPosition, 
        Quaternion clientPredictedRotation, 
        Vector3 moveDirection, 
        float speed, 
        float verticalVelocity, 
        float animationBlend, 
        float inputMagnitude, 
        bool isGrounded, 
        bool jumpInput
        )
    {
        if (_controller == null || _animator == null) return;

        float serverTargetRotationY = transform.eulerAngles.y; // �⺻��
        if (moveDirection != Vector3.zero)
        {
            serverTargetRotationY = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
        }
        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, serverTargetRotationY, ref _rotationVelocity, RotationSmoothTime);
        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

        Vector3 move = moveDirection * (speed * Time.deltaTime) +
                       new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime;
        _controller.Move(move);

        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            //_animator.SetBool(_animIDJump, jumpInput && isGrounded);
            //_animator.SetBool(_animIDFreeFall, !isGrounded && verticalVelocity < 0.0f);
        }
    }
    
    private void GroundedCheck()
    {
        if (!IsOwner) return;
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void CameraRotation()
    {
        if (!IsOwner) return;
        // if there is an input and camera position is not fixed
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
            _input.jump = false;
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // if we are not grounded, do not jump
            _input.jump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private IEnumerator WaitGathering()
    {
        _animator.SetBool("Gathering", true);
        _input.interrupt = true;

        yield return new WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);

        _animator.SetBool("Gathering", false);
        _input.interrupt = false;
        gatheringCo = null;
        yield break;
    }

    private void Gathering()
    {
        if (!_hasAnimator || !IsOwner || !isGathering) return;
        
        if(gatheringCo == null)
        {
            gatheringCo = StartCoroutine(WaitGathering());
        }
    }

    public GameObject CheckObjectInFront()
    {
        RaycastHit hit;
        // Physics.Raycast(������, ����, out �浹 ����, �Ÿ�, ���̾� ����ũ)
        if (Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward, out hit, 7f, layerMask))
        {
            // ���̰� Ư�� ���̾��� ������Ʈ�� �¾Ҵٸ�
            Debug.Log("Object in front (Raycast): " + hit.collider.gameObject.name);
            isGathering = true;
            _playerInfo.GatheringKeyActive(true);
            return hit.collider.gameObject;
        }
        else
        {
            isGathering = false;
            _playerInfo.GatheringKeyActive(false);
            return null;
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (_controller == null) return;

        Gizmos.color = Grounded ? Color.green : Color.red; // Grounded ���¿� ���� ���� ����
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Gizmos.DrawWireSphere(spherePosition, GroundedRadius);
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }
    }
}