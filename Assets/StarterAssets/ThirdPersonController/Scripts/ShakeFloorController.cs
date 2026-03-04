using System.Collections;
using UnityEngine;

namespace StarterAssets
{
    public class ShakeFloorController : MonoBehaviour
    {
        [Header("Animator")]
        [Tooltip("Animator on the shake floor object.")]
        public Animator FloorAnimator;

        [Tooltip("Idle state name.")]
        public string FloorIdleStateName = "FloorIdle";

        [Tooltip("Down state name played when player steps on.")]
        public string FloorDownStateName = "FloorDown";

        [Tooltip("Up state name played after delay.")]
        public string FloorUpStateName = "FloorUp";

        [Header("Timing")]
        [Tooltip("Delay after FloorDown before playing FloorUp.")]
        public float WaitBeforeUpSeconds = 1f;

        [Tooltip("Allow re-trigger while sequence is playing.")]
        public bool AllowRetriggerWhilePlaying = false;

        [Tooltip("If true, the floor sequence can only be triggered once.")]
        public bool TriggerOnlyOnce = false;

        [Tooltip("Play FloorIdle after FloorUp finishes.")]
        public bool ReturnToIdleAfterUp = true;

        [Header("Detection")]
        [Tooltip("Optional player tag. Leave empty to use ThirdPersonController detection only.")]
        public string PlayerTag = "Player";

        [Tooltip("Fallback polling detection using collider bounds (recommended for CharacterController setups).")]
        public bool UseOverlapFallback = true;

        [Tooltip("Collider used for fallback overlap detection. If empty, uses this object collider.")]
        public Collider DetectionCollider;

        [Tooltip("Extra height added upward for overlap fallback detection.")]
        public float DetectionHeightPadding = 1.2f;

        private Coroutine _sequenceCoroutine;
        private readonly Collider[] _overlapResults = new Collider[16];
        private bool _wasPlayerDetectedLastFrame;
        private Collider _lastDetectedPlayerCollider;
        private bool _hasTriggered;

        private void Awake()
        {
            if (FloorAnimator == null)
            {
                FloorAnimator = GetComponentInParent<Animator>();
            }

            if (FloorAnimator != null && !string.IsNullOrEmpty(FloorIdleStateName))
            {
                FloorAnimator.Play(FloorIdleStateName, 0, 0f);
            }

            if (DetectionCollider == null)
            {
                DetectionCollider = GetComponent<Collider>();
            }
        }

        private void FixedUpdate()
        {
            if (!UseOverlapFallback || DetectionCollider == null)
            {
                return;
            }

            bool isPlayerDetected = IsPlayerInsideDetectionBounds();
            if (isPlayerDetected && !_wasPlayerDetectedLastFrame)
            {
                TryStartSequence(_lastDetectedPlayerCollider);
            }

            _wasPlayerDetectedLastFrame = isPlayerDetected;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryStartSequence(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null)
            {
                return;
            }
            TryStartSequence(collision.collider);
        }

        private void TryStartSequence(Collider other)
        {
            if (other == null || FloorAnimator == null)
            {
                return;
            }

            if (TriggerOnlyOnce && _hasTriggered)
            {
                return;
            }

            if (!IsPlayerCollider(other))
            {
                return;
            }

            if (_sequenceCoroutine != null && !AllowRetriggerWhilePlaying)
            {
                return;
            }

            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
            }

            _hasTriggered = true;
            _sequenceCoroutine = StartCoroutine(PlayDownUpSequence());
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (!string.IsNullOrEmpty(PlayerTag) && other.CompareTag(PlayerTag))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(PlayerTag) &&
                other.attachedRigidbody != null &&
                other.attachedRigidbody.CompareTag(PlayerTag))
            {
                return true;
            }

            return other.GetComponentInParent<ThirdPersonController>() != null;
        }

        private bool IsPlayerInsideDetectionBounds()
        {
            _lastDetectedPlayerCollider = null;
            Bounds bounds = DetectionCollider.bounds;
            float upwardPadding = Mathf.Max(0f, DetectionHeightPadding);
            Vector3 center = bounds.center + Vector3.up * (upwardPadding * 0.5f);
            Vector3 halfExtents = bounds.extents + new Vector3(0.02f, 0.02f + upwardPadding * 0.5f, 0.02f);
            Quaternion orientation = DetectionCollider.transform.rotation;
            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                _overlapResults,
                orientation,
                ~0,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _overlapResults[i];
                if (hit == null || hit == DetectionCollider)
                {
                    continue;
                }

                if (IsPlayerCollider(hit))
                {
                    _lastDetectedPlayerCollider = hit;
                    return true;
                }
            }

            return false;
        }

        private IEnumerator PlayDownUpSequence()
        {
            if (!string.IsNullOrEmpty(FloorDownStateName))
            {
                FloorAnimator.Play(FloorDownStateName, 0, 0f);
            }

            if (WaitBeforeUpSeconds > 0f)
            {
                yield return new WaitForSeconds(WaitBeforeUpSeconds);
            }
            else
            {
                yield return null;
            }

            if (!string.IsNullOrEmpty(FloorUpStateName))
            {
                FloorAnimator.Play(FloorUpStateName, 0, 0f);

                if (ReturnToIdleAfterUp && !string.IsNullOrEmpty(FloorIdleStateName))
                {
                    float floorUpLength = GetClipLength(FloorUpStateName);
                    if (floorUpLength > 0f)
                    {
                        yield return new WaitForSeconds(floorUpLength);
                    }
                    else
                    {
                        yield return null;
                    }

                    FloorAnimator.Play(FloorIdleStateName, 0, 0f);
                }
            }

            if (!TriggerOnlyOnce)
            {
                _hasTriggered = false;
            }

            _sequenceCoroutine = null;
        }

        private float GetClipLength(string clipName)
        {
            if (FloorAnimator == null || FloorAnimator.runtimeAnimatorController == null || string.IsNullOrEmpty(clipName))
            {
                return 0f;
            }

            AnimationClip[] clips = FloorAnimator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip != null && clip.name == clipName)
                {
                    return clip.length;
                }
            }

            return 0f;
        }
    }
}
