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
            virtualCam.Priority = 10; // 나만의 시점 활성화
            _playerInput.enabled = true;
        }
        else
        {
            virtualCam.Priority = 0;  // 다른 플레이어는 무효화
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

    // NetworkBehaviour를 상속받았으므로 OnNetworkSpawn을 사용
    public override void OnNetworkSpawn()
    {
        Init();
        // OnNetworkSpawn은 네트워크 오브젝트가 생성될 때 호출됩니다.
        if (!IsOwner)
        {
            // 이 오브젝트의 소유자가 아닌 경우 (다른 플레이어의 캐릭터)
            // 직접 제어하는 스크립트와 카메라를 비활성화합니다.
            enabled = false; // 이 스크립트 자체 비활성화
            if (TryGetComponent<StarterAssetsInputs>(out var playerInput))
            {
                playerInput.enabled = false;
            }
            // 만약 Cinemachine Virtual Camera가 플레이어 프리팹 내부에 있다면 비활성화
            if (virtualCam != null)
            {
                virtualCam.gameObject.SetActive(false); // 가상 카메라 GameObject 비활성화
            }
        }
        else
        {
            // 이 오브젝트가 로컬 플레이어의 소유자인 경우
            // 여기서 필요한 초기화 (예: Cinemachine 카메라 타겟 설정)를 수행합니다.
            if (virtualCam != null)
            {
                virtualCam.Follow = transform.Find("PlayerCameraRoot");
                virtualCam.LookAt = transform.Find("PlayerCameraRoot");
            }
        }
        base.OnNetworkSpawn(); // 부모 OnNetworkSpawn 호출 (중요)
    }

    private void HandleLocalPlayerInput()
    {
        if (_controller == null || _mainCamera == null) return;

        // ===== 1. 속도 및 애니메이션 블렌딩 값 계산 =====
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

        // ===== 2. 이동 방향 및 목표 회전 값 계산 (카메라 기준) =====
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

            // **여기서 중요: moveDirWorld가 0 벡터가 될 수 있는 상황을 방지**
            if (moveDirWorld.sqrMagnitude > 0.001f) // 아주 작은 값보다 크면 유효한 방향으로 간주
            {
                finalMoveDirection = moveDirWorld.normalized;
            }
            else
            {
                finalMoveDirection = Vector3.zero; // 유효한 방향이 아니면 움직이지 않음
            }

            // 캐릭터 회전 목표값 계산
            _targetRotation = Mathf.Atan2(finalMoveDirection.x, finalMoveDirection.z) * Mathf.Rad2Deg;
            targetRotationY = _targetRotation;
        }

        // ===== 3. 로컬에서 즉시 캐릭터 회전 및 이동 (클라이언트 예측) =====
        if (finalMoveDirection != Vector3.zero) // 이동 입력이 있을 때만 회전 적용
        {
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotationY, ref _rotationVelocity, RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        // CharacterController.Move는 FixedUpdate에서 하는게 물리 엔진과 더 잘 맞지만,
        // Starter Assets은 Update에서 하는 경우도 많으므로 일단 Update 유지
        Vector3 move = finalMoveDirection * (_speed * Time.deltaTime) +
                       new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;

        

        _controller.Move(move);

        // ===== 4. 서버로 요청 전송 (클라이언트의 현재 상태를 알림) =====
        // 서버는 이 정보를 받아 검증하고 자신의 캐릭터를 움직입니다.
        RequestCharacterMoveServerRpc(transform.position, transform.rotation, finalMoveDirection, _speed, _verticalVelocity, _animationBlend, inputMagnitude, _controller.isGrounded, _input.jump);
    }


    // 서버에서 실제 캐릭터 이동 및 회전 로직을 수행하는 RPC
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

        float serverTargetRotationY = transform.eulerAngles.y; // 기본값
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
        // Physics.Raycast(시작점, 방향, out 충돌 정보, 거리, 레이어 마스크)
        if (Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward, out hit, 7f, layerMask))
        {
            // 레이가 특정 레이어의 오브젝트에 맞았다면
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

        Gizmos.color = Grounded ? Color.green : Color.red; // Grounded 상태에 따라 색상 변경
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