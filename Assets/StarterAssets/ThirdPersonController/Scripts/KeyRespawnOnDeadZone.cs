using UnityEngine;

namespace StarterAssets
{
    [DisallowMultipleComponent]
    public class KeyRespawnOnDeadZone : MonoBehaviour
    {
        [Tooltip("Tag used by dead zone colliders.")]
        public string DeadZoneTag = "DeadZone";

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null && other.CompareTag(DeadZoneTag))
            {
                RespawnToInitialTransform();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision != null && collision.collider != null && collision.collider.CompareTag(DeadZoneTag))
            {
                RespawnToInitialTransform();
            }
        }

        private void RespawnToInitialTransform()
        {
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.position = _initialPosition;
                _rigidbody.rotation = _initialRotation;
                _rigidbody.WakeUp();
                return;
            }

            transform.SetPositionAndRotation(_initialPosition, _initialRotation);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToAllKeysInScene()
        {
            GameObject[] keyObjects;
            try
            {
                keyObjects = GameObject.FindGameObjectsWithTag("Key");
            }
            catch
            {
                return;
            }

            for (int i = 0; i < keyObjects.Length; i++)
            {
                GameObject key = keyObjects[i];
                if (key == null || key.GetComponent<KeyRespawnOnDeadZone>() != null)
                {
                    continue;
                }

                key.AddComponent<KeyRespawnOnDeadZone>();
            }
        }
    }
}
