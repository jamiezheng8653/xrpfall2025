using Godot;
using System.Collections.Generic;

/// <summary>
/// Autoload of the client side globals. 
/// Call ClientNetworkGlobals.Instance when trying to call
/// </summary>
public partial class ClientNetworkGlobals : Node
{
	public delegate void HandleLocalIDAssignmentDelegate(int peerID);
	public delegate void HandleRemoteIDAssignmentDelegate(int peerID);
	public delegate void HandlePlayerPositionDelegate(PlayerPosition playerPosition);

	public event HandleLocalIDAssignmentDelegate HandleLocalIDAssignment;
	public event HandleRemoteIDAssignmentDelegate HandleRemoteIDAssignment;
	public event HandlePlayerPositionDelegate HandlePlayerPosition;

	//set to -1 to indicate that there is no id set initially
	private int id = -1;
	//list of all the remote clients' ids
	private List<int> remoteIDs;
	//singleton instance
	public static ClientNetworkGlobals Instance { get; private set; }

	/// <summary>
	/// the ID of the client
	/// </summary>
	public int ID
	{
		get { return id; }
	}

	/// <summary>
	/// the client's reference of all other remote clients' ids.
	/// </summary>
	public List<int> RemoteIDs
	{
		get { return remoteIDs; }
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		///hook up appropriate signal with cooresponding method
		NetworkHandler.Instance.OnClientPacket += OnClientPacket;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="data"></param>
	private void OnClientPacket(byte[] data)
	{
		//decode the first byte to figure out what type of packet we are recieving
		int packetType = data[0];

		//handle each packet type accordingly
		switch (packetType)
		{
			//
			case (int)PacketInfo.PACKETTYPE.IDASSIGNMENT:
				ManageIDs(IDAssignment.CreateFromData(data));
				break;

			case (int)PacketInfo.PACKETTYPE.PLAYERPOSITION:
				HandlePlayerPosition?.Invoke(PlayerPosition.CreateFromData(data));
				break;

			default:
				GD.PushError("Packet type with index ", data[0], " unhandled");
				break;
		}
	}

	/// <summary>
	/// Pass in an IDAssignment packet. If we the client do not already 
	/// have an id assigned, assume the passed in packet's information is ours.
	/// Otherwise save the passed in packet's id into the remote id list
	/// </summary>
	/// <param name="idAssignment">IDAssignment packet</param>
	private void ManageIDs(IDAssignment idAssignment)
	{
		// check if there is an id assigned
		if (id == -1)
		{
			//if not, then assign the id and emit that the local id has been assigned to self
			id = idAssignment.ID;
			HandleLocalIDAssignment?.Invoke(idAssignment.ID);
			//save the reference of the list of remote clients' ids
			remoteIDs = idAssignment.RemotedIDS;
			foreach (int remoteID in remoteIDs)
			{
				//skip if our id comes up
				if (remoteID == id) continue;
				//otherwise handle necessary methods with each unique remote id
				HandleRemoteIDAssignment?.Invoke(remoteID);
			}
		}
		//otherwise add the passed in idAssignment packet's id to the list of stored remoteIDs
		else
		{
			remoteIDs.Add(idAssignment.ID);
			HandleRemoteIDAssignment?.Invoke(idAssignment.ID);
		}
	}
}
