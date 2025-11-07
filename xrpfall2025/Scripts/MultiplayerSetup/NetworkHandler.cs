using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles setting up the server and connecting clients
/// Singleton, reference NetworkHandler.Instance when calling 
/// </summary>
public partial class NetworkHandler : Node
{
	//Cooresponding delegates for the event signals below
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
	//the list of peerIDs (new clients that can join the server). Right now 
	// max number is set to 256, will likely lower it to 16 eventually
	//private Godot.Collections.Array availablePeerIDs = Enumerable.Range(0, 256);
	private Stack<int> availablePeerIDs = new(); //filled in ready()
	//All clients currently connected to the server paired with their unique peer ID
	private Dictionary<int, ENetPacketPeer> clientPeers = new();

	//client variables
	private ENetPacketPeer serverPeer;

	//general variables
	private ENetConnection connection;
	//
	private bool isServer = false;
	private string serverIP;
	private int port;

	//static reference to self for autoload
	public static NetworkHandler Instance { get; private set; }

	/// <summary>
    /// What is the port number the server is hosted on
    /// </summary>
	public int CurrentPort
	{
		get
		{
			//return connection.GetLocalPort(); 
			return port;
		}
	}

	/// <summary>
    /// What is the IP of the server
    /// </summary>
	public string ServerIP
	{
		get { return serverIP; }
	}

	/// <summary>
    /// Is the host the server or client. 
	/// Returns true if the host is the server
    /// </summary>
	public bool IsServer
	{
		get { return isServer; }
	}

	/// <summary>
    /// 
    /// </summary>
	public ENetConnection Connection
	{
		get { return connection; }
	}

	//Set up all 
	public override void _Ready()
	{
		Instance ??= this;
		//only populate if the stack is empty prior to loading
		if (availablePeerIDs.Count <= 0)
		{
			IEnumerable<int> numbers = Enumerable.Range(0, 255);
			foreach (int number in numbers)
			{
				availablePeerIDs.Push(number);
				GD.Print("Adding peerID: ", number);
			}
		}

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//if there is no server running/connected to, do not bother
		if (connection == null) return;
		//otherwise process all appropriate packet information
		HandleEvents();
	}

	/// <summary>
	/// Called constantly while there is a connection to a server
	/// Grabs events from the connection and process them accordingly
	/// </summary>
	private void HandleEvents()
	{
		//service returns an Array of size four containing types of 
		// EventType, ENetPacketPeer, any event associated data, and any event associated channel
		Godot.Collections.Array packetEvent = connection.Service();
		ENetConnection.EventType eventType = packetEvent[0].As<ENetConnection.EventType>();

		//while there is an event
		while (eventType != ENetConnection.EventType.None)
		{
			//grab the peer associated with the event
			ENetPacketPeer peer = (ENetPacketPeer)packetEvent[1];

			// GD.Print("peer packet ", peer.GetPacket().Length);
			// foreach(byte b in peer.GetPacket())
			// {
			// 	GD.Print(b);
			// }

			//handle the specific event type accordingly
			switch (eventType)
			{
				//report any warnings given
				case ENetConnection.EventType.Error:
					GD.PushWarning("Package resulted in an unknown error");
					return;

				//if we are the server, add the new peer to the list 
				// of clients connected and assign the peer a unique id
				//otherwise handle any events while you the peer are connected to the server
				case ENetConnection.EventType.Connect:
					if (isServer) PeerConnected(peer);
					else ConnectedToServer();
					break;

				//When attempting to disconnect 
				case ENetConnection.EventType.Disconnect:
					//if you are the server, remove the peer
					if (isServer) PeerDisconnected(peer);
					else
					{
						//otherwise handle anything on your client 
						// side to remove yourself from the server
						DisconnectedFromServer();
						return;
					}
					break;

				//Recieve any appropriate packets
				case ENetConnection.EventType.Receive:
					//if you are the sevrer, recieve 
					if (isServer) OnServerPacket?.Invoke((int)peer.GetMeta("id"), peer.GetPacket());
					else OnClientPacket?.Invoke(peer.GetPacket());
					break;
			}

			//call service() again to handle remaining packets in current while in loop
			packetEvent = connection.Service();
			eventType = packetEvent[0].As<ENetConnection.EventType>();
		}
	}

	/// <summary>
	/// Opens the server given an ip and port. If no ip or port is given, 
	/// the server will happen locally on the computer you are running the program on
	/// </summary>
	/// <param name="ipAddress">Where are you hosting the server</param>
	/// <param name="portP">What port are we hosting the server on</param>
	public void StartServer(String ipAddress = "127.0.0.1", int portP = 55555)
	{
		//Instantiate a new ENetConnection that will 
		// attempt to open the new server at the given ip and port
		connection = new ENetConnection();
		//error is to check if there were issues upon attempting to open server
		Error error = connection.CreateHostBound(ipAddress, portP);

		//If there is an error, set connection to null and 
		// write in the console what the error was
		if (error != Error.Ok) //exists
		{
			GD.Print("Server starting failed: ", error);
			connection = null;
			return;
		}

		//otherwise the server was successfully opened, save the IP 
		// of the server and change bool to indicate a server is running
		GD.Print("Server Started");
		serverIP = ipAddress;
		isServer = true;
		this.port = portP;
	}

	/// <summary>
	/// Call when a peer is first connecting to the server
	/// assigns the passed in peer a unique id and calls following methods
	/// </summary>
	/// <param name="peer">The peer that is first connecting to the server</param>
	private void PeerConnected(ENetPacketPeer peer)
	{
		//assign the first available id to the passed in peer and remove the id from the available-List
		int peerID = availablePeerIDs.Pop();
		peer.SetMeta("id", peerID);
		GD.Print(peerID, peer, clientPeers);
		//add the peer to the list of connected clients
		clientPeers[peerID] = peer;

		GD.Print("Peer connected with assigned id: ", peerID);
		//call methods associated to the appropriate event
		OnPeerConnected?.Invoke(peerID);
	}

	/// <summary>
	/// Remove a peer from the server
	/// Called from server side
	/// </summary>
	/// <param name="peer">The peer that is disconnecting from the server</param>
	private void PeerDisconnected(ENetPacketPeer peer)
	{
		//adds the id of the peer passed in back to the list of available 
		// peer ids and removes the peer from the connected client list
		int peerID = (int)peer.GetMeta("id");
		availablePeerIDs.Push(peerID);
		clientPeers.Remove(peerID);

		//handles consequent methods to disconnect the peer from the server completely
		GD.Print("Successfully disconnected: ", peerID, " from server");
		OnPeerDisconnected?.Invoke(peerID);
	}

	/// <summary>
	/// Call when a client is attempting to connect to the an existing server.
	/// </summary>
	/// <param name="ipAddress">What IP is the server hosted on</param>
	/// <param name="port">What is the local port of the server</param>
	public void StartClient(String ipAddress = "127.0.0.1", int port = 55555)
	{
		//create a new ENetConnection local to your computer
		connection = new ENetConnection();
		Error error = connection.CreateHost(1);

		//if there was an error in starting the host client to be 
		// connected to the server, null connection and report the error
		if (error != Error.Ok)
		{
			GD.Print("Client starting failed: ", error);
			connection = null;
			return;
		}

		//otherwise connect to the server
		GD.Print("Client Started");
		serverPeer = connection.ConnectToHost(ipAddress, port);
	}

	/// <summary>
	/// Call when you are the client attempting to disconnect from the server
	/// </summary>
	public void DisconnectClient()
	{
		if (isServer) return;
		//call the server peer to ask you the client to disconnect from the server,
		serverPeer.PeerDisconnect();
	}

	/// <summary>
	/// Calls any methods associated while the peer is connected to the server
	/// </summary>
	private void ConnectedToServer()
	{
		GD.Print("Successfully connected to server");
		OnConnectedToServer?.Invoke();
	}

	/// <summary>
    /// Disconnect yourself from the server
	/// Called on client side
    /// </summary>
	private void DisconnectedFromServer()
	{
		GD.Print("Successfully disconnected from server");
		OnDisconnectedFromServer?.Invoke();
		connection = null;
	}
}
