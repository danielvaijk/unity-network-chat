using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using System.Collections;
using System.Collections.Generic;

// Coded by Daniel van Dijk @ 2015 (last edited in 12/11/2015).
// Thank you for purchasing this package and supporting me. Visit http://www.vaijk.com.

public class UpdatedNetwork : NetworkManager
{
    public int serverCapacity;

    public GameObject offlineUI;
    public GameObject onlineUI;

    public Text chatTextComponent;

    private int linesOfText = 0;

    // Local variables.
    private string playerName = "Player";
    private string myMessage = "Send a message!";
    private string chatText = string.Empty;
    private string myColor = string.Empty;

    private bool isServer = false;

    private NetworkClient myNetworkClient = null;

    private InputField networkIPInput = null;
    private InputField networkPortInput = null;
    private InputField messageInput = null;
    private InputField playerNameInput = null;

    private List<ChatPlayer> players = new List<ChatPlayer>();

    // Represents Player information within the Chat Network. (Used by Server only).
    private class ChatPlayer : MessageBase
    {
        public string name;
        public string color;

        public int connectionID;
    }

    // Represents a Chat Message.
    private class ChatMessage : MessageBase
    {
        public string senderName;
        public string senderColor;
        public string message;

        public bool isNotification;

        public ChatMessage ()
        {

        }

        public ChatMessage (string name, string color, string chatMessage, bool isNote)
        {
            senderName = name;
            senderColor = color;
            message = chatMessage;
            isNotification = isNote;
        }
    }

    // Contains the message ID's for our custom messages.
    private class MyMsgType
    {
        public static short ChatMessage = MsgType.Highest + 1;
        public static short RegisterPlayer = MsgType.Highest + 2;
    }

    // Called when the "Host" UI button is clicked.
    public void HostServer ()
    {
        // Use <networkPort> to listen for incomming connections.
        NetworkServer.Listen(networkPort);

        // Register the handlers on the NetworkServer.
        NetworkServer.RegisterHandler(MsgType.Connect, OnServerConnect);
        NetworkServer.RegisterHandler(MsgType.Disconnect, OnServerDisconnect);
        NetworkServer.RegisterHandler(MyMsgType.ChatMessage, ForwardChatMessage);
        NetworkServer.RegisterHandler(MyMsgType.RegisterPlayer, RegisterNewPlayer);

        // Connect to the local Server and register the handlers on the local NetworkClient.
        myNetworkClient = ClientScene.ConnectLocalServer();
        myNetworkClient.RegisterHandler(MsgType.Connect, OnClientConnect);

        isServer = true;
    }

    // Called when the "Connect" UI button is clicked.
    public void ConnectToServer ()
    {
        myNetworkClient = new NetworkClient();
        myNetworkClient.RegisterHandler(MsgType.Connect, OnClientConnect);

        myNetworkClient.Connect(networkAddress, networkPort);
    }

    // Called when the "Send" UI button is clicked.
    public void SendMessage ()
    {
        SendChatMessage(playerName, myColor, myMessage, false);

        // Reset the current message input and select it so we can write again.
        messageInput.text = string.Empty;
        messageInput.ActivateInputField();
    }

    // Called on the Server when a Client has connected to the Server.
    public void OnServerConnect (NetworkMessage networkMessage)
    {
        Debug.Log("A Client has connected to the Server: " + networkMessage.conn.connectionId);
    }

    // Called on the Server when a Client has disconnect from the Server.
    public void OnServerDisconnect (NetworkMessage networkMessage)
    {
        Debug.Log("A Client has disconnected from the Server: " + networkMessage.conn.connectionId);

        foreach (ChatPlayer player in players)
        {
            if (player.connectionID == networkMessage.conn.connectionId)
            {
                SendChatMessage(player.name, player.color, "has left the Server.", true);
                break;
            }
        }
    }

    // Called on Client when he has connected to the Server.
    public void OnClientConnect (NetworkMessage networkMessage)
    {
        Debug.Log("Succesfully connected to Server.");

        // Change the UI.
        offlineUI.SetActive(false);
        onlineUI.SetActive(true);

        myNetworkClient.RegisterHandler(MyMsgType.ChatMessage, OnReceiveChatMessage);

        ChatPlayer myInfo = new ChatPlayer();

        myInfo.name = playerName;
        myInfo.color = myColor;

        myNetworkClient.Send(MyMsgType.RegisterPlayer, myInfo);

        // Send a notification message over the Network.
        SendChatMessage(playerName, myColor, "has joined the Server.", true);
    }

    // Called on the Server when a Client sends his Player information.
    public void RegisterNewPlayer (NetworkMessage networkMessage)
    {
        ChatPlayer newPlayer = networkMessage.ReadMessage<ChatPlayer>();

        newPlayer.connectionID = networkMessage.conn.connectionId;

        players.Add(newPlayer);
    }

    // Called on the Server when a Client sends a chat message.
    // Used to forward Network Messages. [Client -> Server -> Everyone]
    private void ForwardChatMessage (NetworkMessage networkMessage)
    {
        // Get the received Chat Message data from <networkMessage>.
        ChatMessage chatMessage = networkMessage.ReadMessage<ChatMessage>();

        // Send the Network Message to everyone (including ourself).
        NetworkServer.SendToAll(MyMsgType.ChatMessage, chatMessage);
    }

    // Called after the Network Chat Message has been received.
    private void OnReceiveChatMessage (NetworkMessage networkMessage)
    {
        ChatMessage chatMessage = networkMessage.ReadMessage<ChatMessage>();

        string notificationMessage = string.Format("<color=#{0}>{1}</color> {2}\n", chatMessage.senderColor, chatMessage.senderName, chatMessage.message);
        string normalMessage = string.Format("<color=#{0}>{1}</color>: {2}\n", chatMessage.senderColor, chatMessage.senderName, chatMessage.message);

        string newText = chatMessage.isNotification ? notificationMessage : normalMessage;

        for (int i = 1; i < (newText.Length / 85) + 1; i++)
        {
            newText = newText.Insert(85 * i, "\n");
            linesOfText++;
        }

        chatText += newText;
        chatTextComponent.text = chatText;

        linesOfText++;

        if (linesOfText >= 10)
        {
            onlineUI.transform.Find("Chat Scrollbar").gameObject.SetActive(true);
            onlineUI.transform.Find("Chat Background").Find("Chat Text").GetComponent<RectTransform>().
                sizeDelta += new Vector2(0f, 20f);
        }

        onlineUI.transform.Find("Chat Scrollbar").GetComponent<Scrollbar>().value = 0;
    }

    // Called when this script is enabled, before the Update() method.
    private void Start ()
    {
        messageInput = onlineUI.transform.Find("Message Field").GetComponent<InputField>();
        networkIPInput = offlineUI.transform.Find("Network IP").GetComponentInChildren<InputField>();
        networkPortInput = offlineUI.transform.Find("Network Port").GetComponentInChildren<InputField>();
        playerNameInput = offlineUI.transform.Find("Player Name").GetComponentInChildren<InputField>();

        // Choose a random color HEX for our player name.
        myColor = new string[]{ "D91E18", "AEA8D3", "26A65B", "E87E04" }[Random.Range(0, 3)];
    }

    // Called every frame of the game.
    private void Update ()
    {
        networkAddress = networkIPInput.text;
        networkPort = int.Parse(networkPortInput.text);
        playerName = playerNameInput.text;
        myMessage = messageInput.text;

        if (EventSystem.current.currentSelectedGameObject == messageInput.gameObject && messageInput.text == "Send a message!")
        {
            messageInput.text = string.Empty;
        }

        if (Input.GetKeyDown(KeyCode.Return) && myMessage.Length > 0)
        {
            SendMessage();
        }
    }

    // Takes care of sending message information over the Network.
    private void SendChatMessage (string senderName, string color, string message, bool isNotification)
    {
        ChatMessage newChatMessage = new ChatMessage(senderName, color, message, isNotification);

        if (isServer)
        {
            // [Server] Send this message to everyone, including myself.
            NetworkServer.SendToAll(MyMsgType.ChatMessage, newChatMessage);
        }
        else
        {
            // [Client] Send this message to the Server.
            myNetworkClient.Send(MyMsgType.ChatMessage, newChatMessage);
        }
    }
}