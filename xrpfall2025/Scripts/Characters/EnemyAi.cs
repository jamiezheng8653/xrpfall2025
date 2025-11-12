using Godot;
using System;

/// <summary>
/// Class for Enemy cars. Inherits from parent Car class. 
/// Only difference between enemy and player is how movement
/// is handled and preset decision making
/// </summary>
public partial class EnemyAi : Car
{
	private enum RacingState
	{
		NormalDriving,
		Overtake,
		Attack,
		Defend,
		Recover

	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
	}

}
