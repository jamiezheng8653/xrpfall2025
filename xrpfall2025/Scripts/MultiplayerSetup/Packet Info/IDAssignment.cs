using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One of the packets to be sent along client and server side
/// IDAssignment identifies specifically which client is sending over what
/// </summary>
public class IDAssignment : PacketInfo
{
	//the id of the packet
	private int id;
	//ids of others(?), TODO: verify
	private List<int> remotedIDs = new List<int>();

	/// <summary>
	/// What is the id of this packet
	/// </summary>
	public int ID
	{
		get { return id; }
		set { id = value; }
	}

	/// <summary>
	/// List of other ids connected(?)
	/// TODO: Verify
	/// </summary>
	public List<int> RemotedIDS
	{
		get { return remotedIDs; }
		set { remotedIDs = value; }
	}

	/// <summary>
	/// Creates a new IDAssignment packet given a id and other ids
	/// </summary>
	/// <param name="idParam">Who is the packet from</param>
	/// <param name="remoteIDsParam">Who are we sending the packet to</param>
	/// <returns>The IDAssignment packet created</returns>
	public static IDAssignment Create(int idParam, List<int> remoteIDsParam)
	{
		IDAssignment info = new()
		{
			packetType = PACKETTYPE.IDASSIGNMENT,
			//make the packet behave like a TCP packet (always will arrive), useful for any 
			// important game updates. In our instance, assigning player ids
			flag = (int)ENetPacketPeer.FlagReliable,
			id = idParam,
			remotedIDs = remoteIDsParam
		};
		return info;
	}

	/// <summary>
	/// Given data byte[], create a IDAssignment packet of the passed in data
	/// </summary>
	/// <param name="data"></param>
	/// <returns>The IDAssignment Packet created</returns>
	public static IDAssignment CreateFromData(byte[] data)
	{
		IDAssignment info = new();
		info.Decode(data);
		return info;
	}

	/// <summary>
	/// Write all stored remote ids into an array and return it.
	/// data[1] is the id of this packet
	/// each remote id is stored in susequent slots
	/// </summary>
	/// <returns>The new data packet byte[]</returns>
	protected override byte[] Encode()
	{
		byte[] data = base.Encode();
		Array.Resize(ref data, 2 + remotedIDs.Count);
		data[1] = (byte)id;
		//start at data[2] because data[0] and data[1] are filled
		for (int i = 0; i < remotedIDs.Count; i++)
		{
			int idTemp = remotedIDs[i];
			//data.EncodeU8(2 + i, idTemp);
			data[2 + i] = (byte)idTemp;
		}
		return data;
	}

	/// <summary>
	/// Set the packetType and id fields of this packet from the passed in data byte[]
	/// </summary>
	/// <param name="data">The information (presumably) from another 
	/// packet sent to and stored in this packet</param>
	protected override void Decode(byte[] data)
	{
		base.Decode(data);
		id = data[1];
		if (data.Length > 2)
		{
			//foreach (int i in Enumerable.Range(2, data.Length))
			for (int i = 2; i < data.Length; i++)
			{
				GD.Print("Adding remote id ", data[i], " index ", i, " from ", id);
				remotedIDs.Add(data[i]);
			}
		}

	}
}
