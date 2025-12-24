using Godot;
using System;
using System.Collections.Generic;

public delegate void OnKillPlaneDelegate(Car c);

/// <summary>
/// Handles what happens when a car falls off the track.
/// When the game is integrated with a the real XRP robot,
/// the use of the killplane will be unnecessary as recalibrating 
/// a off road car takes more time than is fun.
/// However for strictly game purposes, ensures a car will only be on the track
/// </summary>
public partial class KillPlane : Node
{
	public event OnKillPlaneDelegate IsCollidingKillPlane;
	private List<Car> cars;
	[Export] private CsgBox3D planeBox = null;
	private Color color;

	/// <summary>
	/// Axis aligned bounding box of the killing plane for collision detection
	/// </summary>
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
		//check to see if a car is colliding with a kill plane every frame
		//keeping in mind to consider more optimal checking times
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
	/// <param name="cars">List of all cars in the race</param>
	public void Init(List<Car> cars)
	{
		this.cars = cars;
	}

	/// <summary>
	/// Checks if the car is colliding with the kill plane
	/// </summary>
	/// <param name="c">Which car are we checking for collision</param>
	/// <returns>True if the car is colliding with the kill plane</returns>
	private bool IsColliding(Car c)
	{
		// we don't need to adjust the position of the kill plane's aabb 
		// because the kill plane is never going to move while the game is running
		return c.AABB.Intersects(AABB);
	}
}
