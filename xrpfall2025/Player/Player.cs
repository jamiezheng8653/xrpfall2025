using Godot;

public partial class Player : CharacterBody3D
{
	// How fast the player moves in meters per second.
	[Export]
	public int Speed { get; set; } = 14;
	// The downward acceleration when in the air, in meters per second squared.
	[Export]
	public int FallAcceleration { get; set; } = 75;
	
	private Vector3 _targetVelocity = Vector3.Zero;

	public override void _PhysicsProcess(double delta)
	{
		var direction = Vector3.Zero;
		
		if (Input.IsActionPressed("right"))
		{
			direction.X += 1.0f;
			//GD.Print("Pressed D right");
		}
		if (Input.IsActionPressed("left"))
		{
			direction.X -= 1.0f;
			//GD.Print("Pressed A left");
		}
		if (Input.IsActionPressed("back"))
		{
			direction.Z += 1.0f;
			//GD.Print("Pressed S back");
		}
		if (Input.IsActionPressed("forward"))
		{
			direction.Z -= 1.0f;
			//GD.Print("Pressed W forward");
		}

		if (direction != Vector3.Zero)
		{
			direction = direction.Normalized();
			// Setting the basis property will affect the rotation of the node.
			GetNode<Node3D>("Pivot").Basis = Basis.LookingAt(direction);
		}

		// Ground velocity
		_targetVelocity.X = direction.X * Speed;
		_targetVelocity.Z = direction.Z * Speed;

		// Vertical velocity
		if (!IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			_targetVelocity.Y -= FallAcceleration * (float)delta;
			//GD.Print("I'm falling!");
		}

		// Moving the character
		Velocity = _targetVelocity;
		MoveAndSlide();
	}
}
