using UnityEngine;

namespace AnimeSouls
{
    /// <summary>MMO-style orbit camera: hold right mouse to rotate, wheel to zoom. Avoids clipping into terrain and props.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 11f;
        public float yaw;
        public float pitch = 24f;
        public float sensitivity = 3f;
        public float minDistance = 3f;
        public float maxDistance = 24f;

        public Camera Camera { get; private set; }
        Vector3 _pivot;
        float _shake;
        float _currentDistance;

        void Awake()
        {
            Camera = GetComponent<Camera>();
            _currentDistance = distance;
        }

        public void Shake(float amount) { _shake = Mathf.Max(_shake, amount); }

        public void SnapBehind()
        {
            if (target == null) return;
            yaw = target.eulerAngles.y;
            _pivot = target.position + Vector3.up * 1.7f;
        }

        void LateUpdate()
        {
            if (target == null) return;
            if (!HUD.BlocksGameplayInput)
            {
                if (Inp.Mouse(1))
                {
                    yaw += Inp.MouseX * sensitivity;
                    pitch = Mathf.Clamp(pitch - Inp.MouseY * sensitivity, -15f, 75f);
                }
                distance = Mathf.Clamp(distance - Inp.Wheel * 1.5f, minDistance, maxDistance);
            }

            float scale = 1f;
            var rig = target.GetComponentInChildren<CharacterRig>();
            if (rig != null) scale = Mathf.Max(1f, rig.CurrentScale);

            var desiredPivot = target.position + Vector3.up * 1.7f * scale;
            _pivot = Vector3.Lerp(_pivot == Vector3.zero ? desiredPivot : _pivot, desiredPivot, 1f - Mathf.Exp(-Time.deltaTime * 14f));

            var rot = Quaternion.Euler(pitch, yaw, 0);
            float wanted = distance * Mathf.Lerp(1f, scale, 0.6f);
            float allowed = wanted;
            if (Physics.SphereCast(_pivot, 0.35f, rot * Vector3.back, out var hit, wanted, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                allowed = Mathf.Max(minDistance * 0.5f, hit.distance - 0.2f);
            _currentDistance = allowed < _currentDistance ? allowed : Mathf.Lerp(_currentDistance, allowed, 1f - Mathf.Exp(-Time.deltaTime * 4f));

            var pos = _pivot + rot * Vector3.back * _currentDistance;
            float ground = World.GroundHeight(pos) + 0.6f;
            if (pos.y < ground) pos.y = ground;

            if (_shake > 0f)
            {
                pos += Random.insideUnitSphere * _shake * 0.5f;
                _shake = Mathf.MoveTowards(_shake, 0f, Time.deltaTime * 1.5f);
            }

            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(_pivot - pos);
        }
    }
}
