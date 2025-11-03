using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


public partial class ServerNetworkGlobals : Node
{
	public delegate void HandlePlayerPositionDelegate(int peerID, PlayerPosition playerPosition);
	public event HandlePlayerPositionDelegate HandlePlayerPosition;

	private List<int> peerIDs;

	public static ServerNetworkGlobals Instance { get; private set; }	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this; 
		NetworkHandler.Instance.OnPeerConnected += OnPeerConnected;
		NetworkHandler.Instance.OnPeerDisconnected += OnPeerDisconnected;
		NetworkHandler.Instance.OnServerPacket += OnServerPacket;
	}

	private void OnPeerConnected(int peerID)
	{
		//peerIDs.Append<>(peerID);
		peerIDs.Add(peerID);
		IDAssignment.Create(peerID, peerIDs).Broadcast(NetworkHandler.Instance.Connection);
	}

	private void OnPeerDisconnected(int peerID)
	{
		peerIDs.Remove(peerID);
		//create IDAssignment to broadcase to all peers
	}

	private void OnServerPacket(int peerID, byte[] data)
	{
		//int packetType = data.DecodeU8(0);
		int packetType = data[0];

		switch (packetType)
		{
			case (int)PacketInfo.PACKETTYPE.PLAYERPOSITION:
				HandlePlayerPosition.Invoke(peerID, PlayerPosition.CreateFromData(data));
				break;

			default:
				GD.PushError("Packet type with index ", data[0], " unhandled");
				break;
		}
	}
}
