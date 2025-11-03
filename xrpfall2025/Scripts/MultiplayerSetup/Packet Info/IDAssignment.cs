using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class IDAssignment : PacketInfo
{
	private int id;
	private List<int> remotedIDs;

	public int ID
	{
		get { return id; }
		set { id = value; }
	}

	public List<int> RemotedIDS
	{
		get { return remotedIDs; }
		set { remotedIDs = value; }
	}

	public static IDAssignment Create(int idParam, List<int> remoteIDsParam)
	{
		IDAssignment info = new IDAssignment();
		info.packetType = PACKETTYPE.IDASSIGNMENT;
		info.flag = (int)ENetPacketPeer.FlagReliable;
		info.id = idParam;
		info.remotedIDs = remoteIDsParam;
		return info;
	}

	public static IDAssignment CreateFromData(byte[] data)
	{
		IDAssignment info = new IDAssignment();
		info.Decode(data);
		return info;
	}

	private byte[] Encode()
	{
		byte[] data = base.Encode();
		Array.Resize(ref data, 2 + remotedIDs.Count);
		//data.EncodeU8(1, id);
		data[1] = (byte)id;
		for (int i = 0; i < remotedIDs.Count; i++)
		{
			int idTemp = remotedIDs[i];
			//data.EncodeU8(2 + i, idTemp);
			data[2 + i] = (byte)idTemp;
		}
		return data;
	}

	private void Decode(byte[] data)
	{
		base.Decode(data);
		//id = data.DecodeU8(1);
		id = data[1];
		foreach (int i in Enumerable.Range(2, data.Length))
		{
			//remotedIDs.Append<>(data.EncodeU8(i));
			//remotedIDs.Append<byte>(data[i]);
			remotedIDs.Add(data[i]);
		}
	}
}
