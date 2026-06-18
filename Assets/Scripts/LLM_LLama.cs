using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLM_LLama : MonoBehaviour {
    [SerializeField]
    private string _API_KEY;
    const string LLM_API_URI = "https://api.groq.com/openai/v1/chat/completions";

    private enum SelectModel {
        llama_3X1_8b_instant, llama_3X3_70b_versatile
    }

    [SerializeField]
    private SelectModel _selectModel;
    string selectedLLMString;

    private string _LLMresult = "Waiting";

    [SerializeField]
    private NonverbalCueUdpReceiver _cueReceiver;

    [SerializeField]
    TTS_SF_Simba ttsSFSimba;

    [SerializeField]
    private bool _shortResponse;

    [SerializeField]
    private string _whoAmI = "nobody";

    [SerializeField]
    private string _context;

    [SerializeField]
    private bool _useCueContext;

    List<Message> messageHistory;

    [Header("Logging")]
    [SerializeField]
    private bool _enableLogging;


    [SerializeField]
    private string _logFileName = "participant_log.json";

    private string _logPath;
    private string _systemPrompt;

    [Serializable]
    private class LogEntry {
        public string responseTime;
        public string timestamp;
        public string model;
        public string systemPrompt;
        public string userMessageRaw;
        public string userMessageSent;
        public string llmResponse;
    }

    private DateTime _queryStartTime = new DateTime();


    // Start is called before the first frame update
    void Start() {
        string prompt;
        DateTime currentDateTime = DateTime.Now;

        selectedLLMString = _selectModel.ToString().Replace('_', '-').Replace('X', '.');
        Debug.Log($"Selected LLM Model: {selectedLLMString}");

        prompt = "You are " + _whoAmI;
        if (_shortResponse) {
            prompt += "\nAnswer all the questions brief and concise!";
        }

        if (_useCueContext) {
            prompt += "\nYou have access to the user's nonverbal cues (Like: Yes nod means yes or agreement, No nod means no or disagreement, Left or right hand wave can ba a greeting, Both hand wave can be attention, Left or Right foot restless can be impation, nervous, bored, thinking. etc) that can provide additional context about their emotional state and intentions. Use this information to better understand the user's needs and respond more empathetically and appropriately.";
            prompt += "\nAnswer the questions based on the following context:\n===\n";
        } else {
            prompt += "\nAnswer the questions based on the following context:\n===\n";
        }

        prompt += _context;
        prompt += "\nToday is " + currentDateTime.ToShortDateString();
        prompt += "\nCurrent time is " + currentDateTime.ToShortTimeString();
        prompt += "\n===";

        _systemPrompt = prompt;

        _logPath = Path.Combine(Application.persistentDataPath, _logFileName);

        Debug.Log("LLM Prompt: " + prompt);
        Debug.Log("Log Path: " + _logPath);

        messageHistory = new List<Message>();
        AppendConversation(prompt, "system");
    }

    private void AppendLog(LogEntry entry) {
        if (!_enableLogging) return;

        try {
            string json = JsonUtility.ToJson(entry);
            File.AppendAllText(_logPath, json + "\n", Encoding.UTF8);
        } catch (Exception ex) {
            Debug.LogError("Failed to write log entry: " + ex.Message);
        }
    }

    private void AppendConversation(string message, string myRole) {
        Message newMessage = new Message {
            role = myRole,
            content = message
        };
        messageHistory.Add(newMessage);
    }

    private void SetNewResponseTime() {
        _queryStartTime = DateTime.Now;
    }

    private void ResetResponseTime() {
        _queryStartTime = DateTime.MinValue;
    }


    public void TextToLLM(string message) {
        SetNewResponseTime();
        StartCoroutine(TalkToLLM(message));
    }

    private IEnumerator TalkToLLM(string message) {
        RequestBody requestBody = new RequestBody();

        // Add user nonverbal cues
        string cueText = _cueReceiver != null ? _cueReceiver.BuildCueContextString() : "";
        if (_cueReceiver != null) {
            _cueReceiver.Clear();
            _cueReceiver.SetActiveCueEvent(NonverbalCueUdpReceiver.CuePhase.Listening);
        }

        string augmentedUserMessage = message;
        if (!string.IsNullOrEmpty(cueText) && _useCueContext) {
            augmentedUserMessage = $" [User nonverbal cues: {cueText}]\n{message}";
        }

        AppendConversation(augmentedUserMessage, "user");
        requestBody.messages = messageHistory.ToArray();

        foreach (var item in requestBody.messages) {
            Debug.Log($"Role: {item.role} | Content: {item.content}");
        }

        requestBody.model = selectedLLMString;
        string jsonRequestBody = JsonUtility.ToJson(requestBody);
        _LLMresult = "Waiting";

        UnityWebRequest request = new UnityWebRequest(LLM_API_URI, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonRequestBody);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        request.SetRequestHeader("Authorization", "Bearer " + _API_KEY);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            string responseText = request.downloadHandler.text;
            GroqCloudResponse groqCR = JsonUtility.FromJson<GroqCloudResponse>(responseText);
            _LLMresult = groqCR.choices[0].message.content;
            AppendConversation(_LLMresult, "assistant");

            Debug.Log("LLM Response: " + _LLMresult);

            string calculatedResponseTime = "-1";

            if (_queryStartTime != DateTime.MinValue) {
                calculatedResponseTime = (DateTime.Now - _queryStartTime).TotalSeconds.ToString("F2");
            } else {
                calculatedResponseTime = "-1";
            }

            AppendLog(new LogEntry {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "",
                responseTime = calculatedResponseTime,
                model = selectedLLMString,
                systemPrompt = _systemPrompt,
                userMessageRaw = message,
                userMessageSent = augmentedUserMessage,
                llmResponse = _LLMresult
            });

            ResetResponseTime();

            // Call TTS
            if (ttsSFSimba) {
                ttsSFSimba.Say(_LLMresult);
            }
        } else {
            Debug.Log("LLM API Request failed: " + request.error);
        }
    }

    // Write JSON to LLM classes
    [Serializable]
    public class RequestBody {
        public Message[] messages;
        public string model;
    }

    [Serializable]
    public class Message {
        public string role;
        public string content;
    }

    [Serializable]
    public class GroqCloudResponse {
        public string id;
        public string @object;
        public int created;
        public string model;
        public Choice[] choices;
        public Usage usage;
        public string system_fingerprint;
        public XGroq x_groq;
    }

    [Serializable]
    public class Choice {
        public int index;
        public ChoiceMessage message;
        public object logprobs;
        public string finish_reason;
    }

    [Serializable]
    public class ChoiceMessage {
        public string role;
        public string content;
    }

    [Serializable]
    public class Usage {
        public int prompt_tokens;
        public float prompt_time;
        public int completion_tokens;
        public float completion_time;
        public int total_tokens;
        public float total_time;
    }

    [Serializable]
    public class XGroq {
        public string id;
    }
}
