using UnityEngine;

public class LookAtNpcCue : MonoBehaviour {
    [Header("References")]
    [SerializeField]
    private Transform _hmdCamera;
    [SerializeField]
    private Transform _npcRoot;

    [Header("Range")]
    [SerializeField]
    private float _maxDistance = 2.5f;

    [Header("Angle thresholds (degrees)")]
    [SerializeField]
    private float _lookingAt = 15f;

    [SerializeField]
    private float _glancing = 35f;

    [SerializeField]
    private float _lookingNear = 60f;

    [Header("Stability")]
    [SerializeField]
    private float _minSecondsBetweenCueChanges = 0.25f;

    [SerializeField]
    private NonverbalCueUdpReceiver _cueBuffer;

    private string _lastCue = null;

    void Reset() {
        _npcRoot = transform;
    }

    // Update is called once per frame
    void Update() {
        if (!_hmdCamera || !_npcRoot) return;

        // Distance gating
        Vector3 userPos = _hmdCamera.position;
        Vector3 npcPos = _npcRoot.position;
        npcPos.y = userPos.y; // Ignore vertical difference
        float distance = Vector3.Distance(userPos, npcPos);

        if (distance > _maxDistance) {
            _cueBuffer.SetIsCloseToNpc(false);
            return;
        }

        _cueBuffer.SetIsCloseToNpc(true);

        // Direction and angle
        Vector3 toNpc = (npcPos - userPos).normalized;
        Vector3 userHeadForward = _hmdCamera.forward;
        userHeadForward.y = 0; // Ignore vertical direction
        userHeadForward.Normalize();

        float angle = Vector3.Angle(userHeadForward, toNpc);

        string cue =
            angle <= _lookingAt ? "looking_at_npc" :
            angle <= _glancing ? "glancing_at_npc" :
            angle <= _lookingNear ? "looking_near_npc" :
            "looking_away_from_npc";

        SetCueIfChanged(cue);
    }

    private void SetCueIfChanged(string cue) {
        if (cue == _lastCue) return;

        float now = Time.time;

        _lastCue = cue;

        if (_cueBuffer != null) {
            _cueBuffer.AddExternalEventCue(now, cue);
        }
    }
}
