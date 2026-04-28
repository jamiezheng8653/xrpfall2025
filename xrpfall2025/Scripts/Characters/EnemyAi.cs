using System;
using Godot;
using Vector3 = Godot.Vector3;

public enum Difficulty
{
	Easy,
	Medium,
	Hard
};

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

	//will be used to simulate vision for more sophisticated driving behaviors
	[Export] RayCast3D raycast3d;
	//track what the car's raycast is "seeing" currently
	private GodotObject collided; 
	
	//player AI is rubberbanding off of
	private Player rubberbandingTo; 
	private RacingState racingState = RacingState.NormalDriving;
	private Difficulty difficulty;
	//the last node the ai was tracking
	private Vector3 prevPoint;
	//the current node the ai is tracking
	private Vector3 seekPoint;
	//number cooresponds with the index of the point on the track the ai is tracking
	private int trackPointIndex = 1;

	// is this car going to rubberband to the player? 
	// currently assumes only one player in the scene, 
	// cooresponding logic will need to be modified if 
	// multiplayer ever gets implemented
	private bool isRubberbanding = false;

	/// <summary>
	/// Get/Set if this enemy ai car is rubberbanding to the player
	/// </summary>
	public bool IsRubberbanding 
	{ 
		get { return isRubberbanding;}
		set { isRubberbanding = value;}
	}

	/// <summary>
	/// Initialize a Enemy AI 
	/// </summary>
	/// <param name="startingPosition">Where are we first spawning</param>
	/// <param name="track">Reference to the track's path</param>
	/// <param name="totalCheckpoints">How many checkpoints are on the track</param>
	public void Init(Vector3 startingPosition, Path3D track, int totalCheckpoints, Difficulty difficulty = Difficulty.Medium)
	{
		base.Init(startingPosition, track, totalCheckpoints);
		prevPoint = startingPosition;
		seekPoint = track.Curve.GetPointPosition(1);
		racingState = RacingState.NormalDriving;
		this.difficulty = difficulty;
		SetSpeed(difficulty);
		raycast3d.Show();
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
	}

	// Handle any in-game movement and effecting physics on this EnemyAI body
	public override void _PhysicsProcess(double delta)
	{
		//car cannot move while the race has not started
		if (!raceStarted) return;
		// //get what the ray cast is looking at
		// if(raycast3d.IsColliding()) 
		// {
		// 	collided = raycast3d.GetCollider();
		// 	GD.Print("I'm seeing " + collided.Get(Name));
		// }

		// if rubberbanding is on, depending on the distance from 
		// the player in terms of placements and physical gap distance,
		// adjust speed accordingly. This boolean is adjusted in the 
		// Car manager. Not every car needs to be rubberbanding to the player
		if (isRubberbanding)
		{
			//dot product to determine whether the target is in front or behind
			float dot = charbody3d.GlobalTransform.Basis.Z.Dot(rubberbandingTo.GlobalPosition);
			//float angleToTarget = charbody3d.GlobalTransform.Basis.Z.SignedAngleTo(rubberbandingTo.GlobalPosition, Vector3.Forward);
			//if is significantly in front of the player, slow down a bit to close the gap
			if (dot > 0)
			{
				if (difficulty != Difficulty.Easy) difficulty--;
			}
			//if is significantly behind the player, speed up to close the gap
			else
			{
				if (difficulty != Difficulty.Hard) difficulty++;
			}

			isRubberbanding = false;
			rubberbandingTo = null;
		}

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
		//GD.Print("EnemyAI angle: ", angle);
		rotationIncrement = angle/45;
		rotationIncrement = Mathf.Clamp(rotationIncrement, -1, 1);

		//rotate to look at the next node 
		charbody3d.RotateObjectLocal(
			new Vector3(0, 1, 0), 
			(float)rotationIncrement
		);
		
		// //move towards the node when the car is facing the node well enough
		//if (raycast3d.CollideWithAreas) speed *= 0.9 * delta; 
		if (speed < maxSpeed ) 
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
	/// Only implement behavior if on hard
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
		//projectile items have not been implemented yet
	}

	/// <summary>
	/// If a car is behind and attempting to overtaking this car, try to block the incoming car
	/// Only implement behavior if on hard
	/// </summary>
	/// <param name="delta"></param>
	private void Defend(double delta)
	{
		
	}

	/// <summary>
	/// If the car has veered significantly off course, find its way back to the main track
	/// This behavior assumes there is no risk of falling off a track 
	/// </summary>
	/// <param name="delta"></param>
	private void Recover(double delta)
	{
		
	}

	/// <summary>
	/// adjusts the max speed of a ai car according to their set difficulty
	/// </summary>
	/// <param name="difficulty">what difficulty is the car set to</param>
	private void SetSpeed(Difficulty difficulty)
	{
		switch (difficulty)
		{
			case Difficulty.Easy:
			maxSpeed = 10;
			break;

			case Difficulty.Medium:
			maxSpeed = 20;
			break;

			case Difficulty.Hard:
			maxSpeed = 30;
			break;
		}
	}

	/// <summary>
	/// Sets the player the enemy ai is rubberbanding to
	/// </summary>
	/// <param name="p">Which player</param>
	public void IsRubberbandingTo(Player p)
	{
		rubberbandingTo = p;
	}

}
