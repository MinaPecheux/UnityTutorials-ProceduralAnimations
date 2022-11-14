using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralAnimator : MonoBehaviour
{
    class ProceduralLimb
    {
        public Transform IKTarget;
        public Vector3 defaultPosition;
        public Vector3 lastPosition;
        public bool moving;
    }

    [Header("Global")]
    [SerializeField] private LayerMask _groundLayerMask = default;

    [Header("Steps")]
    [SerializeField] private Transform[] _limbTargets;
    [SerializeField] private float _stepSize = 1;
    [SerializeField] private float _stepHeight = 1;
    [SerializeField] private int _smoothness = 1;
    [SerializeField] private float _raycastRange = 2;
    [SerializeField] private float _feetOffset = 0;

    private int _nLimbs;
    private ProceduralLimb[] _limbs;

    private Vector3 _lastBodyPosition;
    private Vector3 _velocity;
    private bool _allLimbsResting;

    void Start()
    {
        _nLimbs = _limbTargets.Length;
        _limbs = new ProceduralLimb[_nLimbs];
        Transform t;
        for (int i = 0; i < _nLimbs; ++i)
        {
            t = _limbTargets[i];
            _limbs[i] = new ProceduralLimb()
            {
                IKTarget = t,
                defaultPosition = t.localPosition,
                lastPosition = t.position,
                moving = false
            };
        }

        _lastBodyPosition = transform.position;
        _allLimbsResting = true;
    }

    void FixedUpdate()
    {
        _velocity = transform.position - _lastBodyPosition;

        if (_velocity.magnitude > Mathf.Epsilon)
            _HandleMovement();
        else if (!_allLimbsResting)
            _BackToRestPosition();
    }

    private void _HandleMovement()
    {
        _lastBodyPosition = transform.position;

        Vector3[] desiredPositions = new Vector3[_nLimbs];
        float greatestDistance = _stepSize;
        int limbToMove = -1;

        for (int i = 0; i < _nLimbs; ++i)
        {
            if (_limbs[i].moving) continue; // limb already moving: can't move again!

            desiredPositions[i] = transform.TransformPoint(_limbs[i].defaultPosition);
            float dist = (desiredPositions[i] + _velocity - _limbs[i].lastPosition).magnitude;
            if (dist > greatestDistance)
            {
                greatestDistance = dist;
                limbToMove = i;
            }
        }

        for (int i = 0; i < _nLimbs; ++i)
            if (i != limbToMove)
                _limbs[i].IKTarget.position = _limbs[i].lastPosition;

        if (limbToMove != -1)
        {
            Vector3 targetOffset = desiredPositions[limbToMove] - _limbs[limbToMove].IKTarget.position;
            Vector3 targetPoint = desiredPositions[limbToMove] + _velocity.magnitude * targetOffset;
            targetPoint = _RaycastToGround(targetPoint, transform.up);
            targetPoint += transform.up * _feetOffset;

            _allLimbsResting = false;
            StartCoroutine(_Stepping(limbToMove, targetPoint));
        }
    }

    private void _BackToRestPosition()
    {
        Vector3 targetPoint; float dist;
        for (int i = 0; i < _nLimbs; ++i)
        {
            if (_limbs[i].moving) continue; // limb already moving: can't move again!

            targetPoint = _RaycastToGround(
                transform.TransformPoint(_limbs[i].defaultPosition),
                transform.up) + transform.up * _feetOffset;
            dist = (targetPoint - _limbs[i].lastPosition).magnitude;
            if (dist > 0.005f)
            {
                StartCoroutine(_Stepping(i, targetPoint));
                return;
            }
        }
        _allLimbsResting = true;
    }

    private Vector3 _RaycastToGround(Vector3 pos, Vector3 up)
    {
        Vector3 point = pos;

        Ray ray = new Ray(pos + _raycastRange * up, -up);
        if (Physics.Raycast(ray, out RaycastHit hit, 2f * _raycastRange, _groundLayerMask))
            point = hit.point;
        return point;
    }

    private IEnumerator _Stepping(int limbIdx, Vector3 targetPosition)
    {
        _limbs[limbIdx].moving = true;
        Vector3 startPosition = _limbs[limbIdx].lastPosition;
        float t;
        for (int i = 1; i <= _smoothness; ++i)
        {
            t = i / (_smoothness + 1f);
            _limbs[limbIdx].IKTarget.position =
                Vector3.Lerp(startPosition, targetPosition, t)
                + transform.up * Mathf.Sin(t * Mathf.PI) * _stepHeight;
            yield return new WaitForFixedUpdate();
        }
        _limbs[limbIdx].IKTarget.position = targetPosition;
        _limbs[limbIdx].lastPosition = targetPosition;
        _limbs[limbIdx].moving = false;
    }
}
