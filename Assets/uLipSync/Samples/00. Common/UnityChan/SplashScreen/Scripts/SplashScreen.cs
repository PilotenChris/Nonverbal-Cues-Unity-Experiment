using UnityEngine;

namespace UnityChan {
    [ExecuteInEditMode]
    public class SplashScreen : MonoBehaviour {
        void NextLevel() {
            Application.LoadLevel(Application.loadedLevel + 1);
        }
    }
}