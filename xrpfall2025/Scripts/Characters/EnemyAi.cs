using Godot;
using System.Windows;
using System.Numerics;
using Vector3 = Godot.Vector3;

/// <summary>
/// Class for Enemy cars. Inherits from parent Car class. 
/// Only difference between enemy and player is how movement
/// is handled and preset decision making
/// </summary>
public partial class EnemyAi : Car
{
	/// <summary>
	/// Different driving modes the Enemy AI would be in
	/// </summary>
	private enum RacingState
	{
		NormalDriving,
		Overtake,
		Attack,
		Defend,
		Recover

	}

	private RacingState racingState = RacingState.NormalDriving;
	private Vector3 prevPoint;
	private Vector3 seekPoint;
	private int trackPointIndex = 1;
	private float mass = 10;

	public override void Init(Vector3 startingPosition, Path3D track, int totalCheckpoints)
	{
		base.Init(startingPosition, track, totalCheckpoints);
		prevPoint = startingPosition;
		seekPoint = track.Curve.GetPointPosition(1);
		racingState = RacingState.NormalDriving;
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}


	public override void _PhysicsProcess(double delta)
	{
		//get input based on racing state behavior
		switch (racingState)
		{
			case RacingState.NormalDriving: 
				NormalDriving(delta); 
				break;
			case RacingState.Overtake: 
				Overtake(delta); 
				break;
			case RacingState.Attack: 
				Attack(delta); 
				break;
			case RacingState.Defend: 
				Defend(delta); 
				break;
			case RacingState.Recover: 
				Recover(delta); 
				break;
		}

		//process input
		base._PhysicsProcess(delta);
	}


	private void NormalDriving(double delta)
	{
		//rotate to look at the next node 
		charbody3d.RotateObjectLocal(
			new Vector3(0, 1, 0), 
			Utils.AngleBetween(charbody3d.Velocity, Seek(seekPoint, 1))
			);
		//move towards the node
		if (speed > maxSpeed) speed += acceleration * delta;
		//once at the node, update your node tracker to the next consecutuve node
		if (Utils.CircleCollision(GlobalPosition, 1.5, seekPoint, 3))
		{
			trackPointIndex++;
			if (trackPointIndex >= track.Curve.PointCount) trackPointIndex = 0;
			prevPoint = seekPoint;
			seekPoint = track.Curve.GetPointPosition(trackPointIndex);
		}
		//apply to movement -> call in base.physicsprocess
	}

	/// <summary>
	/// When the car is at a bend and a car is in front of this car, attempt a tight angle
	/// </summary>
	/// <param name="delta"></param>
	private void Overtake(double delta)
	{
		
	}

	/// <summary>
	/// If the car is holding a projectile item and a car is ahead, attempt to hit
	/// </summary>
	/// <param name="delta"></param>
	private void Attack(double delta)
	{
		
	}

	/// <summary>
	/// If a car is behind and attempting to overtaking this car, try to block the incoming car
	/// </summary>
	/// <param name="dfelta"></param>
	private void Defend(double dfelta)
	{
		
	}

	/// <summary>
	/// If the car has veered significantly off course, find its way back to the main track
	/// </summary>
	/// <param name="delta"></param>
	private void Recover(double delta)
	{
		
	}

	/// <summary>
	/// Calculate a steering force for if this enemy wants to get somewhere
	/// </summary>
	/// <param name="targetPos">Where we are seeking</param>
	/// <param name="weight">How aggressively are we seeking</param>
	/// <returns>Steering force vector</returns>
	private Vector3 Seek(Vector3 targetPos, float weight)
	{
		// Calculate desired velocity
		Vector3 desiredVelocity = targetPos - charbody3d.GlobalPosition;

		// Set desired = max speed
		desiredVelocity = desiredVelocity.Normalized() * (float)maxSpeed;

		// Calculate seek steering force
		Vector3 seekingForce = desiredVelocity - charbody3d.Velocity;

		// Return seek steering force
		return seekingForce * weight;
	}

}
