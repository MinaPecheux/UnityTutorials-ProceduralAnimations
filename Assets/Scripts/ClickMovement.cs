using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickMovement : MonoBehaviour
{
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private float _speed = 10f;

    private Camera _cam;
    private Ray _ray;
    private RaycastHit _hit;

    private Vector3 _targetPosition;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Start()
    {
        _targetPosition = transform.position;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(_ray, out _hit, _groundLayerMask))
            {
                transform.rotation = Quaternion.LookRotation(_hit.point - transform.position, Vector3.up);
                _targetPosition = _hit.point;
            }
        }

        if ((_targetPosition - transform.position).sqrMagnitude < 0.02f)
        {
            transform.position = _targetPosition;
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _speed * Time.deltaTime);
        }
    }
}
