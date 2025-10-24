using Godot;
using System;
using System.Collections.Generic;

//delegate declaration
public delegate void ItemCollisionDelegate();

public partial class ItemManager : Node
{
	//grab a reference to the player
	//Every item generated will get a reference to the player's bounds 
	//Used in collision detection & item event declaration
	private Player player = null;
	private PackedScene itemPrefab = ResourceLoader.Load<PackedScene>("res://Scenes/Prefabs/item.tscn");

	//list of all items generated
	private List<Node> items = null;
	private Track track = null;

	[Export] private int itemLocations = 1;
	[Export] private int itemsPerLocation = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//set up all the item nodes before the scene starts
		items = new List<Node>();
		itemLocations = track.Path3D.Curve.PointCount;

		GenerateItems(player);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	/// <summary>
	/// Set references to the player and track
	/// </summary>
	/// <param name="p">Reference to the player</param>
	/// <param name="t">Reference to the track</param>
	public void Init(Player p, Track t)
	{
		player = p;
		track = t;

	}

	/// <summary>
	/// Instantiates items scattered throughout the track 
	/// </summary>
	/// <param name="player">Necessary for each player to be hooked 
	/// up to a collision event with each item spawned </param>
	public void GenerateItems(Player player)
	{
		for (int i = 0; i < itemLocations; i++)
		{
			//calculate where on the track we want to generate this row of items
			double theta = Mathf.DegToRad(i * 360 / itemLocations);
			Vector3 spawnPos = track.Path3D.Curve.GetPointPosition(i) + new Vector3(0, 1, 0);

			//get direction vector towards the origin from this point
			Vector3 dir = (Vector3.Zero - spawnPos).Normalized();
			//determine the direction to generate the items in
			for (int j = 0; j < itemsPerLocation; j++)
			{
				Item temp;
				items.Add(itemPrefab.Instantiate()); //create the new item
				AddChild(items[^1]); //add to the scene tree
				temp = (Item)items[i]; //grab reference to call its init()
				temp.CustomInit(player, spawnPos + (5 * dir));
				//player subscribes to each item individually
				temp.OnItemCollision += player.StartTimer;
			}			
		}
	}

}
