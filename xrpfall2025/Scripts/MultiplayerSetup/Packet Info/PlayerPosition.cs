using Godot;
using System;

public class PlayerPosition : PacketInfo
{
	private int id;
	private Vector3 position;

	public int ID
	{
		get { return id; }
	}

	public Vector3 Position
	{
		get { return position; }
	}

	public static PlayerPosition Create(int idParam, Vector3 positionParam)
	{
		PlayerPosition info = new PlayerPosition();
		info.packetType = PACKETTYPE.PLAYERPOSITION;
		info.flag = (int)ENetPacketPeer.FlagUnsequenced;
		info.id = idParam;
		info.position = positionParam;
		return info;
	}

	public static PlayerPosition CreateFromData(byte[] data)
	{
		PlayerPosition info = new PlayerPosition();
		info.Decode(data);
		return info;
	}

	private byte[] Encode()
	{
		byte[] data = base.Encode();
		//data.Resize(10);
		Array.Resize(ref data, 12);
		//data.EncodeU8(1, id);
		data[1] = (byte)id;
		//data.EncodeFloat(2, position.X);
		data[2] = (byte)position.X;
		//data.EncodeFloat(6, position.Y);
		data[6] = (byte)position.Y;
		//data.EncodeFloat(9, position.Z); //size might have to be bigger
		data[10] = (byte)position.Z;
		return data;
	}

	private void Decode(byte[] data)
	{
		base.Decode(data);
		//id = data.DecodeU8(1);
		id = data[1];
		position = new Vector3(data[2], data[6], data[10]);
	}
}
