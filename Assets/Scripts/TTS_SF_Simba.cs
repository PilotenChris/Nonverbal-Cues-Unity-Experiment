using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TTS_SF_Simba : MonoBehaviour {
    // Variables
    [SerializeField]
    private string SPEECHIFY_API_KEY;

    private enum SelectVoice {
        US_monica, US_lauren, US_jennifer, US_susan
    }

    [SerializeField]
    private SelectVoice selectVoice;

    [Header("Voice Handler")]
    [SerializeField]
    private AudioSource _voiceHandlerAudioSoruce;

    [SerializeField]
    private NonverbalCueUdpReceiver _cueReceiver;

    const string TTS_API_URI = "https://api.sws.speechify.com/v1/audio/stream";
    private string sfVoice;

    Animator avatarAnimator;


    // Start is called before the first frame update
    void Start() {
        avatarAnimator = GetComponent<Animator>();
        sfVoice = selectVoice.ToString().Substring(3);
        Debug.Log("Selected Speechify Voice: " + sfVoice);

        // Debug
        //Say("This is Piloten_Chris");
    }

    public void Say(string messageInput) {
        StartCoroutine(PlayTTS(messageInput));
    }

    IEnumerator PlayTTS(string message) {
        TextToSpeechData ttsData = new TextToSpeechData();
        ttsData.input = SimpleCleanText(message);
        ttsData.voice_id = sfVoice;

        string jsonPrompt = JsonUtility.ToJson(ttsData);

        // WebRequest
        UnityWebRequest request = new UnityWebRequest(TTS_API_URI, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonPrompt));
        request.downloadHandler = new DownloadHandlerAudioClip(TTS_API_URI, AudioType.MPEG);

        // Headers
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "audio/mpeg");
        request.SetRequestHeader("Authorization", "Bearer " + SPEECHIFY_API_KEY);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            avatarAnimator.SetBool("isTalking", true);
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            AudioSource src = _voiceHandlerAudioSoruce != null ? _voiceHandlerAudioSoruce : GetComponent<AudioSource>();
            src.Stop();
            src.clip = clip;
            src.Play();

            yield return new WaitWhile(() => src.isPlaying);

            if (avatarAnimator) avatarAnimator.SetBool("isTalking", false);
            _cueReceiver.SetActiveCueEvent(NonverbalCueUdpReceiver.CuePhase.Thinking);
        } else {
            Debug.Log("TTS Request Error: " + request.error);
        }
    }

    // JSON Support Classes
    [Serializable]
    public class TextToSpeechData {
        public string input;
        public string voice_id;
    }

    string SimpleCleanText(string message) {
        string result = "";

        for (int i = 0; i < message.Length; i++) {
            switch (message[i]) {
                case '+':
                    result += " plus ";
                    break;
                case ':':
                    result += ", ";
                    break;
                case '*':
                    result += ", ";
                    break;
                case '=':
                    result += " equals ";
                    break;
                case '-':
                    result += " ";
                    break;
                case '#':
                    result += " hash ";
                    break;
                case '&':
                    result += " and ";
                    break;
                default:
                    result += message[i];
                    break;
            }
        }
        return result;
    }

}
