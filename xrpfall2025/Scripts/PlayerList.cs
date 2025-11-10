using Godot;
using System.Collections.Generic;

public partial class PlayerList : Node
{
	private List<Player> list;
	public static PlayerList Instance;
	
	public List<Player> List{
		get { return list; }
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		list = new List<Player>();
	}
}
