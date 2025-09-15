//Code from https://docs.godotengine.org/en/4.4/getting_started/first_3d_game/03.player_movement_code.html
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design.Serialization;
using System.Numerics;
using Godot;
using Vector3 = Godot.Vector3;

//Enum States
public enum States
{
	Regular,
	Inverted,
	Slow,
	Fast
};

public partial class Player : CharacterBody3D
{
	//current state
	private States current = States.Regular;
	//if the player gets a speedup or speed debuff, 
	//multiply/divide _targetVelocity by cooresponding multiplier
	//has to be int because Vectors are made up by ints
	[Export] private int slowMultiplier = 2;

	[Export] private int fastMultiplier = 2;

	private Vector3 _targetVelocity = Vector3.Zero;
	private Vector3 direction = Vector3.Forward;
	private float maxSpeed = 30;

	// How fast the player moves in meters per second.
	private float speed = 0;
	private float speedIncrement = 0.25f;

	//Rotation speed
	[Export] private float rotationSpeed = 1.5f;
	private float rotationIncrement = Mathf.DegToRad(1); //per delta
	private float _rotationDirection;

	// The downward acceleration when in the air, in meters per second squared.
	[Export] private int fallAcceleration = 75;

	/// <summary>
	/// The current state of the player car. Get/Set
	/// Relevant when we eventually add items that modify
	/// the player's state (defined by enum State)
	/// </summary>
	public States Current
	{
		get { return current; }
		set { current = value; }
	}

	/// <summary>
	/// Called every frame on 
	/// </summary>
	/// <param name="delta"></param>
	public override void _PhysicsProcess(double delta)
	{
		GetInput();
		CalcSteeringForces(delta);

		// Vertical velocity
		if (!IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			_targetVelocity.Y -= fallAcceleration * (float)delta;
			//GD.Print("I'm falling!");
		}

		//check states
		switch (current)
		{
			//multiply speed
			case States.Fast:
				_targetVelocity.X *= fastMultiplier;
				_targetVelocity.Z *= fastMultiplier;
				break;
			//divide speed
			case States.Slow:
				_targetVelocity.X /= slowMultiplier;
				_targetVelocity.Z /= slowMultiplier;
				break;
			//flip flop controls
			case States.Inverted:
				_targetVelocity.X *= -1;
				_targetVelocity.Z *= -1;
				break;

				//regular has no change
		}

		// Moving the character
		Velocity = _targetVelocity;
		MoveAndSlide();
	}

	/// <summary>
	/// 
	/// </summary>
	private void ApplyFriction()
	{

	}

	/// <summary>
	/// 
	/// </summary>
	private void GetInput()
	{
		//Adjust left and right steering 
		//TODO: allow for one direction to turn full circle, 
		// currently only turns 90 degrees before not allowing more
		if (Input.IsActionPressed("right"))
		{
			direction.X += rotationIncrement;
			direction.Z -= rotationIncrement;
			//GD.Print("Pressed D right");
		}
		else if (Input.IsActionPressed("left"))
		{
			direction.X -= rotationIncrement;
			direction.Z += rotationIncrement;
			//GD.Print("Pressed A left");
		}
		//want the car to go backwards without 
		// changing its facing orientation
		// if (Input.IsActionJustPressed("back"))
		// {
		// 	//direction.Z += 1.0f;
		// 	speed *= -1;
		// 	//GD.Print("Pressed S back");
		// }

		//TODO: Properly accelerate
		if (Input.IsActionPressed("back"))
		{
			if (speed > -maxSpeed) speed -= speedIncrement;
		}
		if (Input.IsActionPressed("forward"))
		{
			//direction.Z -= 1.0f;
			if (speed < maxSpeed) speed += speedIncrement;
			//GD.Print("Pressed W forward");
		}
		if (Input.IsActionJustReleased("forward") || Input.IsActionJustReleased("back"))
		{
			//will eventually add friction to come to a gradual stop
			speed = 0;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="delta"></param>
	private void CalcSteeringForces(double delta)
	{
		if (direction != Vector3.Zero)
		{
			direction = direction.Normalized();
			// Setting the basis property will affect the rotation of the node.
			GetNode<Node3D>("Pivot").Basis = Basis.LookingAt(direction);
		}

		// Ground velocity
		_targetVelocity.X = direction.X * speed;
		_targetVelocity.Z = direction.Z * speed;
	}
}
