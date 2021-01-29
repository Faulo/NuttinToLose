using System.Collections;
using Unity.WebRTC;
using UnityEngine;

public class HelloWorld : MonoBehaviour {
    [SerializeField]
    RTCOfferOptions offerOptions = new RTCOfferOptions();
    [SerializeField]
    RTCAnswerOptions answerOptions = new RTCAnswerOptions();

    RTCPeerConnection localConnection;
    RTCDataChannel sendChannel;

    RTCPeerConnection remoteConnection;
    RTCDataChannel receiveChannel;
    void OnEnable() {
        // Initialize WebRTC
        WebRTC.Initialize();

        // Create local peer
        localConnection = new RTCPeerConnection();
        sendChannel = localConnection.CreateDataChannel("sendChannel");
        sendChannel.OnOpen = HandleSendChannelOpen;
        sendChannel.OnClose = HandleSendChannelClose;

        // Create remote peer
        remoteConnection = new RTCPeerConnection {
            OnDataChannel = ReceiveChannelCallback
        };

        localConnection.OnIceCandidate = candidate => remoteConnection.AddIceCandidate(candidate);
        localConnection.OnIceConnectionChange = state => Debug.Log(state);
        remoteConnection.OnIceCandidate = candidate => localConnection.AddIceCandidate(candidate);

        StartCoroutine(CreateOfferRoutine());
    }

    IEnumerator CreateOfferRoutine() {
        RTCSessionDescription description;

        var op1 = localConnection.CreateOffer(ref offerOptions);
        yield return op1;
        description = op1.Desc;
        var op2 = localConnection.SetLocalDescription(ref description);
        yield return op2;
        description = op1.Desc;
        var op3 = remoteConnection.SetRemoteDescription(ref description);
        yield return op3;

        var op4 = remoteConnection.CreateAnswer(ref answerOptions);
        yield return op4;
        description = op4.Desc;
        var op5 = remoteConnection.SetLocalDescription(ref description);
        yield return op5;
        description = op4.Desc;
        var op6 = localConnection.SetRemoteDescription(ref description);
        yield return op6;

    }

    void OnDisable() {
        sendChannel.Close();
        receiveChannel.Close();

        localConnection.Close();
        remoteConnection.Close();
    }

    void ReceiveChannelCallback(RTCDataChannel channel) {
        Debug.Log($"ReceiveChannelCallback: {channel}");
        receiveChannel = channel;
        receiveChannel.OnMessage += HandleMessage;
        sendChannel.Send("Hallo Welt");
    }

    void HandleMessage(byte[] buffer) {
        string message = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        Debug.Log(message);
    }

    void HandleSendChannelOpen() {
        Debug.Log($"HandleSendChannelOpen");
    }
    void HandleSendChannelClose() {
        Debug.Log($"HandleSendChannelClose");
    }
}
