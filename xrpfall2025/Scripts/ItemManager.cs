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
	[Export] private int itemsPerLocattion = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//set up all the item nodes before the scene starts
		items = new List<Node>();

		GenerateItems(player);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Init(Player p, Track t)
	{
		player = p;
		track = t;

	}

	public void GenerateItems(Player player)
	{
		for (int i = 0; i < itemLocations; i++)
		{
			//calculate where on the track we want to generate this row of items
			double theta = Mathf.DegToRad(i * 360 / itemLocations);
			//determine the direction to generate the items in
			for (int j = 0; j < itemsPerLocattion; j++)
			{
				Vector3 spawnPos = new Vector3();
				Item temp;
				items.Add(itemPrefab.Instantiate()); //create the new item
				AddChild(items[^1]); //add to the scene tree
				temp = (Item)items[i];
				temp.CustomInit(player, spawnPos);
				//player subscribes to each item individually
				temp.OnItemCollision += player.StartTimer;
			}			
		}
	}

}
