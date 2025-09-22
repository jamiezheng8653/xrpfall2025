using Godot;
using System;

public partial class Item : Node
{
	private enum Selection
	{
		Fast,
		Slow,
		Inverse
	}
	//Upon player collision, the item will disappear and a random
	//  number generator will run to select which item the player will get. 
	// The numbers for the random number generator will coorespond with an enum
	// listing the various items the player can interact with. 
	// Upon selection, the item will be used immediately and disappear before 
	// reloading on the screen after a set period of time. 
	// The item will set off an event that will notify the player to change its state
	// accordingly.

	public delegate void ItemOnCollision(Player player);
	public event ItemOnCollision Collision; //event for notifying collision

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	/// <summary>
	/// Random number generator to select what item the player drew upon collision
	/// </summary>
	/// <returns>Selection enum of what item</returns>
	private Selection SelectItem()
	{
		Random rng = new Random();
		//will need to manually update should we choose to update the list of items
		//currently no error checking of if the selected item is valid in the enum list
		int result = rng.Next(0, 3);
		return (Selection)result;
	}

	protected virtual void OnCollision(Player player)
	{
		Collision?.Invoke(player);
	}
}
