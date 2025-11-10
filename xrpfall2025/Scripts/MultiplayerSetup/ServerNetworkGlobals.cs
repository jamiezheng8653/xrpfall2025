using Godot;
using System.Collections.Generic;

/// <summary>
/// Server side globals 
/// </summary>
public partial class ServerNetworkGlobals : Node
{
	public delegate void HandlePlayerPositionDelegate(int peerID, PlayerPosition playerPosition);
	public event HandlePlayerPositionDelegate HandlePlayerPosition;

	// List of all peer ids connected to the server
	private List<int> peerIDs = new();

	//singleton instance
	public static ServerNetworkGlobals Instance { get; private set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		NetworkHandler.Instance.OnPeerConnected += OnPeerConnected;
		NetworkHandler.Instance.OnPeerDisconnected += OnPeerDisconnected;
		NetworkHandler.Instance.OnServerPacket += OnServerPacket;
	}

	/// <summary>
	/// When a new client peer connects to the server, 
	/// save its peer ID, make the appropriate IDAssignment packet, 
	/// and broadcast the information to the ENet Connection
	/// </summary>
	/// <param name="peerID">ID of the peer that joined</param>
	private void OnPeerConnected(int peerIDNew)
	{
		//peerIDs.Append<>(peerID);
		peerIDs.Add(peerIDNew);
		IDAssignment.Create(peerIDNew, peerIDs).Broadcast(NetworkHandler.Instance.Connection);
	}

	/// <summary>
	/// When a peer disconnects, remove its id from the list of connected clients
	/// </summary>
	/// <param name="peerID">The id of the client disconnecting from the server</param>
	private void OnPeerDisconnected(int peerID)
	{
		peerIDs.Remove(peerID);
		//create IDAssignment to broadcase to all peers
	}

	/// <summary>
	/// Process any packets the server recieves
	/// </summary>
	/// <param name="peerID">Who is sending the packet to the server</param>
	/// <param name="data">The packet information</param>
	private void OnServerPacket(int peerID, byte[] data)
	{
		//int packetType = data.DecodeU8(0);
		int packetType = data[0];

		switch (packetType)
		{
			//server only sends ID information, cannot recieve them. 
			// Hence only processing passed in player positions
			case (int)PacketInfo.PACKETTYPE.PLAYERPOSITION:
				HandlePlayerPosition?.Invoke(peerID, PlayerPosition.CreateFromData(data));
				break;

			default:
				GD.PushError("Packet type with index ", data[0], " unhandled");
				break;
		}
	}
}
