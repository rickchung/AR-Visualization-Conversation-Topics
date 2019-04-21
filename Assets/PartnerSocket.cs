using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

/// <summary>
/// Partner socket is responsible for the communication between the local
/// device and the partner's device. This class provides two ways to build
/// up the connection: LAN servers and Remote server. The LAN servers require
/// both devices are in the same network space (e.g., under the same router)
/// or having static IP addresses. The Remote server uses the server
/// in ASU and it requires the ASU network environment.
/// </summary>
public class PartnerSocket : MonoBehaviour
{
    // For Internet server
    private const string HOST_SERVER = "10.218.106.151";
    private const int HOST_SERVER_PORT = 8082;
    // For local server
    private const int UDP_PORT = 7777;

    public Text localIpDisplay;
    public InputField partnerIPAddress;
    public GameObject serverMsgPanel;
    public SpeechToTextController speechToTextController;

    private Button hostConnBtn;
    private Text serverMsgText;

    public bool useLocalServer;

    private string _localIpAddress, _serverIpAddress;

    private bool _broadcastByHost;

    private NetworkClient clientToPartner;

    private static class MyMsgType
    {
        public static short MSG_TEST = MsgType.Highest + 1;
        public static short MSG_CLIENT_CONNECTED = MsgType.Highest + 2;
        public static short MSG_NEW_KEYWORDS = MsgType.Highest + 3;
        public static short MSG_PINNED_KEYWORDS = MsgType.Highest + 4;
        public static short MSG_TRANS = MsgType.Highest + 5;
    }

    void Start()
    {
        serverMsgText = serverMsgPanel.GetComponentInChildren<Text>();
        hostConnBtn = serverMsgPanel.GetComponent<Button>();

        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                _localIpAddress = ip.ToString();
            }
        }

        localIpDisplay.text = "LAN: " + _localIpAddress;

        if (useLocalServer)
            SetupLocalServer();
    }

    /// <summary>
    /// Setups the remote server. This method should not be used when the
    /// LAN servers are used.
    /// </summary>
    public void SetupRemoteServer()
    {
        Debug.Log("Trying to connect to the HOST server...");
        clientToPartner = new NetworkClient();
        clientToPartner.RegisterHandler(MsgType.Connect, OnConnected);
        clientToPartner.RegisterHandler(MyMsgType.MSG_TEST, OnTestMsgArrived);
        clientToPartner.RegisterHandler(MyMsgType.MSG_NEW_KEYWORDS, OnReceivedNewKeywords);
        clientToPartner.RegisterHandler(MyMsgType.MSG_TRANS, OnReceiveTranscript);
        clientToPartner.Connect(HOST_SERVER, HOST_SERVER_PORT);

        _broadcastByHost = true;

        serverMsgText.text = "Trying to find the host...";
        hostConnBtn.interactable = false;
    }

    // ==================== Middleware ==================== 

    /// <summary>
    /// The helper function that broadcasts the message through the server.
    /// This method will use the server according to the current connection
    /// type (LAN or Remote server).
    /// </summary>
    /// <param name="msgType">Message type.</param>
    /// <param name="msg">Message.</param>
    private void BroadcastToConnected(short msgType, MessageBase msg)
    {
        if (!_broadcastByHost)
        {
            NetworkServer.SendToAll(msgType, msg);
        }
        else
        {
            clientToPartner.Send(msgType, msg);
        }
    }

    // ==================== Server Side ==================== 

    /// <summary>
    /// Setups the local server in LAN environment. This method should not 
    /// be used when the Remote server is used.
    /// </summary>
    private void SetupLocalServer()
    {
        Debug.Log("[SERVER] Setting up a local server listening at " + UDP_PORT);
        NetworkServer.Listen(UDP_PORT);
        NetworkServer.RegisterHandler(MyMsgType.MSG_CLIENT_CONNECTED, OnConnectedClientReport);

        _broadcastByHost = false;
    }

    /// <summary>
    /// After the partner connects to this device, 
    /// you have to make your client connect back to the local server
    /// (this is only for LAN server setup).
    /// </summary>
    /// <param name="netMsg">Net message.</param>
    private void OnConnectedClientReport(NetworkMessage netMsg)
    {
        if (_serverIpAddress == null)
        {
            _serverIpAddress = netMsg.conn.address;
            Debug.Log("[SERVER] Received client report at " + _serverIpAddress);
            NetworkServer.dontListen = true;
            partnerIPAddress.interactable = false;
            partnerIPAddress.text = _serverIpAddress;
            SetupClient(_serverIpAddress);
        }
    }

    public void BroadcasetNewKeywords(string[] keywords)
    {
        BroadcastToConnected(MyMsgType.MSG_NEW_KEYWORDS, new StringMessage
        {
            value = string.Join(",", keywords)
        });
    }

    /// <summary>
    /// Broadcasts the new transcript to the partners. The transcript sent
    /// to the partners will have prefix "P:" showing this is a message from
    /// a partner.
    /// </summary>
    /// <param name="transcripts">Transcripts.</param>
    public void BroadcastNewTranscript(string[] transcripts)
    {
        string[] pTranscripts = new string[transcripts.Length];
        for (int i = 0; i < pTranscripts.Length; i++)
        {
            pTranscripts[i] = "P: " + transcripts[i];
        }
        BroadcastToConnected(MyMsgType.MSG_TRANS, new StringMessage
        {
            value = string.Join("|", pTranscripts)
        });
    }

    // ==================== Client Side ==================== 

    /// <summary>
    /// Create a client handler and connect to the server at partnerIP.
    /// </summary>
    /// <param name="partnerIP">Partner ip.</param>
    private void SetupClient(string partnerIP)
    {
        Debug.Log("[CLIENT] Trying to connect to the partner at " + partnerIP);
        clientToPartner = new NetworkClient();
        clientToPartner.RegisterHandler(MsgType.Connect, OnConnected);
        clientToPartner.RegisterHandler(MyMsgType.MSG_TEST, OnTestMsgArrived);
        clientToPartner.RegisterHandler(MyMsgType.MSG_NEW_KEYWORDS, OnReceivedNewKeywords);
        clientToPartner.RegisterHandler(MyMsgType.MSG_TRANS, OnReceiveTranscript);
        clientToPartner.Connect(partnerIP, UDP_PORT);
    }

    /// <summary>
    /// The callback function that will be invoked when the client connects to 
    /// the server successfully.
    /// </summary>
    /// <param name="netMsg">Net message.</param>
    private void OnConnected(NetworkMessage netMsg)
    {
        _serverIpAddress = clientToPartner.serverIp;
        // Disable the IP input field
        partnerIPAddress.interactable = false;

        Debug.Log("[CLIENT] Connected to the server at " + _serverIpAddress);
        serverMsgText.text = "Connected to " + _serverIpAddress;
        serverMsgText.text = "Connected to " + _serverIpAddress;

        // Tell the server you received the message.
        clientToPartner.Send(MyMsgType.MSG_CLIENT_CONNECTED, new StringMessage
        {
            value = "[CLIENT] Client report from " + _localIpAddress
        });
    }

    /// <summary>
    /// Used to test the connection from server to clients. For development
    /// purpose only.
    /// </summary>
    /// <param name="netMsg">Net message.</param>
    private void OnTestMsgArrived(NetworkMessage netMsg)
    {
        if (netMsg != null)
        {
            var msg = netMsg.ReadMessage<StringMessage>();
            Debug.Log("[CLIENT] Test message arrived: " + msg.value);
            serverMsgText.text = msg.value;
        }
    }

    /// <summary>
    /// The callback that will be invoked when the client receives new 
    /// broacasted transcripts from the server
    /// </summary>
    /// <param name="netMsg">Net message.</param>
    private void OnReceiveTranscript(NetworkMessage netMsg)
    {
        string transcripts = netMsg.ReadMessage<StringMessage>().value;
        speechToTextController.SaveTranscript(transcripts.Split('|'));
        speechToTextController.UpdateVis();
    }

    private void OnReceivedNewKeywords(NetworkMessage netMsg)
    {
        // Add to the panel
        var newKeywordsMsg = netMsg.ReadMessage<StringMessage>().value;
        speechToTextController.SaveTopics(newKeywordsMsg.Split(','));
        speechToTextController.UpdateVis();

        // Debug.Log("Received keywords: " + newKeywordsMsg);
    }
}
