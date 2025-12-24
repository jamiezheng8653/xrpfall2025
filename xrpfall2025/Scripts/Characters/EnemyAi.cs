using System.Diagnostics;
using Godot;
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

	[Export] RayCast3D raycast3d;
	private RacingState racingState = RacingState.NormalDriving;
	private Vector3 prevPoint;
	private Vector3 seekPoint;
	private int trackPointIndex = 1;
	private float mass = 10;

	/// <summary>
	/// Initialize a Enemy AI 
	/// </summary>
	/// <param name="startingPosition">Where are we first spawning</param>
	/// <param name="track">Reference to the track's path</param>
	/// <param name="totalCheckpoints">How many checkpoints are on the track</param>
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

	// Handle any in-game movement and effecting physics on this EnemyAI body
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

	/// <summary>
	/// Mimic a car just driving along the path with 
	/// no other intentions like sabotoge or defense
	/// </summary>
	/// <param name="delta">Deltatime</param>
	private void NormalDriving(double delta)
	{
		DebugDraw3D.DrawSphere(seekPoint, 2, color);
		//need to get the correct angle for the car to turn
		Vector3 targetVector = (seekPoint - GlobalPosition).Normalized();
		float angle = -charbody3d.Transform.Basis.Z.SignedAngleTo(targetVector, Vector3.Up);
		GD.Print("EnemyAI angle: ", angle);
		rotationIncrement = angle/45;
		rotationIncrement = Mathf.Clamp(rotationIncrement, -1, 1);

		//rotate to look at the next node 
		charbody3d.RotateObjectLocal(
			new Vector3(0, 1, 0), 
			(float)rotationIncrement
		);

		// //if the angle between our forward direction and where the enemy is seeking is between 0 and 80, turn left
		// if (angle > Mathf.DegToRad(-10) && angle <= Mathf.DegToRad(-90))
		// {
		// 	charbody3d.RotateObjectLocal(
		// 		new Vector3(0, 1, 0), 
		// 		(float)rotationIncrement
		// 	);
		// }
		// //angle is between 100 to 180 degrees, turn right
		// else if (angle > Mathf.DegToRad(90) && angle <= Mathf.DegToRad(10))
		// {
		// 	charbody3d.RotateObjectLocal(
		// 		new Vector3(0, 1, 0), 
		// 		-(float)rotationIncrement
		// 	);
		// }
		
		// //move towards the node when the car is facing the node well enough
		//if (raycast3d.CollideWithAreas) speed *= 0.9 * delta; 
		if (speed < maxSpeed 
			// && angle <= Mathf.DegToRad(170) && angle >= Mathf.DegToRad(190)
			// || angle <= Mathf.DegToRad(-190) && angle >= Mathf.DegToRad(-170)
			// || angle <= Mathf.DegToRad(-10) && angle >= Mathf.DegToRad(10)
		) 
		{
			speed += acceleration * delta;
		}
		
		//once at the node, update your node tracker to the next consecutuve node
		if (Utils.CircleCollision(GlobalPosition, 5, seekPoint, 2))
		{
			trackPointIndex++;
			//ensure our index for the track loop is valid
			if (trackPointIndex >= track.Curve.PointCount) trackPointIndex = 1;
			//update the point the enemy is tracking.
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
	private void Defend(double delta)
	{
		
	}

	/// <summary>
	/// If the car has veered significantly off course, find its way back to the main track
	/// </summary>
	/// <param name="delta"></param>
	private void Recover(double delta)
	{
		
	}

	// /// <summary>
	// /// Calculate a steering force for if this enemy wants to get somewhere
	// /// </summary>
	// /// <param name="targetPos">Where we are seeking</param>
	// /// <param name="weight">How aggressively are we seeking</param>
	// /// <returns>Steering force vector</returns>
	// private Vector3 Seek(Vector3 targetPos, float weight)
	// {
	// 	// Calculate desired velocity
	// 	Vector3 desiredVelocity = targetPos - charbody3d.GlobalPosition;

	// 	// Set desired = max speed
	// 	desiredVelocity = desiredVelocity.Normalized() * (float)maxSpeed;

	// 	// Calculate seek steering force
	// 	Vector3 seekingForce = desiredVelocity - charbody3d.Velocity;

	// 	//DebugDraw3D.DrawArrow(GlobalPosition, seekingForce, color);

	// 	// Return seek steering force
	// 	return seekingForce * weight;
	// }

}
