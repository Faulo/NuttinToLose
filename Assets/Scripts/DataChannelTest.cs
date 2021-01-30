using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

class DataChannelTest : MonoBehaviour {
#pragma warning disable 0649
    [SerializeField] Button callButton;
    [SerializeField] Button sendButton;
    [SerializeField] Button createOfferButton;
    [SerializeField] Button receiveOfferButton;
    [SerializeField] Button createAnswerButton;
    [SerializeField] Button receiveAnswerButton;
    [SerializeField] InputField textSend;
    [SerializeField] InputField textReceive;
    [SerializeField] InputField offerExchange;
    [SerializeField] InputField answerExchange;
#pragma warning restore 0649

    RTCPeerConnection pc1, pc2;
    RTCDataChannel dataChannel, remoteDataChannel;
    Coroutine sdpCheck;
    string msg;
    DelegateOnIceConnectionChange pc1OnIceConnectionChange;
    DelegateOnIceConnectionChange pc2OnIceConnectionChange;
    DelegateOnIceCandidate pc1OnIceCandidate;
    DelegateOnIceCandidate pc2OnIceCandidate;
    DelegateOnMessage onDataChannelMessage;
    DelegateOnOpen onDataChannelOpen;
    DelegateOnClose onDataChannelClose;
    DelegateOnDataChannel onDataChannel;

    RTCOfferOptions OfferOptions = new RTCOfferOptions {
        iceRestart = false,
        offerToReceiveAudio = true,
        offerToReceiveVideo = false
    };

    RTCAnswerOptions AnswerOptions = new RTCAnswerOptions {
        iceRestart = false,
    };

    void Awake() {
        WebRTC.Initialize();
        callButton.onClick.AddListener(() => { StartCoroutine(Call()); });
        createOfferButton.onClick.AddListener(() => { StartCoroutine(SendOfferRoutine()); });
        receiveOfferButton.onClick.AddListener(() => { StartCoroutine(ReceiveOfferRoutine()); });
        createAnswerButton.onClick.AddListener(() => { StartCoroutine(SendAnswer()); });
        receiveAnswerButton.onClick.AddListener(() => { StartCoroutine(ReceiveAnswer()); });
        createOfferButton.interactable = false;
        receiveOfferButton.interactable = false;
        createAnswerButton.interactable = false;
        receiveAnswerButton.interactable = false;
    }

    void OnDestroy() {
        WebRTC.Dispose();
    }

    void Start() {
        callButton.interactable = true;

        pc1OnIceConnectionChange = state => { OnIceConnectionChange(pc1, state); };
        pc2OnIceConnectionChange = state => { OnIceConnectionChange(pc2, state); };
        pc1OnIceCandidate = candidate => { OnIceCandidate(pc1, candidate); };
        pc2OnIceCandidate = candidate => { OnIceCandidate(pc1, candidate); };
        onDataChannel = channel => {
            remoteDataChannel = channel;
            remoteDataChannel.OnMessage = onDataChannelMessage;
        };
        onDataChannelMessage = bytes => { textReceive.text = System.Text.Encoding.UTF8.GetString(bytes); };
        onDataChannelOpen = () => { sendButton.interactable = true; };
        onDataChannelClose = () => { sendButton.interactable = false; };
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
                Debug.Log($"{GetName(pc)} IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log($"{GetName(pc)} IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log($"{GetName(pc)} IceConnectionState: Connected");
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log($"{GetName(pc)} IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug.Log($"{GetName(pc)} IceConnectionState: Max");
                break;
            default:
                break;
        }
    }
    void Pc1OnIceConnectinChange(RTCIceConnectionState state) {
        OnIceConnectionChange(pc1, state);
    }
    void Pc2OnIceConnectionChange(RTCIceConnectionState state) {
        OnIceConnectionChange(pc2, state);
    }

    void Pc1OnIceCandidate(RTCIceCandidate candidate) {
        OnIceCandidate(pc1, candidate);
    }
    void Pc2OnIceCandidate(RTCIceCandidate candidate) {
        OnIceCandidate(pc2, candidate);
    }

    IEnumerator Call() {
        Debug.Log("GetSelectedSdpSemantics");
        var configuration = GetSelectedSdpSemantics();
        pc1 = new RTCPeerConnection(ref configuration);
        Debug.Log("Created local peer connection object pc1");
        pc1.OnIceCandidate = pc1OnIceCandidate;
        pc1.OnIceConnectionChange = pc1OnIceConnectionChange;
        pc2 = new RTCPeerConnection(ref configuration);
        Debug.Log("Created remote peer connection object pc2");
        pc2.OnIceCandidate = pc2OnIceCandidate;
        pc2.OnIceConnectionChange = pc2OnIceConnectionChange;
        pc2.OnDataChannel = onDataChannel;

        var conf = new RTCDataChannelInit();
        dataChannel = pc1.CreateDataChannel("data", conf);
        dataChannel.OnOpen = onDataChannelOpen;
        yield return null;
        callButton.interactable = false;
        createOfferButton.interactable = true;
        receiveOfferButton.interactable = true;
    }
    RTCSessionDescription CreateOfferDescription() => new RTCSessionDescription {
        type = RTCSdpType.Offer,
        sdp = offerExchange.text
    };
    IEnumerator SendOfferRoutine() {
        yield return CreateOffer();
        yield return SetLocalOffer();
        yield return SendOffer();
    }
    IEnumerator CreateOffer() {
        Debug.Log("pc1: CreateOffer");
        var op = pc1.CreateOffer(ref OfferOptions);
        yield return op;

        if (op.IsError) {
            OnCreateSessionDescriptionError(op.Error);
        } else {
            offerExchange.text = op.Desc.sdp;
        }
    }
    IEnumerator SetLocalOffer() {
        Debug.Log("pc1: SetLocalDescription");
        var desc = CreateOfferDescription();
        var op = pc1.SetLocalDescription(ref desc);
        yield return op;

        if (op.IsError) {
            var error = op.Error;
            OnSetSessionDescriptionError(ref error);
        }
    }
    IEnumerator SendOffer() {
        Debug.Log("pc1: SendOffer");
        var desc = CreateOfferDescription();
        var op = pc1.SetLocalDescription(ref desc);
        yield return op;

        if (op.IsError) {
            var error = op.Error;
            OnSetSessionDescriptionError(ref error);
        } else {
            OnSetLocalSuccess(pc1);
        }
    }
    IEnumerator ReceiveOfferRoutine() {
        yield return SetRemoteDescription();
    }
    IEnumerator SetRemoteDescription() {
        var desc = CreateOfferDescription();
        var op = pc2.SetRemoteDescription(ref desc);
        yield return op;

        if (op.IsError) {
            var error = op.Error;
            OnSetSessionDescriptionError(ref error);
        } else {
            OnSetRemoteSuccess(pc2);
        }
    }
    RTCSessionDescription CreateAnswerDescription() => new RTCSessionDescription {
        type = RTCSdpType.Answer,
        sdp = answerExchange.text
    };
    IEnumerator SendAnswer() {
        Debug.Log("pc2 createAnswer start");
        // Since the 'remote' side has no media stream we need
        // to pass in the right constraints in order for it to
        // accept the incoming offer of audio and video.

        var op3 = pc2.CreateAnswer(ref AnswerOptions);
        yield return op3;

        var desc = op3.Desc;

        if (!op3.IsError) {
            answerExchange.text = desc.sdp;
        } else {
            OnCreateSessionDescriptionError(op3.Error);
        }

        Debug.Log("pc2 setLocalDescription start");
        var op = pc2.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError) {
            OnSetLocalSuccess(pc2);
        } else {
            var error = op.Error;
            OnSetSessionDescriptionError(ref error);
        }
    }
    IEnumerator ReceiveAnswer() {
        Debug.Log("pc2 receiveAnswer start");
        var desc = new RTCSessionDescription() {
            type = RTCSdpType.Answer,
            sdp = answerExchange.text
        };
        yield return OnCreateAnswerSuccess(desc);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pc"></param>
    /// <param name="streamEvent"></param>
    void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate) {
        GetOtherPc(pc).AddIceCandidate(candidate);
        Debug.Log($"{GetName(pc)} ICE candidate:\n {candidate.Candidate}");
    }

    public void SendMsg() {
        dataChannel.Send(textSend.text);
    }
    string GetName(RTCPeerConnection pc) {
        return (pc == pc1) ? "pc1" : "pc2";
    }

    RTCPeerConnection GetOtherPc(RTCPeerConnection pc) {
        return (pc == pc1) ? pc2 : pc1;
    }

    void OnSetLocalSuccess(RTCPeerConnection pc) {
        Debug.Log($"{GetName(pc)} SetLocalDescription complete");
    }

    void OnSetSessionDescriptionError(ref RTCError error) { }

    void OnSetRemoteSuccess(RTCPeerConnection pc) {
        Debug.Log($"{GetName(pc)} SetRemoteDescription complete");
    }

    IEnumerator OnCreateAnswerSuccess(RTCSessionDescription desc) {
        Debug.Log($"Answer from pc2:\n{desc.sdp}");

        Debug.Log("pc1 setRemoteDescription start");

        var op2 = pc1.SetRemoteDescription(ref desc);
        yield return op2;
        if (!op2.IsError) {
            OnSetRemoteSuccess(pc1);
        } else {
            var error = op2.Error;
            OnSetSessionDescriptionError(ref error);
        }
    }

    IEnumerator LoopGetStats() {
        while (true) {
            yield return new WaitForSeconds(1f);

            if (!sendButton.interactable) {
                continue;
            }

            var op1 = pc1.GetStats();
            var op2 = pc2.GetStats();

            yield return op1;
            yield return op2;

            Debug.Log("pc1");
            foreach (var stat in op1.Value.Stats.Values) {
                Debug.Log(stat.Type.ToString());
            }
            Debug.Log("pc2");
            foreach (var stat in op2.Value.Stats.Values) {
                Debug.Log(stat.Type.ToString());
            }
        }
    }

    void OnAddIceCandidateSuccess(RTCPeerConnection pc) {
        Debug.Log($"{GetName(pc)} addIceCandidate success");
    }

    void OnAddIceCandidateError(RTCPeerConnection pc, RTCError error) {
        Debug.Log($"{GetName(pc)} failed to add ICE Candidate: ${error}");
    }

    void OnCreateSessionDescriptionError(RTCError e) {

    }
}
