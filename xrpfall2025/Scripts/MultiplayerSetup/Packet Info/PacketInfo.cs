using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;

public class PacketInfo 
{
	public enum PACKETTYPE
	{
		IDASSIGNMENT = 0,
		PLAYERPOSITION = 10,
	}

	protected PACKETTYPE packetType;
	protected int flag;

	protected byte[] Encode()
	{
		byte[] data = [];
		Array.Resize(ref data, 1);
		//data.EncodeU8(0, packetType);
		data[0] = (byte)packetType;
		return data;
	}

	protected void Decode(byte[] data)
	{
		//packetType = (PACKETTYPE)data.DecodeU8(0);
		packetType = (PACKETTYPE)data[0];
	}

	private void Send(ENetPacketPeer target)
	{
		if (target == null) return;
		target.Send(0, Encode(), flag);
	}

	public void Broadcast(ENetConnection server)
	{
		server.Broadcast(0, Encode(), flag);
	}
}
