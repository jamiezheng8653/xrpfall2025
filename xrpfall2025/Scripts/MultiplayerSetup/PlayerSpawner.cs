using Godot;

/// <summary>
/// Handles spawning players per client connected to the server
/// </summary>
public partial class PlayerSpawner : Node
{
	//player scene to be instantiated
	private PackedScene playerPrefab = ResourceLoader.Load<PackedScene>("res://Scenes/Prefabs/player.tscn");

	public override void _Ready()
	{
		NetworkHandler.Instance.OnPeerConnected += SpawnPlayer;
		ClientNetworkGlobals.Instance.HandleLocalIDAssignment += SpawnPlayer;
		ClientNetworkGlobals.Instance.HandleRemoteIDAssignment += SpawnPlayer;

	}

	/// <summary>
	/// When a new client connects to the server, create 
	/// a player instance and assign the passed in id to them
	/// </summary>
	/// <param name="id">Id of the player being added to the scene</param>
	private void SpawnPlayer(int id)
	{
		Player player = (Player)playerPrefab.Instantiate();
		player.OwnerID = id;
		player.Name = id.ToString();
		this.CallDeferred(Node.MethodName.AddChild, player);
		PlayerList.Instance.List.Add(player);
	}
}
