using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class DataChannelTest : MonoBehaviour {
#pragma warning disable 0649
    [SerializeField] Button callButton;
    [SerializeField] Button sendButton;
    [SerializeField] Button createOfferButton;
    [SerializeField] Button receiveOfferButton;
    [SerializeField] Button receiveAnswerButton;
    [SerializeField] Button receiveICEButton;
    [SerializeField] InputField textSend;
    [SerializeField] InputField textReceive;
    [SerializeField] InputField sdpExchange;
    [SerializeField] InputField iceExchange;
#pragma warning restore 0649

    RTCPeerConnection connection;
    RTCDataChannel localChannel;
    RTCDataChannel remoteChannel;

    RTCOfferOptions OfferOptions = new RTCOfferOptions {
        iceRestart = false,
        offerToReceiveAudio = false,
        offerToReceiveVideo = false
    };

    RTCAnswerOptions AnswerOptions = new RTCAnswerOptions {
        iceRestart = false,
    };

    void Awake() {
        WebRTC.Initialize();
        callButton.onClick.AddListener(() => { StartCoroutine(Call()); });
        createOfferButton.onClick.AddListener(() => { StartCoroutine(SendOfferRoutine()); });
        receiveOfferButton.onClick.AddListener(() => { StartCoroutine(ReceiveOfferAndSendAnswerRoutine()); });
        receiveAnswerButton.onClick.AddListener(() => { StartCoroutine(ReceiveAnswerRoutine()); });
        receiveICEButton.onClick.AddListener(() => { StartCoroutine(ReceiveICERoutine()); });
        createOfferButton.interactable = false;
        receiveOfferButton.interactable = false;
        receiveAnswerButton.interactable = false;
    }

    void Update() {
        if (Keyboard.current.f5Key.isPressed) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void OnDestroy() {
        WebRTC.Dispose();
    }

    void Start() {
        callButton.interactable = true;
    }

    void HandleOnMessage(byte[] bytes) {
        ReceiveMessage(System.Text.Encoding.UTF8.GetString(bytes));
    }
    void HandleOnDataChannel(RTCDataChannel channel) {
        Debug.Log($"{nameof(HandleOnDataChannel)}: {channel}");
        remoteChannel = channel;
        remoteChannel.OnMessage = HandleOnMessage;
    }
    void HandleOnIceCandidate(RTCIceCandidate candidate) {
        Debug.Log($"{nameof(HandleOnIceCandidate)}: {candidate.Candidate}");

        iceExchange.text = JsonUtility.ToJson(new RTCIceCandidateInit {
            sdpMid = candidate.SdpMid,
            sdpMLineIndex = candidate.SdpMLineIndex,
            candidate = candidate.Candidate
        });
    }
    void HandleOnDataChannelOpen() {
        Debug.Log($"{nameof(HandleOnDataChannelOpen)}: ??");
    }
    void HandleOnIceConnectionChange(RTCIceConnectionState state) {
        Debug.Log($"{nameof(HandleOnIceConnectionChange)}: {state}");
    }

    RTCConfiguration GetSelectedSdpSemantics() {
        RTCConfiguration config = default;
        config.iceServers = new RTCIceServer[]
        {
            new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
        };

        return config;
    }
    void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state) {
        switch (state) {
            case RTCIceConnectionState.New:
                Debug.Log($"{pc} IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log($"{pc} IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log($"{pc} IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log($"{pc} IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log($"{pc} IceConnectionState: Connected");
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log($"{pc} IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log($"{pc} IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug.Log($"{pc} IceConnectionState: Max");
                break;
            default:
                break;
        }
    }
    IEnumerator Call() {
        Debug.Log("GetSelectedSdpSemantics");
        var configuration = GetSelectedSdpSemantics();
        connection = new RTCPeerConnection(ref configuration) {
            OnIceCandidate = HandleOnIceCandidate,
            OnIceConnectionChange = HandleOnIceConnectionChange
        };

        var conf = new RTCDataChannelInit();
        localChannel = connection.CreateDataChannel("data", conf);
        localChannel.OnOpen = HandleOnDataChannelOpen;
        connection.OnDataChannel = HandleOnDataChannel;

        yield return null;

        callButton.interactable = false;
        createOfferButton.interactable = true;
        receiveOfferButton.interactable = true;
    }

    RTCSessionDescription CreateOfferDescription() => new RTCSessionDescription {
        type = RTCSdpType.Offer,
        sdp = sdpExchange.text
    };
    RTCSessionDescription CreateAnswerDescription() => new RTCSessionDescription {
        type = RTCSdpType.Answer,
        sdp = sdpExchange.text
    };
    void SetDescription(RTCSessionDescription desc) {
        sdpExchange.text = desc.sdp;
    }


    IEnumerator SendOfferRoutine() {
        createOfferButton.interactable = false;
        receiveOfferButton.interactable = false;
        receiveAnswerButton.interactable = false;
        yield return CreateOffer();
        yield return SetLocalOffer();
        yield return SendOffer();
        receiveAnswerButton.interactable = true;
    }
    IEnumerator ReceiveOfferAndSendAnswerRoutine() {
        createOfferButton.interactable = false;
        receiveOfferButton.interactable = false;
        receiveAnswerButton.interactable = false;
        yield return SetRemoteOffer();
        yield return CreateAnswer();
        yield return SetLocalAnswer();
    }
    IEnumerator ReceiveAnswerRoutine() {
        createOfferButton.interactable = false;
        receiveOfferButton.interactable = false;
        receiveAnswerButton.interactable = false;
        yield return SetRemoteAnswer();
    }
    IEnumerator ReceiveICERoutine() {
        var ice = JsonUtility.FromJson<RTCIceCandidateInit>(iceExchange.text);
        var candidate = new RTCIceCandidate(ice);
        connection.AddIceCandidate(candidate);
        yield return null;
    }




    IEnumerator CreateOffer() {
        Debug.Log("CreateOffer");
        var op = connection.CreateOffer(ref OfferOptions);
        yield return op;

        if (op.IsError) {
            OnSessionDescriptionError(op.Error);
        } else {
            SetDescription(op.Desc);
        }
    }
    IEnumerator SendOffer() {
        Debug.Log("SendOffer (TODO)");
        yield return null;
    }
    IEnumerator CreateAnswer() {
        var op = connection.CreateAnswer(ref AnswerOptions);
        yield return op;

        if (op.IsError) {
            OnSessionDescriptionError(op.Error);
        } else {
            SetDescription(op.Desc);
        }
    }

    public void ReceiveMessage(string text) {
        textReceive.text = text;
    }
    public void SendMessage() {
        localChannel.Send(textSend.text);
    }

    void OnSessionDescriptionError(RTCError error) {
        Debug.Log($"{nameof(OnSessionDescriptionError)}: {error.message}");
    }

    IEnumerator SetLocalOffer() {
        Debug.Log("SetLocalOffer");
        var desc = CreateOfferDescription();
        var op = connection.SetLocalDescription(ref desc);
        yield return op;

        if (op.IsError) {
            var error = op.Error;
            OnSessionDescriptionError(error);
        }
    }
    IEnumerator SetRemoteOffer() {
        Debug.Log("SetRemoteOffer");
        var desc = CreateOfferDescription();
        var op = connection.SetRemoteDescription(ref desc);
        yield return op;

        if (op.IsError) {
            OnSessionDescriptionError(op.Error);
        }
    }
    IEnumerator SetLocalAnswer() {
        Debug.Log("SetLocalOffer");
        var desc = CreateAnswerDescription();
        var op = connection.SetLocalDescription(ref desc);
        yield return op;

        if (op.IsError) {
            OnSessionDescriptionError(op.Error);
        }
    }
    IEnumerator SetRemoteAnswer() {
        var desc = CreateAnswerDescription();
        var op = connection.SetRemoteDescription(ref desc);
        yield return op;

        if (op.IsError) {
            OnSessionDescriptionError(op.Error);
        }
    }
}
