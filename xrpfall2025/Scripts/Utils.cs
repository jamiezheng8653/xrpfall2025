using Godot;
using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Vector3 = Godot.Vector3;
using Plane = Godot.Plane;

/// <summary>
/// Class to store any universal functions that may be used throughout the project
/// </summary>
public static class Utils
{
	/// <summary>
	/// Math which projects vector u onto vector v
	/// v(u*v)/(||v||^2) 
	/// </summary>
	/// <param name="u">What vector are you projecting</param>
	/// <param name="v">What vector are you projecting onto</param>
	/// <returns>Resulting vector after the projection</returns>
	public static Vector3 ProjUOntoV(Vector3 u, Vector3 v)
	{
		//Projection of vector v onto vector u = v(v * u)/||v||^2
		return (v.Dot(u) / Mathf.Pow(v.Length(), 2)) * v;
	}

	public static Vector3 SortByMagnitude(Vector3 u, Vector3 v)
	{
		if (u.LengthSquared() < v.LengthSquared()) return v;
		else return u;
	}

	public static float Max(float a, float b, float c)
	{
		return Mathf.Max(Mathf.Max(a, b), c);
	}

	public static float Min(float a, float b, float c)
	{
		return Mathf.Min(Mathf.Min(a, b), c);
	}

	/// <summary>
	/// Given a path3d, find the closest point on the path from the given Global Position.
	/// Code from: https://medium.com/@oddlyshapeddog/finding-the-nearest-global-position-on-a-curve-in-godot-4-726d0c23defb
	/// </summary>
	/// <param name="path">The Path3D you want to snap to</param>
	/// <param name="GlobalPosition">Where in world space are you right now</param>
	/// <returns>The point on the path closest to the Global Position in world space</returns>
	public static Vector3 GetClosestAbsolutePosition(Path3D path, Vector3 GlobalPosition)
	{
		Curve3D curve = path.Curve;

		//transform the target position to local space
		Transform3D pathTransform = path.GlobalTransform;
		Vector3 localPoint = GlobalPosition * pathTransform;

		//get the nearest offset on the curve
		float offset = curve.GetClosestOffset(localPoint);

		//get the local position at this offset
		Vector3 curvePoint = curve.SampleBaked(offset, true);

		//transform it back to world space
		curvePoint = pathTransform * curvePoint;

		return curvePoint;
	}

	public static bool AABBPlaneIntersect(Aabb aabb, Plane plane)
	{
		//these two lines not necessary with a (center, extents) AABB representation
		Point c = (aabb.Max + aabb.Min) * 0.5f;
		Point e = aabb.Max - c;

		//compute the projection interval radius of b onto L(t) = aabb.c + t * plane.normal
		float r = e[0] * Mathf.Abs(plane.normal[0]) + e[1] * Mathf.Abs(plane.normal[1]) + e[2] * Mathf.Abs(plane.normal[2]);

		//compute distance of box center from plane 
		float s = plane.Normal.Dot(c) - plane.D;

		//intersection occurs when distance s falls within [-r, r] interval
		return Mathf.Abs(s) <= r;
	}

	/// <summary>
	/// Implementation of the Seperating Axis Theorem (SAT) agains a triangle and aabb
	/// Checks 8 planes
	/// </summary>
	/// <returns>If the two passed in shapes overlap in any way</returns>
	public static bool TriangleAABBSAT(Vector3[] triangle, Vector3[] box)
	{
		if (triangle.Length <= 0) return false; //no triangle exists
		float p0, p1, p2, r;

		//compute the box center and extends (if not already given in that format)
		Vector3 c = (box.Min() + box.Max()) * 0.5f;
		float e0 = (box.Max().X - box.Min().X) * 0.5f;
		float e1 = (box.Max().Y - box.Min().Y) * 0.5f;
		float e2 = (box.Max().Z - box.Min().Z) * 0.5f;

		//translate triangle as conceptually moving AABB to origin
		triangle[0] = triangle[0] - c;
		triangle[1] = triangle[1] - c;
		triangle[2] = triangle[2] - c;

		//compute edge vectors for triangle
		Vector3[] edges = [
			triangle[1] - triangle[0],
			triangle[2] - triangle[1],
			triangle[0] - triangle[2]
			];

		Vector3 aij;

		//test axes a00...a22 (category 3)
		//test a00
		aij = new Vector3(0, -edges[0].Z, edges[0].Y);
		p0 = triangle[0].Dot(aij);
		p1 = triangle[1].Dot(aij);
		p2 = triangle[2].Dot(aij);
		r = e1 * Mathf.Abs(edges[0].Z)
			+ e2 * Mathf.Abs(edges[0].Y);
		if (Mathf.Max(-Mathf.Max(p0, p2), Mathf.Min(p0, p2)) > r) return false; //axis is a seperating axis

		//test a01
		aij = new Vector3(0, -edges[1].Z, edges[1].Y);
		p0 = triangle[0].Dot(aij);
		p1 = triangle[1].Dot(aij);
		p2 = triangle[2].Dot(aij);
		r = e1 * Mathf.Abs(edges[1].Z)
			+ e2 * Mathf.Abs(edges[1].Y);
		if (Mathf.Max(-Mathf.Max(p0, p2), Mathf.Min(p0, p2)) > r) return false; //axis is a seperating axis

		//test a02
		if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r) return false;
		//test a10
		if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r) return false;
		//test a11
		if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r) return false;
		//test a12
		if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r) return false;
		//test a20
		if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r) return false;
		//test a21
		if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r) return false;
		//test a22
		if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r) return false;
		
		//test the three axes corresponding to the face normals of AABB b (category 1)
		//exit if...
		//... [-e0, e0] and [min(triangle[0].X, t1.X, t2.X), max(t0.X, t2.X, t2.X)] do not overlap
		if (Max(triangle[0].X, triangle[1].X, triangle[2].X) < -e0
			|| Min(triangle[0].X, triangle[1].X, triangle[2].X) > e0) return false;
		//... [-e1, e1] and [min(t0.Y, t1.Y, t2.Y), max(t0.Y, t1.Y, t2.Y)] do not overlap
		if (Max(triangle[0].Y, triangle[1].Y, triangle[2].Y) < -e1
			|| Min(triangle[0].Y, triangle[1].Y, triangle[2].Y) > e1) return false;
		//... [-e2, e2] and [min(t0.Z, t1.Z, t2.Z), max(t0.Z, t1.Z, t2.Z)] do not overlap
		if (Max(triangle[0].Z, triangle[1].Z, triangle[2].Z) < -e2
			|| Min(triangle[0].Z, triangle[1].Z, triangle[2].Z) > e2) return false;

		//test seperating axis corresponding to triangle face normal (category 2)
		Plane p = new Godot.Plane();
		p.Normal = edges[0].Cross(edges[1]);
		p.D = p.Normal.Dot(triangle[0]);
		return AABBPlaneIntersect(box, p);

		return true; //all tests fail, 
	}
}
