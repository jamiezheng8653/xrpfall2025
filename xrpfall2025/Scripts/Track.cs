using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Custom track generation. Feed a set of vector3 points and a scale to size the track
/// The first node should ALWAYS be the origin, this will not be checked
/// </summary>
public partial class Track : Node
{
	[Export] private Path3D path = null;
	[Export] private int scale = 1;
	[Export] private int numOfPts = 0;
	private Vector3 startPoint;

	public Curve3D Curve
	{
		get{ return path.Curve; }
	}

	public Vector3 StartingPoint
	{
		get{ return startPoint; }
	}

    public override void _EnterTree()
	{

	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Init()
	{
		//list of points that will generate track loop
		List<Vector3> pts = new List<Vector3>();
		Random rng = new Random();

		//change in angle
		double deltaTheta = Mathf.DegToRad(360 / numOfPts);		
		
		//with the origin being the center of generation, we generate numOfPts random lengths. 
		// then calculate the point from there
		for (int i = 0; i < numOfPts; i++)
		{
			double hypotenus = rng.Next(1, 15); //how far from the origin is the point
			double x = hypotenus * Mathf.Cos(deltaTheta * i + 1); //find x coord
			double z = hypotenus * Mathf.Sin(deltaTheta * i + 1); //find z coord
			pts.Add(new Vector3((float)x, 0, (float)z) * scale); //add and scale the point
			//GD.Print(pts[i]);
		}

		startPoint = pts[0]; //get the starting point saved
		path.Curve.Closed = true; //ensure the loop is closed 
		//start generating the loop
		for (int i = 0; i < pts.Count; i++)
		{
			//GD.Print(pts[i]);
			path.Curve.AddPoint(pts[i], null, null, i);
		}

	}
}
