using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

public partial class NetworkHandler : Node
{
	public delegate void OnPeerConnectedDelegate(int peerID);
	public delegate void OnPeerDisconnectedDelegate(int peerID);
	public delegate void OnServerPacketDelegate(int peerInt, byte[] data);
	public delegate void OnConnectedToServerDelegate();
	public delegate void OnDisconnectedFromServerDelegate();
	public delegate void OnClientPacketDelegate(byte[] data);

	//Server signals
	public event OnPeerConnectedDelegate OnPeerConnected;
	public event OnPeerDisconnectedDelegate OnPeerDisconnected;
	public event OnServerPacketDelegate OnServerPacket;

	//client signals
	public event OnConnectedToServerDelegate OnConnectedToServer;
	public event OnDisconnectedFromServerDelegate OnDisconnectedFromServer;
	public event OnClientPacketDelegate OnClientPacket;

	//server variables
	//start 255, stop at -1, increment down
	//or start at 0, stop at 255
	//private Godot.Collections.Array availablePeerIDs = Enumerable.Range(0, 256);
	private Stack<int> availablePeerIDs = new Stack<int>();
	private Dictionary<int, ENetPacketPeer> clientPeers;

	//client variables
	private ENetPacketPeer serverPeer;

	//general variables
	private ENetConnection connection;
	private bool isServer = false;

	//static reference to self for autoload
	public static NetworkHandler Instance { get; private set; }

	public bool IsServer
	{
		get { return isServer; }
	}

	public ENetConnection Connection
    {
        get { return connection; }
    }

    public override void _Ready()
    {
		Instance = this;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (connection == null) return;
		HandleEvents();
	}

	private void HandleEvents()
	{
		Godot.Collections.Array packetEvent = connection.Service();
		ENetConnection.EventType eventType = packetEvent[0].As<ENetConnection.EventType>();

		while (eventType != ENetConnection.EventType.None)
		{
			ENetPacketPeer peer = (ENetPacketPeer)packetEvent[1];

			switch (eventType)
			{
				case ENetConnection.EventType.Error:
					GD.PushWarning("Package resulted in an unknown error");
					return;
				case ENetConnection.EventType.Connect:
					if (isServer) PeerConnected(peer);
					else ConnectedToServer();
					break;
				case ENetConnection.EventType.Disconnect:
					if (isServer) PeerDisconnected(peer);
					else
					{
						DisconnectedFromServer();
						return;
					}
					break;
				case ENetConnection.EventType.Receive:
					if (isServer) OnServerPacket.Invoke((int)peer.GetMeta("id"), peer.GetPacket());
					else OnClientPacket.Invoke(peer.GetPacket());
					break;
			}

			//call service() again to handle remaining packets in current while in loop
			packetEvent = connection.Service();
			eventType = packetEvent[0].As<ENetConnection.EventType>();
		}
	}

	public void StartServer(String ipAddress = "127.0.0.1", int port = 55555)
	{
		connection = new ENetConnection();
		Error error = connection.CreateHostBound(ipAddress, port);

		if (error != Error.Ok) //exists
		{
			GD.Print("Server starting failed: ", error);
			connection = null;
			return;
		}
		GD.Print("Server Started");
		isServer = true;
	}

	private void PeerConnected(ENetPacketPeer peer)
	{
		int peerID = availablePeerIDs.Pop();
		peer.SetMeta("id", peerID);
		clientPeers[peerID] = peer;

		GD.Print("Peer connected with assigned id: ", peerID);
		OnPeerConnected.Invoke(peerID);
	}

	private void PeerDisconnected(ENetPacketPeer peer)
	{
		int peerID = (int)peer.GetMeta("id");
		availablePeerIDs.Push(peerID);
		clientPeers.Remove(peerID);

		GD.Print("Successfully disconnected: ", peerID, " from server");
		OnPeerDisconnected.Invoke(peerID);
	}

	public void StartClient(String ipAddress = "127.0.0.1", int port = 5555)
	{
		connection = new ENetConnection();
		Error error = connection.CreateHost(1);

		if (error != Error.Ok)
		{
			GD.Print("Client starting failed: ", error);
			connection = null;
			return;
		}

		GD.Print("Client Started");
		serverPeer = connection.ConnectToHost(ipAddress, port);
	}

	public void DisconnectClient()
	{
		if (isServer) return;
		serverPeer.PeerDisconnect();
	}

	private void ConnectedToServer()
	{
		GD.Print("Successfully connected to server");
		OnConnectedToServer.Invoke();
	}

	private void DisconnectedFromServer()
	{
		GD.Print("Successfully disconnected from server");
		OnDisconnectedFromServer.Invoke();
		connection = null;
	}
}
