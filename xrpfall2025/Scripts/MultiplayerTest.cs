using Godot;
using System;
using System.Data.Common;

public partial class MultiplayerTest : Node
{
	private ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
	[Export] private PackedScene playerPrefab = ResourceLoader.Load<PackedScene>("res://Scenes/Prefabs/player.tscn");
	/*@onready*/private Camera3D camera;
	 
	// Called when the node enters the scene tree for the first time.
/* 	public override void _Ready()
	{
		camera = null;
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	} */

	private void OnHostPressed()
	{
		peer.CreateServer(6789);
		Multiplayer.MultiplayerPeer = peer;
		//Multiplayer.PeerConnected += AddPlayer;
		AddPlayer(1);
		camera.Visible = false;
	}

	private void OnJoinPressed()
	{
		peer.CreateClient("127.0.0.1", 6789);
		Multiplayer.MultiplayerPeer = peer;
		camera.Visible = false;
	}

	private void AddPlayer(int id)
	{
		Player player = (Player)playerPrefab.Instantiate();
		player.Name = id.ToString();
		CallDeferred("AddChild", player);
	}

	private void ExitGame(int id)
	{
		//Multiplayer.PeerDisconnected += DeletePlayer;
		DeletePlayer(id);
	}

	private void DeletePlayer(int id)
	{
		Rpc("DeletePlayer", id);
	}
	
	
	/*@Rpc("AnyPeer", "CallLocal") private void DeletePlayer(int id)
	{
		GetNode(id.ToString()).QueueFree();
	}*/
}
