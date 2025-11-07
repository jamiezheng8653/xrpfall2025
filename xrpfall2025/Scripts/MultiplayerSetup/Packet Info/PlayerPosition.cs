using Godot;
using System;

/// <summary>
/// Packet for sending a client/player's position in game to others
/// </summary>
public class PlayerPosition : PacketInfo
{
	//the id of the player
	private int id;
	//the player's position 
	private Vector3 position;

	/// <summary>
	/// The ID of the player/client
	/// </summary>
	public int ID
	{
		get { return id; }
	}

	/// <summary>
	/// The in game global position of the player/client
	/// </summary>
	public Vector3 Position
	{
		get { return position; }
	}

	/// <summary>
	/// Create a packet to send of the player's position
	/// </summary>
	/// <param name="idParam">ID specific to a client</param>
	/// <param name="positionParam">The client's current in-game global position</param>
	/// <returns>Packet of the passed in information, ready to be sent</returns>
	public static PlayerPosition Create(int idParam, Vector3 positionParam)
	{
		PlayerPosition info = new()
		{
			packetType = PACKETTYPE.PLAYERPOSITION,
			//unsequenced so the packet behaves like a UDP packet (fast)
			flag = (int)ENetPacketPeer.FlagUnsequenced,
			id = idParam,
			position = positionParam
		};
		return info;
	}

	/// <summary>
	/// Creates a packet for the player position given a data byte[]
	/// </summary>
	/// <param name="data">Assumes data[0] denotes packet type, 
	/// data[1] id number, and data[2, 3, 4] are position's x, y, and z components</param>
	/// <returns>The PlayerPosition packet of the passed in data</returns>
	public static PlayerPosition CreateFromData(byte[] data)
	{
		PlayerPosition info = new();
		info.Decode(data);
		return info;
	}

	/// <summary>
	/// Writes the id and position information of the packet 
	/// into a data[] with the purpose to be sent to another.
	/// </summary>
	/// <returns>a data byte[] ready to be sent</returns>
	protected override byte[] Encode()
	{
		byte[] data = base.Encode();
		//data.Resize(10);
		Array.Resize(ref data, 14);
		//data.EncodeU8(1, id);
		data[1] = (byte)id;
		//each float of a vector3 takes four bytes to store, 
		// hence we jump in increments of four
		data[2] = (byte)position.X;
		data[6] = (byte)position.Y;
		data[10] = (byte)position.Z;
		return data;
	}

	/// <summary>
    /// Takes the passed in data[] to overwrite this packet's stored id and position data
    /// </summary>
    /// <param name="data">data from another that we are storing. 
	/// Assumes data[0] denotes packet type, data[1] id number, 
	/// and data[2, 3, 4] are position's x, y, and z components</param>
	protected override void Decode(byte[] data)
	{
		base.Decode(data);
		//id = data.DecodeU8(1);
		id = data[1];
		position = new Vector3(data[2], data[6], data[10]);
	}
}
