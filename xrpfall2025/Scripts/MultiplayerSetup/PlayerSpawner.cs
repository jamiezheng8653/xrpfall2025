using Godot;
using System;

public partial class PlayerSpawner : Node
{
	private PackedScene playerPrefab = ResourceLoader.Load<PackedScene>("res://Scenes/Prefabs/player_spawner.tscn");

	public override void _Ready()
	{
		NetworkHandler.Instance.OnPeerConnected += SpawnPlayer;
		ClientNetworkGlobals.Instance.HandleLocalIDAssignment += SpawnPlayer;
		ClientNetworkGlobals.Instance.HandleRemoteIDAssignment += SpawnPlayer;

	}
	
	private void SpawnPlayer(int id)
    {
		Player player = (Player)playerPrefab.Instantiate();
		player.OwnerID = id;
		player.Name = id.ToString();

		CallDeferred("AddChild", player);
    }
}
