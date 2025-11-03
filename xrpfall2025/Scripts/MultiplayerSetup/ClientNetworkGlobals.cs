using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ClientNetworkGlobals : Node
{
	public delegate void HandleLocalIDAssignmentDelegate(int localID);
	public delegate void HandleRemoteIDAssignmentDelegate(int remoteID);
	public delegate void HandlePlayerPositionDelegate(PlayerPosition playerPosition);

	public event HandleLocalIDAssignmentDelegate HandleLocalIDAssignment;
	public event HandleRemoteIDAssignmentDelegate HandleRemoteIDAssignment;
	public event HandlePlayerPositionDelegate HandlePlayerPosition;

	private int id = -1;
	private List<int> remoteIDs;

	public static ClientNetworkGlobals Instance {get; private set;}

	public int ID
	{
		get { return id; }
	}

	public List<int> RemoteIDs
	{
		get { return remoteIDs; }
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		NetworkHandler.Instance.OnClientPacket += OnClientPacket;
	}

	private void OnClientPacket(byte[] data)
	{
		//int packetType = data.DecodeU8(0);
		int packetType = data[0];

		switch (packetType)
		{
			case (int)PacketInfo.PACKETTYPE.IDASSIGNMENT:
				ManageIDs(IDAssignment.CreateFromData(data));
				break;

			case (int)PacketInfo.PACKETTYPE.PLAYERPOSITION:
				HandlePlayerPosition.Invoke(PlayerPosition.CreateFromData(data));
				break;

			default:
				GD.PushError("Packet type with index ", data[0], " unhandled");
				break;
		}
	}

	private void ManageIDs(IDAssignment idAssignment)
	{
		if (id == -1)
		{
			id = idAssignment.ID;
			HandleLocalIDAssignment.Invoke(idAssignment.ID);
			remoteIDs = idAssignment.RemotedIDS;
			foreach (int remoteID in remoteIDs)
			{
				if (remoteID == id) continue;
				HandleRemoteIDAssignment.Invoke(remoteID);
			}
		}

		else
		{
			//remoteIDs.Append<IDAssignment>(idAssignment.ID);
			remoteIDs.Add(idAssignment.ID);
			HandleRemoteIDAssignment.Invoke(idAssignment.ID);
		}
	}
}
