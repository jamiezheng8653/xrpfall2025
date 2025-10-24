using Godot;
using System;
using System.Collections.Generic;


public delegate void CheckpointCollisionDelegate(Checkpoint chpt);

public partial class CheckpointManager : Node
{
	private PackedScene checkpointPrefab = ResourceLoader.Load<PackedScene>("res://Scenes/Prefabs/checkpoint.tscn");
	private Player player;
	private List<Checkpoint> checkpointList = new List<Checkpoint>();

	public int TotalCheckpoints
	{
		get{ return checkpointList.Count; }
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	/// <summary>
	/// Initialize the references to players and checkpoint 
	/// positions based on track points and generate the checkpoints
	/// </summary>
	/// <param name="p">Reference to the player</param>
	/// <param name="checkpoints">List of points where we want to instantiate our checkpoints</param>
	public void Init(Player p, List<Vector3> checkpoints)
	{
		player = p;
		Checkpoint temp;

		for (int i = 0; i < checkpoints.Count; i++)
		{
			checkpointList.Add((Checkpoint)checkpointPrefab.Instantiate());
			AddChild(checkpointList[^1]);
			temp = checkpointList[i];
			temp.Init(checkpoints[i], player, 20);
			temp.OnCheckpointCollision += player.AddCheckpoint;
			GD.Print("making checkpoint: " + temp);
		}
	}
}
