using UnityEngine;

using System.Collections;
using System.Collections.Generic;

#pragma warning disable 0618

// Coded by Daniel van Dijk @ 2015 (last edited in 12/11/2015).
// Thank you for purchasing this package and supporting me. Visit http://www.vaijk.com.

public class LegacyNetwork : MonoBehaviour
{
    public int serverCapacity;

    public GUISkin guiSkin;

    private string networkIP = "127.0.0.1";
    private string networkPort = "25000";
    private string guiSelection = "Network Menu";

    // Local variables.
    private string playerName = "Player";
    private string myMessage = "Send a message!";
    private string myColor = string.Empty;

    private bool canSendMessage = false;

    private Vector2 scrollPosition = Vector2.zero;

    private List<ChatMessage> chatMessages = new List<ChatMessage>();

    private NetworkView thisNetworkView = null;

    // Represents a message in the chat.
    private struct ChatMessage
    {
        public string sender;
        public string text;

        public bool isNotification;
    }

    private void Start ()
    {
        thisNetworkView = GetComponent<NetworkView>();

        // Choose a random color HEX for our player name.
        myColor = new string[] { "D91E18", "AEA8D3", "26A65B", "E87E04" }[Random.Range(0, 3)];
    }

    private void Update ()
    {
        // If we have connected to a Server and the GUI has not changed, change it to the chat GUI.
        if ((Network.isServer || Network.isClient) && guiSelection != "Chat")
        {
            guiSelection = "Chat";
        }

        if (canSendMessage)
        {
            // We can only send messages if we have atleast 1 char of text.
            if (myMessage.Length > 0)
            {
                // Send our message information over the Network.
                thisNetworkView.RPC("SendChatMessage", RPCMode.AllBuffered, playerName, myMessage, false);

                // Reset our current message text.
                myMessage = string.Empty;
            }

            canSendMessage = false;
        }

        Rect textFieldRect = new Rect(Screen.width / 2 - 240, Screen.height / 2 + 120, 380, 25);
        Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        if (textFieldRect.Contains(mousePosition) && Input.GetMouseButtonDown(0))
        {
            if (myMessage.Length == 0)
                myMessage = string.Empty;
        }
    }

    private void OnGUI ()
    {
        GUI.skin = guiSkin;

        if (guiSelection == "Network Menu")
        {
            GUILayout.BeginArea(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 100, 200, 200));

            networkIP = GUILayout.TextField(networkIP);
            networkPort = GUILayout.TextField(networkPort);
            playerName = GUILayout.TextField(playerName);

            if (GUILayout.Button("Host"))
            {
                Network.InitializeServer(serverCapacity, int.Parse(networkPort), false);
            }

            if (GUILayout.Button("Connect"))
            {
                Network.Connect(networkIP, int.Parse(networkPort));
            }

            GUILayout.EndArea();
        }
        else if (guiSelection == "Chat")
        {
            GUI.Box(new Rect(Screen.width / 2 - 250, Screen.height / 2 - 150, 500, 300), "Chat Room");

            GUILayout.BeginArea(new Rect(Screen.width / 2 - 240, Screen.height / 2 - 100, 480, 200));

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            // Show all the messages we have stored in our <chatMessages> list.
            foreach (ChatMessage message in chatMessages)
            {
                if (message.isNotification)
                {
                    GUILayout.Label(string.Format("{0} {1}", message.sender, message.text));
                }
                else
                {
                    GUILayout.Label(string.Format("{0}: {1}", message.sender, message.text));
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Input field for the message to be written in.
            myMessage = GUI.TextField(new Rect(Screen.width / 2 - 240, Screen.height / 2 + 120, 380, 25), myMessage);

            // If we press the ENTER key or click on the SEND button, send a message.
            if (GUI.Button(new Rect(Screen.width / 2 + 145, Screen.height / 2 + 120, 100, 25), "Send")
                || Event.current.keyCode == KeyCode.Return)
            {
                // Send a message through the Update() method by using a bool.
                canSendMessage = true;
            }
        }
    }

    // Called on the Server when the Server has initialized.
    private void OnServerInitialized ()
    {
        // Add the random color to our name.
        playerName = string.Format("<color=#{0}>{1}</color>", myColor, playerName);

        // Send a notification message over the Network.
        thisNetworkView.RPC("SendChatMessage", RPCMode.AllBuffered, playerName, "has joined the Server.", true);
    }

    // Called on the Client when he has connected to the Server.
    private void OnConnectedToServer ()
    {
        // Add the random color to our name.
        playerName = string.Format("<color=#{0}>{1}</color>", myColor, playerName);

        // Send a notification message over the Network.
        thisNetworkView.RPC("SendChatMessage", RPCMode.AllBuffered, playerName, "has joined the Server.", true);
    }

    // Called locally when we have disconnected from the Server.
    private void OnDisconnectedFromServer ()
    {
        // Add the random color to our name.
        playerName = string.Format("<color=#{0}>{1}</color>", myColor, playerName);

        // Send a notification message over the Network.
        thisNetworkView.RPC("SendChatMessage", RPCMode.AllBuffered, playerName, "has left the Server.", true);
    }

    // Called locally when we are about to close the application.
    private void OnApplicationQuit ()
    {
        // Add the random color to our name.
        playerName = string.Format("<color=#{0}>{1}</color>", myColor, playerName);

        // Send a notification message over the Network.
        if (Network.isClient || Network.isServer)
            thisNetworkView.RPC("SendChatMessage", RPCMode.AllBuffered, playerName, "has left the Server.", true);
    }

    // Takes care of sending message information over the Network.
    [RPC]
    private void SendChatMessage (string senderName, string message, bool isNotification)
    {
        ChatMessage chatMessage = new ChatMessage();

        chatMessage.sender = senderName;
        chatMessage.text = message;
        chatMessage.isNotification = isNotification;

        chatMessages.Add(chatMessage);

        // Automatically scroll down for each new message received.
        scrollPosition += new Vector2(0f, 60f);
    }
}