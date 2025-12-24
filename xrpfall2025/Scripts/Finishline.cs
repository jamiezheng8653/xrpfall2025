using Godot;
using System;
using System.Collections.Generic;

//upon collision from a direction call this event
public delegate void OnCrossing(Car c);
public delegate void OnRaceFinished();

/// <summary>
/// Handles when cars cross the finish line,
///  verifies the car made valid laps 
/// </summary>
public partial class Finishline : Node
{
	public event OnCrossing OnCrossingEvent;
	public event OnRaceFinished OnRaceFinishedEvent;
	[Export] private Area3D area3D;
	private List<Car> cars;
	private int numPlayersFinished = 0;
	private int numPlayers = 0;
	private bool prevCollisionState = false;
	[Export] private CollisionShape3D collisionShape3D;
	private Vector3 halflength;
	private float radius = 5;
	private Color color;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//this is temporary
		color = new Color("BLUE");
		halflength = new Vector3(radius * 2, 1, radius);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//check for when overlap and the direction of the velocity vector of the player points correctly
		//this will have to be revamped when enemy ai cars get implemented
		foreach (Car c in cars)
		{
			if (IsOverlapping(c) && HitAllCheckpoints(c)) OnCrossingEvent?.Invoke(c); //call event
		}

		//check for if all players have completed the race. 
		// Finish race with current placements of all cars
		if (numPlayersFinished >= numPlayers) OnRaceFinishedEvent?.Invoke();

		
		//DebugDraw3D.DrawBox(area3D.GlobalPosition, Quaternion.Identity, 2 * halflength, color, true);
	}

	/// <summary>
	/// Initialize the external fields of the finshline.
	/// </summary>
	/// <param name="p">Reference the player in scene</param>
	/// <param name="startPt">Where the track first begins generating, where the finish line will be placed</param>
	/// <param name="scale">How big is the track, and therefore how wide do we need the track</param>
	public void Init(List<Car> cars, Vector3 startPt, double scale = 1)
	{
		this.cars = cars;
		area3D.Position = startPt + new Vector3(0, 1.5f, 0);

		//ensure the finishline takes up the width of the road
		// set the "forward" direction vector to match the flow of the track

	}

	/// <summary>
	/// Checks if the player is crossing the finish line with an aabb check
	/// </summary>
	/// <param name="c">Which car are we checking that passed the finished line</param>
	/// <returns>If the car is going over the finishline, return true</returns>
	private bool IsOverlapping(Car c)
	{
		if (Utils.AABBCollision(c.GlobalPosition, c.Halflength, area3D.GlobalPosition, halflength))
		{
			//GD.Print("is overlapping finishline true");
			return true;
		}
		else return false;
	}
	
	/// <summary>
	/// Checks if the inputted player has passed all the checkpoints 
	/// on the track to count as a lap upon passing the finish line
	/// </summary>
	/// <param name="C">The car we want to check for checkpoints passed</param>
	/// <returns>True if the player has all checkpoint references in their passed list</returns>
	private bool HitAllCheckpoints(Car c)
	{
		//check if the car has crossed all checkpoints
		if (c.CheckCheckpoints())
		{
			//GD.Print("Checking checkpoints true");
			return true;
		}
		else
		{
			//GD.Print("Checking checkpoints false");
			return false;
		}
	}

	/// <summary>
	/// Everytime a player is finished with the race, increment the counter
	/// </summary>
	private void IncrementPlayersFinished()
	{
		numPlayersFinished++;
	}

	/// <summary>
	/// Upon all players finishing the race, zero out the counter
	/// </summary>
	private void ClearPlayersFinished()
	{
		numPlayersFinished = 0;
	}
}
