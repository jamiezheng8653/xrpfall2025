using Godot;
using System;

/// <summary>
/// Parent class that all packet information will inherit from
/// </summary>
public class PacketInfo
{
	/// <summary>
	/// Enum to title the packet on what information we are trying to send specifically
	/// </summary>
	public enum PACKETTYPE
	{
		IDASSIGNMENT = 0,
		PLAYERPOSITION = 10,
	}

	protected PACKETTYPE packetType;
	protected int flag;

	/// <summary>
	/// Sets up the data byte[]. this method will be called and overriden in consequent children
	/// data[0] is always information storing what type of packet this is
	/// </summary>
	/// <returns>the start of the data byte[]</returns>
	protected virtual byte[] Encode()
	{
		byte[] data = [];
		//reserve the first slot as it will always store the PACKETTYPE of this packet
		Array.Resize(ref data, 1);
		//data.EncodeU8(0, packetType);
		data[0] = (byte)packetType;
		return data;
	}

	/// <summary>
	/// sets the packetType of this packet to the data read from the first element of the passed in byte[]
	/// </summary>
	/// <param name="data">data packet from another peer</param>
	protected virtual void Decode(byte[] data)
	{
		//packetType = (PACKETTYPE)data.DecodeU8(0);
		packetType = (PACKETTYPE)data[0];
	}

	/// <summary>
	/// Call when you want to send this packet's data to the passed in target
	/// </summary>
	/// <param name="target">who do you want to send information to</param>
	private void Send(ENetPacketPeer target)
	{
		if (target == null) return;
		//param: channel (0), the packet we want to send, and this packet's flag
		target.Send(0, Encode(), flag);
	}

	/// <summary>
	/// Queue this packet's information to the passed in server connection
	/// </summary>
	/// <param name="server"></param>
	public void Broadcast(ENetConnection server)
	{
		server.Broadcast(0, Encode(), flag);
	}
}
