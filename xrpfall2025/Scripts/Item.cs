using Godot;
using System;

public enum Selection
{
	Inverse,
	Fast,
	Slow
}

//delegate declaration
public delegate void ItemCollisionDelegate();

/// <summary>
/// What the player will collide with to change their state temporarily.
/// Items are NOT rigidbodies, players should be able to just pass through them
/// And with a collision check we see if the player gets the item or not
/// </summary>
public partial class Item : Node
{
	public event ItemCollisionDelegate OnItemCollision;
	//Upon player collision, the item will disappear and a random
	//  number generator will run to select which item the player will get. 
	// The numbers for the random number generator will coorespond with an enum
	// listing the various items the player can interact with. 
	// Upon selection, the item will be used immediately and disappear before 
	// reloading on the screen after a set period of time. 
	// The item will set off an event that will notify the player to change its state
	// accordingly.
	private Player player = null; //Item manager will pass in this information

	public Item(Player player)
	{
		this.player = player;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//check for if this item has overlapped with the player.
		/*
		if ([collision logic])
		{
			//invoke event
			//if (OnItemCollision != null) OnItemCollision();
			OnItemCollision?.Invoke();
		}*/
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
}
