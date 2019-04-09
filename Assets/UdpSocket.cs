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
    public InputField partnerIpInput;

    private string _localIpAddress;

    NetworkClient clientToPartner;

    public static class MyMsgType
    {
        public static short MSG_TEST = MsgType.Highest + 1;
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
        localIpDisplay.text = _localIpAddress;

        SetupServer();
    }

    // Create a server and listen on a port
    public void SetupServer()
    {
        Debug.Log("[Server] Setting up a local server listening at " + UDP_PORT);
        NetworkServer.Listen(UDP_PORT);
    }

    // Create a client and connect to the server port
    public void SetupClient(string partnerIP)
    {
        Debug.Log("[Client] Trying to connect to the partner at " + partnerIP);
        clientToPartner = new NetworkClient();
        clientToPartner.RegisterHandler(MsgType.Connect, OnConnected);
        clientToPartner.RegisterHandler(MyMsgType.MSG_TEST, OnTestMsgArrived);
        clientToPartner.Connect(partnerIP, UDP_PORT);
    }

    // Client OnConnected callback
    public void OnConnected(NetworkMessage netMsg)
    {
        Debug.Log("[Client] Connected to the server at " + clientToPartner.serverIp);
    }

    public void OnTestMsgArrived(NetworkMessage netMsg)
    {
        if (netMsg != null)
        {
            var msg = netMsg.ReadMessage<StringMessage>();
            Debug.Log("[Client] Test message arrived: " + msg.value);
        }
    }

    public void TestServerToClient()
    {
        var testMsg = new StringMessage();
        StringMessage stringMessage = new StringMessage
        {
            value = "Hello I'm server. This is a test message"
        };
        NetworkServer.SendToAll(MyMsgType.MSG_TEST, stringMessage);
    }
}
