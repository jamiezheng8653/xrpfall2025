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

	//for the steering demo, will be perm if it works
	private float wheelBase = 70; //distance from front to rear wheel
	private float steeringAngle = 15; //amt front wheel turns in degrees
	//private Vector3 velocity = Vector3.Zero;
	private float steerAngle = 0; //current
	private float enginePower = 800; //forward acceleration force
	private Vector3 acceleration = Vector3.Zero;
	private float friction = -0.9f;
	private float drag = -0.0015f;
	private float braking = -450;
	private float maxSpeedReverse = 250;
	private float slipSpeed = 400; //speed where traction is required;
	private float tractionFast = 0.1f; //high-speed traction
	private float tractionSlow = 0.7f; //low-speed traction

	//change of state. may update this when we hash out 
	// how we want to track player state upon item collision
	[Export]
	public States Current
	{
		get { return current; }
		set { current = value; }
	}

	public override void _PhysicsProcess(double delta)
	{
		/* //Adjust left and right steering 
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

		if (direction != Vector3.Zero)
		{
			direction = direction.Normalized();
			// Setting the basis property will affect the rotation of the node.
			GetNode<Node3D>("Pivot").Basis = Basis.LookingAt(direction);
		}

		// Ground velocity
		_targetVelocity.X = direction.X * speed;
		_targetVelocity.Z = direction.Z * speed;

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
		MoveAndSlide(); */
		acceleration = Vector3.Zero;
		GetInput();
		ApplyFriction();
		CalcSteeringForces(delta);
		Velocity += acceleration * (float)delta;
		MoveAndSlide();
	}

	private void ApplyFriction()
	{
		if (Velocity.Length() < 5) Velocity = Vector3.Zero;

		Vector3 frictionForce = Velocity * friction;

		Vector3 dragForce = Velocity * Velocity.Length() * drag;

		if (Velocity.Length() < 100) frictionForce *= 3;

		acceleration += dragForce + frictionForce;
	}

	private void GetInput()
	{
		float turn = 0;
		if (Input.IsActionPressed("right")) turn++;
		if (Input.IsActionPressed("left")) turn--;

		steerAngle = turn * Mathf.DegToRad(steeringAngle);
		Velocity = Vector3.Zero;

		if (Input.IsActionPressed("accelerate")) Velocity = Transform.X * enginePower;

		if(Input.IsActionPressed("brake")) acceleration = Transform.X * braking;
	}

	//implementing algorithm from https://engineeringdotnet.blogspot.com/2010/04/simple-2d-car-physics-in-games.html
	//will clean up if this proves to be successful
	private void CalcSteeringForces(double delta)
	{
		/* 		Vector3 carLocation = Vector3.Zero;
				float carHeading = 5;
				float carSpeed = 10;
				float steerAngle = 15;
				float wheelBase = 10; //the distance between the two axles
				Vector3 frontWheel = carLocation + wheelBase / 2 * new Vector3(Mathf.Cos(carHeading), 0, Mathf.Sin(carHeading));
				Vector3 backWheel = carLocation - wheelBase / 2 * new Vector3(Mathf.Cos(carHeading), 0, Mathf.Sin(carHeading));

				backWheel += carSpeed * delta * new Vector3(Mathf.Cos(carHeading), 0, Mathf.Sin(carHeading));
				frontWheel += carSpeed * delta * new Vector3(Mathf.Cos(carHeading + steerAngle), 0, Mathf.Sin(carHeading + steerAngle));

				carLocation = (frontWheel + backWheel) / 2;
				carHeading = Mathf.Atan2(frontWheel.Y - backWheel.Y, frontWheel.X - backWheel.X); */

		Vector3 rearWheel = Position - Transform.X * wheelBase / 2;
		Vector3 frontWheel = Position + Transform.X * wheelBase / 2;
		rearWheel += Velocity * (float)delta;
		frontWheel += Velocity.Rotated(steerAngle) * delta;
		Vector3 newHeading = (frontWheel - rearWheel).Normalized();
		float traction = tractionSlow;
		if (Velocity.Length() > slipSpeed) traction = tractionFast;
		float d = newHeading.Dot(Velocity.Normalized());
		if (d > 0) Velocity = Velocity.Lerp(newHeading * Velocity.Length(), traction); 
		if (d < 0) Velocity = -newHeading * MinLengthAttribute(Velocity.Length(), maxSpeedReverse);
		Rotation = newHeading.Angle();
	}
}
