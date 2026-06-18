using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AvatarHeightCalibrator : MonoBehaviour {
    [Header("XR")]
    public Transform hmdCamera;
    public Transform leftController;
    public Transform rightController;

    [Header("Avatar")]
    public Transform avatarRoot;
    public Transform headBone;
    public Transform leftFootBone;
    public Transform rightFootBone;

    [Header("Rig")]
    public RigBuilder rigBuilder;

    [Header("Avatar Targets (used by your rig constraints)")]
    public Transform avatarTargetRoot;
    public Transform headTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Floor")]
    public Transform floorReference;

    [Header("Testing / Override")]
    public bool useOverrideHeight = false;
    public float overrideUserHeightMeters = 1.65f;

    [Header("Head Target Offset")]
    public Vector3 headTargetLocalOffset = new Vector3(0f, -0.10f, 0.05f);

    private Vector3 _baseScale;
    private bool _initialized;

    private float FloorY => floorReference ? floorReference.position.y : 0.0f;

    private void Awake() {
        if (avatarRoot) {
            _baseScale = avatarRoot.localScale;
            _initialized = true;
        }
    }

    private void LateUpdate() {
        DriveTargetsFromXR();
    }

    public void CalibrateHeight() {
        if (!hmdCamera || !avatarRoot || !headBone || !leftFootBone || !rightFootBone) {
            Debug.LogError("AvatarHeightCalibrator: Missing references for calibration.");
            return;
        }

        if (!_initialized) {
            _baseScale = avatarRoot.localScale;
            _initialized = true;
        }

        if (rigBuilder) rigBuilder.enabled = false;

        // Calculate HMD height
        //float userHeight = hmdCamera.position.y - floorY;
        float userHeight = useOverrideHeight ? overrideUserHeightMeters : (hmdCamera.position.y - FloorY);

        if (userHeight <= 0.5f) {
            Debug.LogError($"AvatarHeightCalibrator: userHeight looks small ({userHeight:F2}m.) Check floorY/xrOrigin setup.");
        }

        Vector3 headLocal = avatarRoot.InverseTransformPoint(headBone.position);
        Vector3 leftFootLocal = avatarRoot.InverseTransformPoint(leftFootBone.position);
        Vector3 rightFootLocal = avatarRoot.InverseTransformPoint(rightFootBone.position);

        float footY = Mathf.Min(leftFootLocal.y, rightFootLocal.y);
        float avatarHeight = headLocal.y - footY;

        if (userHeight <= 0.1f || avatarHeight <= 0.1f) {
            Debug.LogError($"Invalid heights. userHeight={userHeight:F2}, avatarHeight={avatarHeight:F2}");
            if (rigBuilder) rigBuilder.enabled = true;
            return;
        }

        float scaleFactor = userHeight / avatarHeight;

        //_baseScale = avatarRoot.localScale;

        avatarRoot.localScale = _baseScale * scaleFactor;

        float newFootY = Mathf.Min(leftFootBone.position.y, rightFootBone.position.y);
        float deltaY = FloorY - newFootY;
        avatarRoot.position += Vector3.up * deltaY;

        if (rigBuilder) {
            rigBuilder.enabled = true;
            rigBuilder.Build();
        }

        Debug.Log($"Calibrated: userHeight={userHeight:F2}m avatarHeight={avatarHeight:F2}m scaleFactor={scaleFactor:F3}");
    }

    private void DriveTargetsFromXR() {
        if (!avatarTargetRoot || !headTarget) return;
        if (!hmdCamera) return;

        Vector3 hmdPos = hmdCamera.position;
        Quaternion hmdRot = hmdCamera.rotation;

        Vector3 headPosWithOffset = hmdPos + (hmdRot * headTargetLocalOffset);


        Vector3 leftPos = leftController ? leftController.position : Vector3.zero;
        Quaternion leftRot = leftController ? leftController.rotation : Quaternion.identity;

        Vector3 rightPos = rightController ? rightController.position : Vector3.zero;
        Quaternion rightRot = rightController ? rightController.rotation : Quaternion.identity;

        if (useOverrideHeight) {
            float realHeight = hmdCamera.position.y - FloorY;
            float desiredHeight = overrideUserHeightMeters;
            float delta = desiredHeight - realHeight;

            Vector3 offset = Vector3.up * delta;
            hmdPos += offset;
            leftPos += offset;
            rightPos += offset;
        }

        headTarget.SetPositionAndRotation(headPosWithOffset, hmdRot);

        if (leftHandTarget && leftController)
            leftHandTarget.SetPositionAndRotation(leftPos, leftRot);

        if (rightHandTarget && rightController)
            rightHandTarget.SetPositionAndRotation(rightPos, rightRot);
    }
}
