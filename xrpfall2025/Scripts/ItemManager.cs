using Godot;
using System;
using System.Collections.Generic;

//delegate declaration
public delegate void ItemCollisionDelegate();

public partial class ItemManager : Node
{
	//grab a reference to the player
	//Every item generated will get a reference to the player's bounds 
	//Used in collision detection
	[Export] private CharacterBody3D player = null;
	private PackedScene itemPrefab = ResourceLoader.Load<PackedScene>("res://Scenes/Prefabs/item.tscn");

	//list of all items generated
	private List<Node> items = null;
	//list of locations where items will generate in a line
	//[Export] private List<Node> locations = new List<Node>();

	[Export] private int maxItems = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//set up all the item nodes before the scene starts
		items = new List<Node>();
		for (int i = 0; i < maxItems; i++)
		{
			items.Add(itemPrefab.Instantiate()); //create the new item
			AddChild(items[items.Count - 1]); //add to the scene tree
			//might have to set up the individual components of the item node 
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public Item GenerateItem()
	{
		return null;
	}

}
