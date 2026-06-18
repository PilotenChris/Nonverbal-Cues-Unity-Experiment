using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class NonverbalCueUdpReceiver : MonoBehaviour {
    [Header("UDP")]
    public int listenPort = 9000;

    [Header("Buffer")]
    public int maxEvents = 50;

    // Threading
    private UdpClient _udp;
    private Thread _thread;
    private volatile bool _isRunning;

    // Thread-safe queue from receiver thread -> main thread
    private readonly Queue<string> _eventQueue = new Queue<string>();
    private readonly object _queueLock = new object();

    // Stored cue events (main thread)
    private readonly List<CueEvent> _cueSpeakingEvents = new List<CueEvent>();
    private readonly List<CueEvent> _cueThinkingEvents = new List<CueEvent>();
    private readonly List<CueEvent> _cueListeningEvents = new List<CueEvent>();

    // 1 = listening, 2 = thinking, 3 = speaking
    private CuePhase _activeCueEvent = CuePhase.Thinking;
    private CuePhase? _lastCueEvent = null;
    private string _lastCue = null;
    private bool _isCloseToNpc = false;

    [Serializable]
    public class CuePacket {
        public double timestamp;
        public string[] active;
        public string[] triggers;
        public bool is_standing;
    }

    private class CueEvent {
        public float time;
        public string cue;
    }

    public enum CuePhase {
        Listening = 1,
        Thinking = 2,
        Speaking = 3
    }

    private void OnEnable() {
        StartReceiver();
    }

    void OnDisable() {
        StopReceiver();
    }

    private void StartReceiver() {
        if (_isRunning) return;
        _isRunning = true;

        _udp = new UdpClient(listenPort);
        _udp.Client.ReceiveTimeout = 500;

        _thread = new Thread(ReceiveLoop) { IsBackground = true };
        _thread.Start();

        Debug.Log($"UDP Receiver listening on 127.0.0.1:{listenPort}");
    }

    private void StopReceiver() {
        _isRunning = false;

        try {
            _udp?.Close();
        } catch (Exception ex) {
            Debug.LogWarning($"Error closing UDP: {ex.Message}");
        }
        _udp = null;

        try {
            _thread?.Join(300);
        } catch (Exception ex) {
            Debug.LogWarning($"Error stopping thread: {ex.Message}");
        }
        _thread = null;
    }

    private void ReceiveLoop() {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

        while (_isRunning) {
            try {
                byte[] data = _udp.Receive(ref ep);
                string json = Encoding.UTF8.GetString(data);

                lock (_queueLock) {
                    _eventQueue.Enqueue(json);
                }
            } catch (SocketException ex) {
                if (ex.SocketErrorCode != SocketError.TimedOut) {
                    Debug.LogWarning($"UDP receive error: {ex.Message}");
                }
            } catch (ObjectDisposedException ex) {
                Debug.LogWarning($"UDP socket closed: {ex.Message}");
                break; // Socket was closed, exit loop
            } catch (Exception ex) {
                Debug.LogWarning($"Unexpected error in UDP receive loop: {ex.Message}");
            }
        }
    }

    public void SetIsCloseToNpc(bool isClose) {
        _isCloseToNpc = isClose;
    }

    // Update is called once per frame
    void Update() {
        while (true) {
            string json;
            lock (_queueLock) {
                if (_eventQueue.Count == 0) break;
                json = _eventQueue.Dequeue();
            }

            TryConsume(json);
        }
    }

    private void TryConsume(string json) {
        json = json.Trim();

        CuePacket packet;

        try {
            packet = JsonUtility.FromJson<CuePacket>(json);
        } catch (Exception ex) {
            Debug.LogWarning($"Failed to parse cue packet: {ex.Message}\nRAW: {json}");
            return;
        }

        float now = Time.time;

        // Add triggers + active cues as events
        if (packet.triggers != null && _isCloseToNpc) {
            foreach (var t in packet.triggers) {
                AddEvent(now, t);
            }
        }

        if (packet.active != null && _isCloseToNpc) {
            foreach (var a in packet.active) {
                AddEvent(now, a);
            }
        }
    }

    private void AddEvent(float now, string cue) {
        if (string.IsNullOrEmpty(cue)) return;

        if (_lastCue != null && cue == _lastCue && _lastCueEvent == _activeCueEvent) return;

        if (_activeCueEvent == CuePhase.Listening) {
            _cueListeningEvents.Add(new CueEvent { time = now, cue = cue });
        }

        if (_activeCueEvent == CuePhase.Thinking) {
            _cueThinkingEvents.Add(new CueEvent { time = now, cue = cue });
        }

        if (_activeCueEvent == CuePhase.Speaking) {
            _cueSpeakingEvents.Add(new CueEvent { time = now, cue = cue });
        }

        if (_cueThinkingEvents.Count > maxEvents) {
            _cueThinkingEvents.RemoveAt(0);
        }

        if (_cueListeningEvents.Count > maxEvents) {
            _cueListeningEvents.RemoveAt(0);
        }

        if (_cueSpeakingEvents.Count > maxEvents) {
            _cueSpeakingEvents.RemoveAt(0);
        }

        _lastCueEvent = _activeCueEvent;
        _lastCue = cue;
    }

    public void AddExternalEventCue(float time, string cue) {
        AddEvent(time, cue);
    }

    public void SetActiveCueEvent(CuePhase phase) {
        _activeCueEvent = phase;
    }

    // Called by LLM before sending prompt, to get recent cues
    public string BuildCueContextString() {
        if (_cueSpeakingEvents.Count == 0 && _cueThinkingEvents.Count == 0 && _cueListeningEvents.Count == 0) return "";

        List<string> cuesSpeaking = new List<string>(_cueSpeakingEvents.Count);
        List<string> cuesThinking = new List<string>(_cueThinkingEvents.Count);
        List<string> cuesListening = new List<string>(_cueListeningEvents.Count);

        foreach (var e in _cueSpeakingEvents) {
            cuesSpeaking.Add($" Cue: {e.cue}");
        }

        foreach (var e in _cueThinkingEvents) {
            cuesThinking.Add($" Cue: {e.cue}");
        }

        foreach (var e in _cueListeningEvents) {
            cuesListening.Add($" Cue: {e.cue}");
        }

        List<string> sections = new List<string>();

        if (cuesListening.Count > 0) {
            sections.Add("LISTENING: " + string.Join(",", cuesListening));
        }

        if (cuesThinking.Count > 0) {
            sections.Add("THINKING: " + string.Join(",", cuesThinking));
        }

        if (cuesSpeaking.Count > 0) {
            sections.Add("SPEAKING: " + string.Join(",", cuesSpeaking));
        }

        return string.Join(" | ", sections);
    }

    public void Clear() {
        _cueSpeakingEvents.Clear();
        _cueThinkingEvents.Clear();
        _cueListeningEvents.Clear();
        _lastCue = null;
        _lastCueEvent = null;
    }
}