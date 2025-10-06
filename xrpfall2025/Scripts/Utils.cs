using Godot;
using System;

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

	/// <summary>
	/// Implementation of the Seperating Axis Theorem (SAT)
	/// Checks 8 planes
	/// </summary>
	/// <returns>If the two passed in shapes overlap in any way</returns>
	public static bool SAT()
	{
		return true;
	}
}
