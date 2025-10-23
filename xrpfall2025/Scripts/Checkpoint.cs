using Godot;
using System;

public partial class Checkpoint : Node
{
	public event CheckpointCollisionDelegate OnCheckpointCollision;
	private float radius;
	[Export] private Area3D area3d;
	private Player player;
	private Checkpoint self;
	private Color color;

	public Vector3 Position { get { return area3d.Position; } }
	public Vector3 GlobalPosition {get { return area3d.GlobalPosition; }}

	public Checkpoint()
	{
		player = null;
		self = this;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		DebugDraw3D.DrawSphere(GlobalPosition, radius, color);
		if (OnCollision()) OnCheckpointCollision?.Invoke(this);
	}

	/// <summary>
	/// Set this checkpoint's spawn location and the range of contact
	/// </summary>
	/// <param name="spawnPos">where the checkpoint is located, should be in the middle of the track</param>
	/// <param name="player">Reference to the player for collision checks</param>
	/// <param name="radius">Should be at least the half length of the track's width</param>
	public void Init(Vector3 spawnPos, Player player, float radius = 1)
	{
		color = new Color("YELLOW");
		area3d.Position = spawnPos;
		this.radius = radius;
		this.player = player;
	}

	/// <summary>
	/// Performs a circle collision check on if the player is overlapping with the checkpoint
	/// </summary>
	/// <returns>If the player is colliding with the checkpoint, return true. Otherwise false</returns>
	public bool OnCollision()
	{
		if (Utils.CircleCollision(GlobalPosition, radius, player.GlobalPosition, player.Radius)) return true;
		else return false;
	}
}
