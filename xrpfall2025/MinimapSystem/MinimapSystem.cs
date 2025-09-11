//code from https://gameidea.org/2024/12/13/how-to-make-mini-map-or-radar-for-3d-game/

using Godot;
using System;

public partial class MinimapSystem : Node
{
	//top down camera for minimap
	[Export] Camera3D minimapCamera;
	//player instance to make the camera follow
	[Export] Node3D player;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SubViewport subViewport = GetNode("SubViewportContainer")/SubViewport;
		Camera3D normalCamera = GetViewport().GetCamera3D();
		
		Rid minimapCameraRid = minimapCamera.GetCameraRid();
		Rid normalCameraRid = normalCamera.GetCameraRid();
		
		Rid subViewportRid = subViewport.GetViewportRid();
		Rid viewportRid = GetViewport().GetViewportRid();
		
		RenderingServer.ViewportAttachCamera(subViewportRid, minimapCameraRid);
		RenderingServer.ViewportAttachCamera(viewportRid, normalCameraRid);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		minimapCamera.GlobalPosition.X = player.GlobalPosition.X;
		minimapCamera.GlobalPosition.Z = player.GlobalPosition.Z;
	}
}
