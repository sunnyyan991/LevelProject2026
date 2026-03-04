 using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        public enum MagnetPolarity
        {
            Positive,
            Negative
        }

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

        [Header("Magnet")]
        public MagnetPolarity CurrentPolarity = MagnetPolarity.Positive;

        [Tooltip("Magnetic force center. Leave empty to use player center.")]
        public Transform MagnetOrigin;

        [Tooltip("Positive polarity target layers.")]
        public LayerMask PositiveTargetLayers;

        [Tooltip("Negative polarity target layers.")]
        public LayerMask NegativeTargetLayers;

        [Tooltip("Effective radius of the magnetic force.")]
        public float MagnetRadius = 6.0f;

        [Tooltip("Force multiplier applied to magnetic targets.")]
        public float MagnetForce = 30.0f;

        [Tooltip("Looping particle played while casting with positive polarity.")]
        public ParticleSystem PositiveCastEffect;

        [Tooltip("Looping particle played while casting with negative polarity.")]
        public ParticleSystem NegativeCastEffect;

        [Tooltip("One-shot particle played when switching from positive to negative.")]
        public ParticleSystem PositiveToNegativeSwitchEffect;

        [Tooltip("One-shot particle played when switching from negative to positive.")]
        public ParticleSystem NegativeToPositiveSwitchEffect;

        [Header("Polarity Visual")]
        [Tooltip("Renderer on the model node whose material should change with polarity.")]
        public Renderer PolarityVisualRenderer;

        [Tooltip("Material slot index on the target renderer.")]
        public int PolarityMaterialIndex = 0;

        [Tooltip("Material used when polarity is positive.")]
        public Material PositivePolarityMaterial;

        [Tooltip("Material used when polarity is negative.")]
        public Material NegativePolarityMaterial;

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

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private int _respawnGraceFrames;
        private readonly Collider[] _magnetOverlapResults = new Collider[32];

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

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
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            
            _hasAnimator = TryGetComponent(out _animator);
            _hasAnimator = false;
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            if (PositiveTargetLayers.value == 0 && NegativeTargetLayers.value == 0)
            {
                int positiveLayer = LayerMask.NameToLayer("Positive");
                int negativeLayer = LayerMask.NameToLayer("Negative");
                if (positiveLayer >= 0)
                {
                    PositiveTargetLayers = LayerMask.GetMask("Positive");
                }

                if (negativeLayer >= 0)
                {
                    NegativeTargetLayers = LayerMask.GetMask("Negative");
                }
            }

            ApplyPolarityVisualMaterial();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            //Dash();
            HandleMagnetismInputAndVfx();
            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void FixedUpdate()
        {
            ApplyMagneticForces();
        }

        private void Dash()
        {
            if (_input.dash)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }

                //
                _input.dash = false;
            }
        }

        private void HandleMagnetismInputAndVfx()
        {
            if (_input.ConsumeTransformPressed())
            {
                TogglePolarity();
            }

            bool isCasting = IsCastPressed();
            UpdateCastEffects(isCasting);
        }

        private void TogglePolarity()
        {
            MagnetPolarity previousPolarity = CurrentPolarity;
            CurrentPolarity = CurrentPolarity == MagnetPolarity.Positive ? MagnetPolarity.Negative : MagnetPolarity.Positive;

            if (previousPolarity == MagnetPolarity.Positive && PositiveToNegativeSwitchEffect != null)
            {
                PositiveToNegativeSwitchEffect.Play();
            }
            else if (previousPolarity == MagnetPolarity.Negative && NegativeToPositiveSwitchEffect != null)
            {
                NegativeToPositiveSwitchEffect.Play();
            }

            ApplyPolarityVisualMaterial();
            UpdateCastEffects(IsCastPressed());
        }

        private void ApplyMagneticForces()
        {
            if (!IsCastPressed() || MagnetForce <= 0f || MagnetRadius <= 0f)
            {
                return;
            }

            Vector3 origin = GetMagnetOrigin();
            ApplyMagneticForcesForPolarity(origin, PositiveTargetLayers, MagnetPolarity.Positive);
            ApplyMagneticForcesForPolarity(origin, NegativeTargetLayers, MagnetPolarity.Negative);
        }

        private void ApplyMagneticForcesForPolarity(Vector3 origin, LayerMask targetLayers, MagnetPolarity targetPolarity)
        {
            if (targetLayers.value == 0)
            {
                return;
            }

            int overlapCount = Physics.OverlapSphereNonAlloc(
                origin,
                MagnetRadius,
                _magnetOverlapResults,
                targetLayers,
                QueryTriggerInteraction.Ignore
            );

            bool shouldAttract = CurrentPolarity != targetPolarity;
            for (int i = 0; i < overlapCount; i++)
            {
                Collider targetCollider = _magnetOverlapResults[i];
                if (targetCollider == null)
                {
                    continue;
                }

                Rigidbody targetBody = targetCollider.attachedRigidbody;
                if (targetBody == null || targetBody.isKinematic)
                {
                    continue;
                }

                Vector3 toTarget = targetBody.worldCenterOfMass - origin;
                float distance = toTarget.magnitude;
                if (distance <= 0.001f || distance > MagnetRadius)
                {
                    continue;
                }

                Vector3 direction = toTarget / distance;
                Vector3 forceDirection = shouldAttract ? -direction : direction;
                float falloff = 1f - Mathf.Clamp01(distance / MagnetRadius);
                targetBody.AddForce(forceDirection * (MagnetForce * falloff), ForceMode.Acceleration);
            }
        }

        private Vector3 GetMagnetOrigin()
        {
            if (MagnetOrigin != null)
            {
                return MagnetOrigin.position;
            }

            return transform.position + _controller.center;
        }

        private bool IsCastPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (IsCurrentDeviceMouse && Mouse.current != null)
            {
                return Mouse.current.leftButton.isPressed;
            }
#endif
            return _input.cast;
        }

        private void UpdateCastEffects(bool isCasting)
        {
            if (!isCasting)
            {
                StopCastEffects();
                return;
            }

            ParticleSystem activeEffect = CurrentPolarity == MagnetPolarity.Positive ? PositiveCastEffect : NegativeCastEffect;
            ParticleSystem inactiveEffect = CurrentPolarity == MagnetPolarity.Positive ? NegativeCastEffect : PositiveCastEffect;

            if (activeEffect != null && !activeEffect.isPlaying)
            {
                activeEffect.Play();
            }

            if (inactiveEffect != null && inactiveEffect.isPlaying)
            {
                inactiveEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void StopCastEffects()
        {
            if (PositiveCastEffect != null && PositiveCastEffect.isPlaying)
            {
                PositiveCastEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (NegativeCastEffect != null && NegativeCastEffect.isPlaying)
            {
                NegativeCastEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void ApplyPolarityVisualMaterial()
        {
            if (PolarityVisualRenderer == null)
            {
                return;
            }

            Material targetMaterial = CurrentPolarity == MagnetPolarity.Positive ? PositivePolarityMaterial : NegativePolarityMaterial;
            if (targetMaterial == null)
            {
                return;
            }

            Material[] materials = PolarityVisualRenderer.materials;
            if (materials == null || materials.Length == 0)
            {
                return;
            }

            int materialIndex = Mathf.Clamp(PolarityMaterialIndex, 0, materials.Length - 1);
            materials[materialIndex] = targetMaterial;
            PolarityVisualRenderer.materials = materials;
        }

        private void OnDisable()
        {
            StopCastEffects();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
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

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (_respawnGraceFrames > 0)
            {
                _respawnGraceFrames--;
                _verticalVelocity = 0f;
                _input.jump = false;
                return;
            }

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

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DeadZone"))
            {
                RespawnToStart();
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider.CompareTag("DeadZone"))
            {
                RespawnToStart();
            }
        }

        private void RespawnToStart()
        {
            Vector3 respawnPosition = _spawnPosition;
            TryGetGroundSnappedRespawnPosition(out respawnPosition);
            _controller.enabled = false;
            transform.SetPositionAndRotation(respawnPosition, _spawnRotation);
            _controller.enabled = true;

            _verticalVelocity = -2f;
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _input.jump = false;
            _respawnGraceFrames = 2;
        }

        private bool TryGetGroundSnappedRespawnPosition(out Vector3 result)
        {
            result = _spawnPosition;
            Vector3 rayOrigin = _spawnPosition + Vector3.up * 5f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, GroundLayers, QueryTriggerInteraction.Ignore))
            {
                float groundedY = hit.point.y - _controller.center.y + (_controller.height * 0.5f) + _controller.skinWidth;
                result = new Vector3(_spawnPosition.x, groundedY, _spawnPosition.z);
                return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);

            Gizmos.color = new Color(0.0f, 0.6f, 1.0f, 0.2f);
            Gizmos.DrawWireSphere(Application.isPlaying ? GetMagnetOrigin() : transform.position, MagnetRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
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
}