using UnityEngine;
using System.Collections;

namespace StarterAssets
{
    public class DoorKeyUnlocker : MonoBehaviour
    {
        [Header("Door")]
        [Tooltip("Animator that controls the door states.")]
        public Animator DoorAnimator;

        [Tooltip("Closed state name in door animator.")]
        public string DoorCloseStateName = "DoorClose";

        [Tooltip("Opening state name in door animator.")]
        public string DoorOpeningStateName = "DoorOpening";

        [Tooltip("Opened state name in door animator.")]
        public string DoorOpenStateName = "DoorOpen";

        [Tooltip("If true, code will play DoorOpen after DoorOpening clip duration without using transitions.")]
        public bool PlayOpenStateByCode = true;

        [Header("Key")]
        [Tooltip("Objects with this tag are treated as keys.")]
        public string KeyTag = "Key";

        [Tooltip("Destroy key when consumed. If false, key will be deactivated.")]
        public bool DestroyKeyOnUse = true;

        [Tooltip("Disable this trigger after the door is unlocked.")]
        public bool DisableTriggerAfterUnlock = true;

        private bool _isUnlocked;
        private Coroutine _doorOpenCoroutine;

        private void Awake()
        {
            if (DoorAnimator == null)
            {
                DoorAnimator = GetComponentInParent<Animator>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryUnlockWithCollider(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null)
            {
                return;
            }

            TryUnlockWithCollider(collision.collider);
        }

        private void TryUnlockWithCollider(Collider other)
        {
            if (_isUnlocked || other == null)
            {
                return;
            }

            GameObject keyObject = ResolveKeyObject(other);
            if (keyObject == null)
            {
                return;
            }

            ConsumeKey(keyObject);
            UnlockDoor();
        }

        private GameObject ResolveKeyObject(Collider other)
        {
            if (other.CompareTag(KeyTag))
            {
                return other.gameObject;
            }

            if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(KeyTag))
            {
                return other.attachedRigidbody.gameObject;
            }

            Transform current = other.transform.parent;
            while (current != null)
            {
                if (current.CompareTag(KeyTag))
                {
                    return current.gameObject;
                }

                current = current.parent;
            }

            return null;
        }

        private void ConsumeKey(GameObject keyObject)
        {
            if (DestroyKeyOnUse)
            {
                Destroy(keyObject);
            }
            else
            {
                keyObject.SetActive(false);
            }
        }

        private void UnlockDoor()
        {
            _isUnlocked = true;

            if (DoorAnimator != null)
            {
                if (!string.IsNullOrEmpty(DoorOpeningStateName))
                {
                    DoorAnimator.Play(DoorOpeningStateName, 0, 0f);

                    if (PlayOpenStateByCode && !string.IsNullOrEmpty(DoorOpenStateName))
                    {
                        if (_doorOpenCoroutine != null)
                        {
                            StopCoroutine(_doorOpenCoroutine);
                        }

                        _doorOpenCoroutine = StartCoroutine(PlayOpenAfterOpening());
                    }
                }
                else if (!string.IsNullOrEmpty(DoorOpenStateName))
                {
                    DoorAnimator.Play(DoorOpenStateName, 0, 0f);
                }
            }

            if (DisableTriggerAfterUnlock)
            {
                Collider triggerCollider = GetComponent<Collider>();
                if (triggerCollider != null)
                {
                    triggerCollider.enabled = false;
                }
            }
        }

        private IEnumerator PlayOpenAfterOpening()
        {
            float waitSeconds = GetAnimationClipLength(DoorOpeningStateName);
            if (waitSeconds > 0f)
            {
                yield return new WaitForSeconds(waitSeconds);
            }
            else
            {
                yield return null;
            }

            if (DoorAnimator != null && !string.IsNullOrEmpty(DoorOpenStateName))
            {
                DoorAnimator.Play(DoorOpenStateName, 0, 0f);
            }

            _doorOpenCoroutine = null;
        }

        private float GetAnimationClipLength(string clipName)
        {
            if (DoorAnimator == null || DoorAnimator.runtimeAnimatorController == null || string.IsNullOrEmpty(clipName))
            {
                return 0f;
            }

            AnimationClip[] clips = DoorAnimator.runtimeAnimatorController.animationClips;
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
