using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class HostBroadcastServer : MonoBehaviour
{
    private const string HOST_SERVER = "10.218.106.151:8082";
    private const int UDP_PORT = 8082;  // For local server

    private List<int> connections;

    private static class MyMsgType
    {
        public static short MSG_TEST = MsgType.Highest + 1;
        public static short MSG_CLIENT_CONNECTED = MsgType.Highest + 2;
        public static short MSG_NEW_KEYWORDS = MsgType.Highest + 3;
        public static short MSG_PINNED_KEYWORDS = MsgType.Highest + 4;
        public static short MSG_TRANS = MsgType.Highest + 5;
        public static short MSG_TOPIC_CTRL = MsgType.Highest + 6;
        public static short MSG_AVATAR_CTRL = MsgType.Highest + 7;
    }

    void Start()
    {
        connections = new List<int>();
        SetupServer();
    }

    // ==================== Server Side ==================== 

    // Create a server and listen on a port
    private void SetupServer()
    {
        Debug.Log("Setting up a local server listening at " + UDP_PORT);
        NetworkServer.RegisterHandler(MyMsgType.MSG_TRANS, OnReceivedBroadcastRequest);
        NetworkServer.RegisterHandler(MyMsgType.MSG_NEW_KEYWORDS, OnReceivedBroadcastRequest);
        NetworkServer.RegisterHandler(MyMsgType.MSG_TOPIC_CTRL, OnReceivedBroadcastRequest);
        NetworkServer.RegisterHandler(MyMsgType.MSG_AVATAR_CTRL, OnReceivedBroadcastRequest);
        NetworkServer.RegisterHandler(MyMsgType.MSG_CLIENT_CONNECTED, OnReceivedFirstHello);
        NetworkServer.RegisterHandler(MsgType.Disconnect, OnClientDisconned);
        NetworkServer.Listen(UDP_PORT);
    }

    private void OnClientDisconned(NetworkMessage netMsg)
    {
        Debug.Log("A client disconned from the server " + netMsg.conn.connectionId);
        if (connections.IndexOf(netMsg.conn.connectionId) > 0)
        {
            connections.Remove(netMsg.conn.connectionId);
        }
    }

    private void OnReceivedFirstHello(NetworkMessage netMsg)
    {
        Debug.Log("Received a new connection from " + netMsg.conn.connectionId + " at " + netMsg.conn.address);
        if (!connections.Contains(netMsg.conn.connectionId))
        {
            if (connections.Count > 4)
            {
                connections.RemoveAt(0);
            }
            connections.Add(netMsg.conn.connectionId);
        }

        Debug.Log("Greeting: " + netMsg.ReadMessage<StringMessage>().value);
    }

    private void OnReceivedBroadcastRequest(NetworkMessage netMsg)
    {
        // Broadcast the message to all the connected devices except the sender
        
        foreach (var c in connections)
        {
            if (c != netMsg.conn.connectionId)
            {
                Debug.Log("Forwarding the msg from " + netMsg.conn.connectionId + " to " + c);
                NetworkServer.SendToClient(c, netMsg.msgType, netMsg.ReadMessage<StringMessage>());
            }
        }
    }
}
