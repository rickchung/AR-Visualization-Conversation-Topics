using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class UdpSocket : MonoBehaviour
{
    private const int UDP_PORT = 7777;

    public Text localIpDisplay;
    public InputField partnerIPAddress;
    public Text testMsgText;

    private string _localIpAddress, _serverIpAddress;

    NetworkClient clientToPartner;

    private static class MyMsgType
    {
        public static short MSG_TEST = MsgType.Highest + 1;
        public static short MSG_CLIENT_CONNECTED = MsgType.Highest + 2;
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
        NetworkServer.SendToAll(MyMsgType.MSG_TEST, stringMessage);
    }

    // ==================== Client Side ==================== 

    // Create a client and connect to the server port
    private void SetupClient(string partnerIP)
    {
        Debug.Log("[CLIENT] Trying to connect to the partner at " + partnerIP);
        clientToPartner = new NetworkClient();
        clientToPartner.RegisterHandler(MsgType.Connect, OnConnected);
        clientToPartner.RegisterHandler(MyMsgType.MSG_TEST, OnTestMsgArrived);
        clientToPartner.Connect(partnerIP, UDP_PORT);
    }

    // Client OnConnected callback
    private void OnConnected(NetworkMessage netMsg)
    {
        _serverIpAddress = clientToPartner.serverIp;
        // Disable the IP input field
        partnerIPAddress.interactable = false;

        Debug.Log("[CLIENT] Connected to the server at " + _serverIpAddress);

        // Tell the server you received the message.
        StringMessage stringMessage = new StringMessage
        {
            value = "[CLIENT] Client report"
        };

        clientToPartner.Send(MyMsgType.MSG_CLIENT_CONNECTED, stringMessage);
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
}
