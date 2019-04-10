using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class PartnerSocket : MonoBehaviour
{
    private const string HOST_SERVER = "10.218.106.151:8082";
    private const int UDP_PORT = 7777;  // For local server

    public Text localIpDisplay;
    public InputField partnerIPAddress;
    public Text testMsgText;
    public PinnedConceptController pinnedConceptController;
    public SpeechToTextController speechToTextController;
    public bool useLocalServer;

    private string _localIpAddress, _serverIpAddress;

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
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                _localIpAddress = ip.ToString();
            }
        }

        localIpDisplay.text = "HOST: " + _localIpAddress;

        if (useLocalServer)
            SetupServer();
    }

    // ==================== Server Side ==================== 

    // Create a server and listen on a port
    private void SetupServer()
    {
        Debug.Log("[SERVER] Setting up a local server listening at " + UDP_PORT);
        NetworkServer.Listen(UDP_PORT);
        NetworkServer.RegisterHandler(MyMsgType.MSG_CLIENT_CONNECTED, OnConnectedClientReport);
    }

    // After the partner connects to this device, you have to
    // build make your client connect back.
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

    // Used to test whether the clients can receive a message.
    public void TestServerToClient()
    {
        var testMsg = new StringMessage();
        StringMessage stringMessage = new StringMessage
        {
            value = "[SERVER] Hello I'm Server at " + _localIpAddress
        };
        BroadcastToConnected(MyMsgType.MSG_TEST, stringMessage);

        StringMessage testNewConcepts = new StringMessage
        {
            value = "C1,C2"
        };
        BroadcastToConnected(MyMsgType.MSG_NEW_KEYWORDS, testNewConcepts);
    }

    public void BroadcasetNewKeywords(string[] keywords)
    {
        BroadcastToConnected(MyMsgType.MSG_NEW_KEYWORDS, new StringMessage
        {
            value = string.Join(",", keywords)
        });
    }

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

    private void BroadcastToConnected(short msgType, MessageBase msg)
    {
        NetworkServer.SendToAll(msgType, msg);
    }

    // ==================== Client Side ==================== 

    // Create a client and connect to the server port
    private void SetupClient(string partnerIP)
    {
        Debug.Log("[CLIENT] Trying to connect to the partner at " + partnerIP);
        clientToPartner = new NetworkClient();
        clientToPartner.RegisterHandler(MsgType.Connect, OnConnected);
        clientToPartner.RegisterHandler(MyMsgType.MSG_TEST, OnTestMsgArrived);
        clientToPartner.RegisterHandler(MyMsgType.MSG_NEW_KEYWORDS, OnReceivedNewKeywords);
        clientToPartner.RegisterHandler(MyMsgType.MSG_PINNED_KEYWORDS, OnReceivedPinnedKeywords);
        clientToPartner.RegisterHandler(MyMsgType.MSG_TRANS, OnReceiveTranscript);
        clientToPartner.Connect(partnerIP, UDP_PORT);
    }

    // Client OnConnected callback
    private void OnConnected(NetworkMessage netMsg)
    {
        _serverIpAddress = clientToPartner.serverIp;
        // Disable the IP input field
        partnerIPAddress.interactable = false;

        Debug.Log("[CLIENT] Connected to the server at " + _serverIpAddress);
        testMsgText.text = "Connected to " + _serverIpAddress;

        // Tell the server you received the message.
        clientToPartner.Send(MyMsgType.MSG_CLIENT_CONNECTED, new StringMessage
        {
            value = "[CLIENT] Client report"
        });
    }

    // Used to test the connection from server to clients
    private void OnTestMsgArrived(NetworkMessage netMsg)
    {
        if (netMsg != null)
        {
            var msg = netMsg.ReadMessage<StringMessage>();
            Debug.Log("[CLIENT] Test message arrived: " + msg.value);
            testMsgText.text = msg.value;
        }
    }

    private void OnReceiveTranscript(NetworkMessage netMsg)
    {
        string transcripts = netMsg.ReadMessage<StringMessage>().value;
        speechToTextController.SaveTranscript(transcripts.Split('|'));
        speechToTextController.UpdateVis();
    }

    private void OnReceivedNewKeywords(NetworkMessage netMsg)
    {
        // Add to the panel
        var newKeywordsMsg = netMsg.ReadMessage<StringMessage>();
        foreach (String kw in newKeywordsMsg.value.Split(','))
        {
            pinnedConceptController.AddNewConcept(new ConceptData(kw));
        }
    }

    private void OnReceivedPinnedKeywords(NetworkMessage netMsg)
    {

    }
}
