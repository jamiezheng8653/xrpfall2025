using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;
using Vector3 = Godot.Vector3;

/// <summary>
/// Custom track generation. Feed a set of vector3 points and a scale to size the track
/// The first node should ALWAYS be the origin, this will not be checked
/// </summary>
public partial class Track : Node
{
	[Export] private Path3D innerPath;
	[Export] private Path3D outerPath;
	[Export] private int scale = 1;
	[Export] private int numOfPts = 0;
	private Vector3 startPoint;
	private List<Vector3> checkPoints = new List<Vector3>();
	private List<Vector3> points = new List<Vector3>();

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
		if (points.Count <= 0)
		{
			
			PopulateList(points);
			//get the starting point saved
			startPoint = points[0];
			//ensure the loop is closed
			innerPath.Curve.Closed = true;
			outerPath.Curve.Closed = true;
		} 
		
		//GenerateTrackMeshStraight(Checkpoints, points, outerPath);
		GenerateTrackMeshBezier(checkPoints, points, innerPath);
	}

	/// <summary>
	/// Interpolate point of a quadratic bezier 
	/// </summary>
	/// <param name="p0"></param>
	/// <param name="p1"></param>
	/// <param name="p2"></param>
	/// <param name="t">Weight (must be less than one)</param>
	/// <returns></returns>
	private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
	{
		Vector3 q0 = p0.Lerp(p1, t);
		Vector3 q1 = p1.Lerp(p2, t);
		Vector3 r = q0.Lerp(q1, t);
		return r;
	}

	/// <summary>
	/// Interpolate point of a cubic bezier
	/// </summary>
	/// <param name="p0"></param>
	/// <param name="p1"></param>
	/// <param name="p2"></param>
	/// <param name="p3"></param>
	/// <param name="t">Weight (must be less than one)</param>
	/// <returns></returns>
	private Vector3 CubicBezier (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		Vector3 q0 = p0.Lerp(p1, t);
		Vector3 q1 = p1.Lerp(p2, t);
		Vector3 q2 = p2.Lerp(p3, t);
		Vector3 r0 = q0.Lerp(q1, t);
		Vector3 r1 = q1.Lerp(q1, t);
		Vector3 s = r0.Lerp(r1, t);
		return s;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="pts"></param>
	private void PopulateList(List<Vector3> pts)
	{
		//list of points that will generate track loop
		//checkpoints scattered throughout the track
		Random rng = new Random();
		//Path3D outerPath = new Path3D();

		//change in angle
		double deltaTheta = Mathf.DegToRad(360 / numOfPts);

		//with the origin being the center of generation, we generate numOfPts random lengths. 
		// then calculate the point from there
		for (int i = 0; i < numOfPts; i++)
		{
			double hypotenus = rng.Next(1, 12); //how far from the origin is the point
			double x = hypotenus * Mathf.Cos(deltaTheta * i + 1); //find x coord
			double z = hypotenus * Mathf.Sin(deltaTheta * i + 1); //find z coord
			pts.Add(new Vector3((float)x, 0, (float)z) * scale); //add and scale the point
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="chckpts"></param>
	/// <param name="pts"></param>
	/// <param name="pathA"></param>
	/// <param name="pathB"></param>
	private void GenerateTrackMeshStraight(List<Vector3> chckpts, List<Vector3> pts, Path3D pathA, Path3D pathB = null)
	{
		//start generating the track loop. 
		//the first one will be inner track, second loop will be the outer track. 
		// Since the mesh generates towards the origin rather than centered on the 
		// bezier curve, we will treat the inner track as the track's midpoint line overall
		for (int i = 0; i < pts.Count; i++) //first; inner
		{
			//GD.Print(pts[i]);
			pathA.Curve.AddPoint(pts[i], null, null, i);
			if(pathB != null) pathB.Curve.AddPoint(pts[i] * (float)1.1, null, null, i);
			if (i % 2 == 0) chckpts.Add(pts[i]);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="chckpts"></param>
	/// <param name="pts"></param>
	/// <param name="path"></param>
	public void GenerateTrackMeshBezier(List<Vector3> chckpts, List<Vector3> pts, Path3D path)
	{
		float t = 0;
		float tI = 0.1f;
		int i = 0;
		Vector3 lastPoint = pts[0];

		while(i < pts.Count - 2)
		{
			if(i == pts.Count - 3)
			{
				path.Curve.AddPoint(QuadraticBezier(
					lastPoint,
					//pts[i], 
					pts[pts.Count-1], 
					pts[0], 
					t
				));
			}
			path.Curve.AddPoint(QuadraticBezier(
				lastPoint,
				//pts[i], 
				pts[i + 1], 
				pts[i + 2], 
				t
			));
			t += tI;

			if (t > 1) 
			{
				if(i % 2 == 0) chckpts.Add(pts[i]);
				GD.Print("_i = ", i, ", ", pts[i]);
				t = 0;
				i += 2;
				lastPoint = path.Curve.GetPointPosition(path.Curve.PointCount - 1);
			}
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
