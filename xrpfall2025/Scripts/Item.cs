using Godot;
using System;

/// <summary>
/// What the player will collide with to change their state temporarily.
/// Items are NOT rigidbodies, players should be able to just pass through them
/// And with a collision check we see if the player gets the item or not
/// </summary>
public partial class Item : Node
{
	// Upon player collision, the item will disappear and a random
	// number generator will run to select which item the player will get. 
	// The numbers for the random number generator will coorespond with an enum
	// listing the various items the player can interact with. 
	// Upon selection, the item will be used immediately and disappear before 
	// reloading on the screen after a set period of time. 
	// The item will set off an event that will notify the player to change its state
	// accordingly.
	public event ItemCollisionDelegate OnItemCollision;
	private Player player; //Item manager will pass in this information

	//references necessary for collision bounds
	private CsgCylinder3D cylinder;
	private Area3D area3d;

	//for gizmos debugging
	private Color color;

	private Vector3 Position
	{
		get { return area3d.Position; }
		set { area3d.Position = value; }
	}

	/// <summary>
	/// Cannot call the constructor when trying to instantiate a 
	/// </summary>
	/// <param name="player"></param>
	public Item(Player player = null)
	{
		//Position = position;
	}

	public Aabb AABB
	{
		get { return cylinder.GetAabb(); }
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		color = new Color("YELLOW");

		OnItemCollision += SelectItem;
		//node path will need to be updated when we get a formal player car model
		area3d = GetNode<Area3D>("Area3D");
		Position = new Vector3(2, 3, 1);
		cylinder = GetNode<CsgCylinder3D>("Area3D/CollisionShape3D/CSGCylinder3D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//check for if this item has overlapped with the player.

		if (AABB.Intersects(player.AABB))
		{
			GD.Print("I'm Colliding!");
			//invoke event
			//if (OnItemCollision != null) OnItemCollision();
			OnItemCollision?.Invoke(); //shorthand for above

			//have the model be hidden from the scene 
			//unsubscribe from OnItemCollision Event 
		}

		//DebugDraw3D.DrawBox(AABB.Position, Godot.Quaternion.Identity, Vector3.One, color);
		//DebugDraw3D.DrawAabb(AABB, color);
	}

	/// <summary>
	/// Random number generator to select what item the player drew upon collision
	/// Reason this is being done in the item rather than the item manager is because
	/// should this project expand into multiplayer, the event would only signal to the 
	/// specific player that collided with this item.
	/// </summary>
	private void SelectItem()
	{
		Random rng = new Random();
		//will need to manually update should we choose to update the list of items
		//currently no error checking of if the selected item is valid in the enum list
		int result = rng.Next(0, 3);
		player.Current = (States)result;
	}

	public void CustomInit(Player player)
	{
		this.player = player;
	}
}
