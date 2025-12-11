using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Custom track generation. Feed a set of vector3 points and a scale to size the track
/// The first node should ALWAYS be the origin, this will not be checked
/// </summary>
public partial class RoundTrack : Node
{
	[Export] private Path3D innerPath;
	[Export] private Path3D outerPath;
	[Export] private int scale = 1;
	[Export] private int numOfPts = 0;
	private Vector3 startPoint;
	private List<Vector3> checkPoints;

	public Path3D Path3D
	{
		get { return innerPath; }
	}

	public Vector3 StartingPoint
	{
		get { return startPoint; }
	}
	
	public List<Vector3> Checkpoints
	{
		get { return checkPoints; }
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

	/// <summary>
	/// Initializes the track curve and generates the trash mesh along it
	/// </summary>
	public void Init()
	{
		//list of points that will generate track loop
		List<Vector3> pts = new List<Vector3>();
		//checkpoints scattered throughout the track
		checkPoints = new List<Vector3>(); 
		Random rng = new Random();
		//Path3D outerPath = new Path3D();
		
		float radius = 5f;  

		//change in angle
		float deltaTheta = Mathf.DegToRad(360 / numOfPts);

		//with the origin being the center of generation, we generate numOfPts random lengths. 
		// then calculate the point from there
		for (int i = 0; i < numOfPts; i++)
		{
			//double hypotenus = rng.Next(1, 12); //how far from the origin is the point
			//double x = hypotenus * Mathf.Cos(deltaTheta * i + 1); //find x coord
			//double z = hypotenus * Mathf.Sin(deltaTheta * i + 1); //find z coord
			
			float angle = deltaTheta * i;
			float x = radius * Mathf.Cos(angle);
			float z = radius * Mathf.Sin(angle);
			pts.Add(new Vector3((float)x, 0, (float)z) * scale); //add and scale the point
		}

		//get the starting point saved
		startPoint = pts[0];
		//ensure the loop is closed
		innerPath.Curve.Closed = true;
		//start generating the track loo. 
		//the first one will be inner track, second loop will be the outer track. 
		// Since the mesh generates towards the origin rather than centered on the 
		// bezier curve, we will treat the inner track as the track's midpoint line overall
		for (int i = 0; i < pts.Count; i++) //first; inner
		{
			//GD.Print(pts[i]);
			innerPath.Curve.AddPoint(pts[i], null, null, i);
			outerPath.Curve.AddPoint(pts[i] * (float)1.1, null, null, i);
			if (i % 2 == 0) checkPoints.Add(pts[i]);
		}



	}
	
	/// <summary>
	/// Get the direction of flow at a specific point on the track curve
	/// </summary>
	/// <returns></returns>
	private Transform3D GetTangentOfPoint()
	{
		Transform3D result  = new Transform3D();

		return result;
	}
}
