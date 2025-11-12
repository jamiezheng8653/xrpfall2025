using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Vector3 = Godot.Vector3;
using System.IO;
using System.Text.Json;


public partial class Player : Car
{
	/// <summary>
	/// Initialize any values upon load. If you're initializing this 
	/// through another script, please utilize the Init() method.
	/// Currently no Init() method is in place, if we choose to 
	/// programmatically load all objects in, be sure to implement Init()
	/// </summary>
	public override void _Ready()
	{
		color = new Godot.Color("CYAN");
		current = States.Regular;
		timer = new Stopwatch();
		prevPosition = new Vector3();
		pathOfFalling = new List<Vector3>();
		halflength = new Vector3(radius, radius, radius); //TODO
		
		// For customization, not yet applied
		carMesh = GetNode<MeshInstance3D>("CollisionShape3D/XRP_Car/SM_Car");
		ApplySavedCarColor();
		
	}

	/// <summary>
	/// Any logic to ensure no memory leaks (particularly with any events)
	/// when this Player leaves the scene tree during runtime for whatever reason
	/// </summary>
	public override void _ExitTree()
	{
	}

	/// <summary>
	/// General game logic being run every frame.
	/// If you're modifying anything relating to the Player's physics,
	/// please use _PhysicsProcess()
	/// </summary>
	/// <param name="delta">delta time</param>
	public override void _Process(double delta)
	{
		//draw gizmos
		//DebugDraw3D.DrawBox(AABB.Position, Godot.Quaternion.Identity, Vector3.One, color);
		//GD.Print("Player State: " + Current);

		//closest point
		/*DebugDraw3D.DrawBox(
			GetClosestAbsolutePosition(track, GlobalPosition),
			Godot.Quaternion.Identity,
			Vector3.One,
			color
			);*/

		//check the timer
		//GD.Print("Player Timer: " + timer.ElapsedMilliseconds);

		//when collision event, run rng and start stopwatch. unsubscribe collision event and start stopwatch event
		//after certain time, resubscribe collision event, reset stop watch.
	}

	/// <summary>
	/// Called every frame to process how this object will move by what
	/// Will rotate this object left or right, move forwards or backwards locally
	/// and fall if necessary
	/// </summary>
	/// <param name="delta">delta time</param>
	public override void _PhysicsProcess(double delta)
	{
		//Adjust left and right steering
		if (((Input.IsActionPressed("right") && current != States.Inverted)
			|| (Input.IsActionPressed("left") && current == States.Inverted)) && charbody3d.IsOnFloor())
		{
			charbody3d.RotateObjectLocal(new Vector3(0, 1, 0), -(float)rotationIncrement);
		}
		else if (((Input.IsActionPressed("left") && current != States.Inverted)
			|| (Input.IsActionPressed("right") && current == States.Inverted)) && charbody3d.IsOnFloor())
		{
			charbody3d.RotateObjectLocal(new Vector3(0, 1, 0), (float)rotationIncrement);
		}

		//accelerate in forward or backward direction
		if (Input.IsActionPressed("back") && charbody3d.IsOnFloor())
		{
			if (speed > -maxSpeed) speed -= acceleration * delta;
		}
		else if (Input.IsActionPressed("forward") && charbody3d.IsOnFloor())
		{
			if (speed < maxSpeed) speed += acceleration * delta;
		}
		else
		{
			//will eventually add friction to come to a gradual stop
			speed *= 0.9 * delta;
		}

		// Vertical velocity
		if (!charbody3d.IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			//keep adding position before gravity is implemented until impact with kill plane
			pathOfFalling.Add(charbody3d.GlobalPosition);
			charbody3d.Position -= charbody3d.GetTransform().Basis.Y * (float)(delta * fallAcceleration);
		}
		else
		{
			pathOfFalling.Clear();
			//GD.Print("Clearing list!");
		}

		if (timer.ElapsedMilliseconds > 0)
		{
			RevertState(current, speed, delta);
		}

		// Moving the character
		charbody3d.Position += charbody3d.GetTransform().Basis.Z * (float)(delta * UpdateStateSpeed(current, speed)) * -1;

		charbody3d.MoveAndSlide();
		//GD.Print("Player speed: " + speed);
	}
	
	//Customization file reading, saves the color to be applied to the car
	protected override void ApplySavedCarColor()
	{
		string path = ProjectSettings.GlobalizePath("user://customization.json");
		if (!File.Exists(path))
		{
			GD.Print("No customization file found.");
			return;
		}
		else 
		{
			GD.Print("File Found.");
		}

		string json = File.ReadAllText(path);
		var data = JsonSerializer.Deserialize<CustomizationData>(json);

		if (data != null && !string.IsNullOrEmpty(data.CarColor))
		{
			Color color = new Color(data.CarColor); // Godot parses "#RRGGBB"
			
			if (color != null){
				GD.Print(color);
			}
			
			SetCarColor(color);
		}
	}
	
	// Sets the material of the car to the proper color
	protected override void SetCarColor(Color color)
	{
		if (carMesh == null)
		{
			GD.PrintErr("Car mesh not found!");
			return;
		}

		var mat = carMesh.GetActiveMaterial(0) as StandardMaterial3D;
		if (mat != null)
		{
			var newMat = (StandardMaterial3D)mat.Duplicate();
			newMat.AlbedoColor = color;
			carMesh.SetSurfaceOverrideMaterial(0, newMat);
		}
		else
		{
			var newMat = new StandardMaterial3D();
			newMat.AlbedoColor = color;
			carMesh.MaterialOverride = newMat;
		}
	}

	private class CustomizationData
	{
		[System.Text.Json.Serialization.JsonPropertyName("car_color")]
		public string CarColor { get; set; }
	}
	 
}
