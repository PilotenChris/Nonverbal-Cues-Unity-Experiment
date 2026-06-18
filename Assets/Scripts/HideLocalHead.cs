using UnityEngine;

public class HideLocalHead : MonoBehaviour {
    public Transform headBode;

    // Start is called before the first frame update
    void Start() {
        if (headBode != null) {
            headBode.localScale = Vector3.zero;
        }
    }
}
