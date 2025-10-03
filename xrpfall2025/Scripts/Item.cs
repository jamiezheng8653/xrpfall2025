using Godot;
using System;
using System.Diagnostics;

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
	[Export] private CsgCylinder3D cylinder = null;
	[Export] private Area3D area3d = null;

	private Stopwatch timer = null;

	//for gizmos debugging
	private Color color;

	private Vector3 Position
	{
		get { return area3d.Position; }
		set { area3d.Position = value; }
	}

	/// <summary>
	/// Returns the Axis Aligned Bounding Box centered on this Item
	/// </summary>
	public Aabb AABB
	{
		get
		{
			Aabb temp = cylinder.GetAabb();
			temp.Position = area3d.GlobalPosition - temp.Size / 2;
			return temp;
		}
	}

	public Item()
	{
		player = null;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		color = new Color("YELLOW");

		OnItemCollision += SelectItem;
		OnItemCollision += StartTimer;
		OnItemCollision += HideModel;
		//node path will need to be updated when we get a formal player car model
		Position = new Vector3(2, 0, 1);
		timer = new Stopwatch();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//if a timer is going, then reset the item and the timer
		if (timer.ElapsedMilliseconds > 10000)
		{
			ClearTimer();
			ShowModel();
		}
		//otherwise read for collisions with the player
		else if (timer.ElapsedMilliseconds >= 0)
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
		}

		//GD.Print("Item timer: " + timer.ElapsedMilliseconds);

		//DebugDraw3D.DrawBox(AABB.Position, Godot.Quaternion.Identity, Vector3.One, color);
		DebugDraw3D.DrawAabb(AABB, color);
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

	/// <summary>
	/// Since parameterized constructors are inacessible in Godot, 
	/// this function will be called immediately after initializing 
	/// this instance in a different scene
	/// </summary>
	/// <param name="player">Reference to the Player instance in scene</param>
	public void CustomInit(Player player, Vector3 position)
	{
		this.player = player;
		Position = position;
	}

	private void StartTimer()
	{
		timer.Start();
		//unsubscribe from event
		OnItemCollision -= StartTimer;
		OnItemCollision -= SelectItem;
		OnItemCollision -= HideModel;
	}

	private void ClearTimer()
	{
		timer.Reset();
		//resubscribe to event
		OnItemCollision += StartTimer;
		OnItemCollision += SelectItem;
		OnItemCollision += HideModel;
	}

	private void HideModel()
	{
		cylinder.Hide();
	}

	private void ShowModel()
	{
		cylinder.Show();
	}
}
