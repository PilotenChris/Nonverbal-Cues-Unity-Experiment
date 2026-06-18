using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.Interaction.Toolkit;

public class STT_HF_OpenAI : MonoBehaviour {

    [SerializeField]
    private string HF_API_KEY;
    const string STT_API_URI = "https://router.huggingface.co/hf-inference/models/openai/whisper-large-v3";

    [SerializeField]
    private LLM_LLama llmLlama;

    [SerializeField]
    private NonverbalCueUdpReceiver _cueReceiver;

    MemoryStream stream;

    public void SelectEventHandler(SelectEnterEventArgs eventArgs) {
        StartSpeaking();
    }

    public void SelectExitEventHandler(SelectExitEventArgs eventArgs) {
        Microphone.End(null);
    }

    public void StartSpeaking() {
        stream = new MemoryStream();

        AudioSource aud = GetComponent<AudioSource>();
        Debug.Log("Start Recording");
        _cueReceiver.SetActiveCueEvent(NonverbalCueUdpReceiver.CuePhase.Speaking);
        aud.clip = Microphone.Start(null, false, 30, 11025);

        StartCoroutine(RecordAudio(aud.clip));
    }

    IEnumerator RecordAudio(AudioClip clip) {
        while (Microphone.IsRecording(null)) {
            yield return null;
        }

        AudioSource aud = GetComponent<AudioSource>();
        ConvertClipToWav(aud.clip);
        StartCoroutine(STT());
    }

    IEnumerator STT() {
        SpeechToTextData sttData = new SpeechToTextData();

        UnityWebRequest request = new UnityWebRequest(STT_API_URI, "POST");
        request.uploadHandler = new UploadHandlerRaw(stream.GetBuffer());
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "audio/wav");
        request.SetRequestHeader("Authorization", "Bearer " + HF_API_KEY);

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success) {
            string responseText = request.downloadHandler.text;
            SpeechToTextData sttResponse = JsonUtility.FromJson<SpeechToTextData>(responseText);
            Debug.Log("STT Response: " + sttResponse.text);

            if (llmLlama) {
                llmLlama.TextToLLM(sttResponse.text);
            }
        } else {
            Debug.LogError("STT API request failed: " + request.error);
        }
    }

    // JSON Handler
    [Serializable]
    public class SpeechToTextData {
        public string text;
    }


    public Stream ConvertClipToWav(AudioClip clip) {
        var data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);

        if (stream != null) {
            stream.Dispose();
        }
        stream = new MemoryStream();

        var bitsPerSample = (ushort)16;
        var chunkID = "RIFF";
        var format = "WAVE";
        var subChunk1ID = "fmt ";
        var subChunk1Size = (uint)16;
        var audioFormat = (ushort)1;
        var numChannels = (ushort)clip.channels;
        var sampleRate = (uint)clip.frequency;
        var byteRate = (uint)(sampleRate * clip.channels * bitsPerSample / 8);
        var blockAlign = (ushort)(numChannels * bitsPerSample / 8);
        var subChunk2ID = "data";
        var subChunk2Size = (uint)(data.Length * clip.channels * bitsPerSample / 8);
        var chunkSize = (uint)(36 + subChunk2Size);

        WriteString(stream, chunkID);
        WriteUInt(stream, chunkSize);
        WriteString(stream, format);
        WriteString(stream, subChunk1ID);
        WriteUInt(stream, subChunk1Size);
        WriteShort(stream, audioFormat);
        WriteShort(stream, numChannels);
        WriteUInt(stream, sampleRate);
        WriteUInt(stream, byteRate);
        WriteShort(stream, blockAlign);
        WriteShort(stream, bitsPerSample);
        WriteString(stream, subChunk2ID);
        WriteUInt(stream, subChunk2Size);

        foreach (var sample in data) {
            var deNormalizedSample = (short)0;
            if (sample > 0) {
                var temp = sample * short.MaxValue;
                if (temp > short.MaxValue) {
                    temp = short.MaxValue;
                }
                deNormalizedSample = (short)temp;
            }
            if (sample < 0) {
                var temp = sample * (-short.MinValue);
                if (temp < short.MinValue) {
                    temp = short.MinValue;
                }
                deNormalizedSample = (short)temp;
            }
            WriteShort(stream, (ushort)deNormalizedSample);
        }
        return stream;
    }

    private void WriteUInt(Stream stream, uint data) {
        stream.WriteByte((byte)(data & 0xFF));
        stream.WriteByte((byte)((data >> 8) & 0xFF));
        stream.WriteByte((byte)((data >> 16) & 0xFF));
        stream.WriteByte((byte)((data >> 24) & 0xFF));
    }

    private void WriteShort(Stream stream, ushort data) {
        stream.WriteByte((byte)(data & 0xFF));
        stream.WriteByte((byte)((data >> 8) & 0xFF));
    }

    private void WriteString(Stream stream, string data) {
        foreach (var character in data) {
            stream.WriteByte((byte)character);
        }
    }
}
