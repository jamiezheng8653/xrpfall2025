using Godot;
using System;
using System.Collections.Generic;

public delegate void OnKillPlaneDelegate(Car c);
public partial class KillPlane : Node
{
	public event OnKillPlaneDelegate IsCollidingKillPlane;
	//private Player playerP = null;
	private List<Car> cars;
	[Export] private CsgBox3D planeBox = null;
	private Color color;

	public Aabb AABB
	{
		get
		{
			//move the aabb to the actual object's transform
			//otherwise the aabb sits in the origin
			Aabb temp = planeBox.GlobalTransform * planeBox.GetAabb();
			temp.Size = planeBox.GlobalTransform.Basis.Scale;
			return temp;
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		color = new Color("RED");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		foreach(Car c in cars)
		{
			if (IsColliding(c))
			{
				//GD.Print("I'm colliding!");
				//fire event
				IsCollidingKillPlane?.Invoke(c);
			}
		}
		

		//draw aabb 
		DebugDraw3D.DrawAabb(AABB, color);

	}

	/// <summary>
	/// Initializes the object. Associates any external references that this object needs
	/// </summary>
	/// <param name="player"></param>
	public void Init(/*Player player*/List<Car> cars)
	{
		//playerP = player;
		this.cars = cars;
	}

	/// <summary>
	/// Checks if the player is colliding with the kill plane
	/// </summary>
	/// <returns>True if the player is colliding with the kill plane</returns>
	private bool IsColliding(Car c)
	{
		// we don't need to adjust the position of the kill plane's aabb 
		// because the kill plane is never going to move while the game is running
		return /*playerP*/c.AABB.Intersects(AABB);
	}
}
