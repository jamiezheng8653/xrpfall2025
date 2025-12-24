using Godot;
using System.Collections.Generic;

/// <summary>
/// Class to define a singular checkpoint
/// Checkpoint manager is in charge of instantiating all checkpoints
/// </summary>
public partial class Checkpoint : Node
{
	public event CheckpointCollisionDelegate OnCheckpointCollision;
	//roughly the distance a car has to be within this checkpoint to be marked as passed
	private float radius;
	[Export] private Area3D area3d;
	//reference to all cars so a checkpoint will know which car passed them
	private List<Car> cars;
	//used to pass information along to the car of which checkpoint the car passed
	private Checkpoint self;
	private Color color;

	public Vector3 Position { get { return area3d.Position; } }
	public Vector3 GlobalPosition {get { return area3d.GlobalPosition; }}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//DebugDraw3D.DrawSphere(GlobalPosition, radius, color);
		foreach (Car c in cars)
		{
			if (OnCollision(c)) 
			{
				//if (c is Player p) OnCheckpointCollision?.Invoke(this, p);
				//else if (c is EnemyAi e) OnCheckpointCollision?.Invoke(this,e);
				//else OnCheckpointCollision?.Invoke(this,c);
				OnCheckpointCollision?.Invoke(this,c);
			}
		}
		
	}

	/// <summary>
	/// Set this checkpoint's spawn location and the range of contact
	/// </summary>
	/// <param name="spawnPos">where the checkpoint is located, should be in the middle of the track</param>
	/// <param name="player">Reference to the player for collision checks</param>
	/// <param name="radius">Should be at least the half length of the track's width</param>
	public void Init(Vector3 spawnPos, List<Car> cars, float radius = 1)
	{
		color = new Color("YELLOW");
		area3d.Position = spawnPos;
		this.radius = radius;
		this.cars = cars;
		self = this;
	}

	/// <summary>
	/// Performs a circle collision check on if the player is overlapping with the checkpoint
	/// </summary>
	/// <returns>If the player is colliding with the checkpoint, return true. Otherwise false</returns>
	public bool OnCollision(Car c)
	{
		if (Utils.CircleCollision(GlobalPosition, radius, c.GlobalPosition, c.Radius)) return true;
		else return false;
	}
}
