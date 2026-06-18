using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadLook : MonoBehaviour {

    [SerializeField]
    private Transform _headObject, _targetObject, _headForward;

    [SerializeField]
    private Transform _hmdCamera;
    [SerializeField]
    private Transform _npcRoot;

    [SerializeField]
    private float _maxDistance = 2.5f;

    [SerializeField]
    private float _maxAngle = 70f;

    [SerializeField]
    private float _minAngle = -70f;

    [SerializeField]
    private float _lookSpeed = 7f;

    private bool _isLooking;

    private Quaternion _lastRotation;

    private float _headResetTimer;

    private void LateUpdate() {
        if (!_hmdCamera || !_npcRoot) return;

        // Distance gating
        Vector3 userPos = _hmdCamera.position;
        Vector3 npcPos = _npcRoot.position;
        npcPos.y = userPos.y; // Ignore vertical difference
        float distance = Vector3.Distance(userPos, npcPos);

        if (distance > _maxDistance) {
            ResetHeadRotation();
            return;
        }
        _isLooking = true;

        Vector3 Direction = (_targetObject.position - _headObject.position).normalized;
        float angle = Vector3.SignedAngle(Direction, _headForward.forward, _headForward.up);
        if (angle < _maxAngle && angle > _minAngle) {
            if (!_isLooking) {
                _isLooking = true;
                _lastRotation = _headObject.rotation;
            }
            Quaternion TargetRotation = Quaternion.LookRotation(_targetObject.position - _headObject.position);
            _lastRotation = Quaternion.Slerp(_lastRotation, TargetRotation, _lookSpeed * Time.deltaTime);
            
            _headObject.rotation = _lastRotation;
            _headResetTimer = 0.5f;
        } else if (_isLooking) {
            ResetHeadRotation();
        }
        
    }

    private void ResetHeadRotation() {
        _lastRotation = Quaternion.Slerp(_lastRotation, _headForward.rotation, _lookSpeed * Time.deltaTime);
        _headObject.rotation = _lastRotation;
        _headResetTimer -= Time.deltaTime;

        if (_headResetTimer <= 0f) {
            _headObject.rotation = _headForward.rotation;
            _isLooking = false;
        }
    }
}
