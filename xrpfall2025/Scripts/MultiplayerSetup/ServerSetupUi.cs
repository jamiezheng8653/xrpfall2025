using Godot;
using System;

public partial class ServerSetupUi : Node
{
	//exports from tree
	[Export] private LineEdit ipInput;
	[Export] private LineEdit portInput;
	[Export] private RichTextLabel portOutput;

	/// <summary>
	/// Actions that occur upon attempting to set up a new server
	/// </summary>
	public void OnServerPressed()
	{
		string ip;
		int port;
		if (ipInput.Text != string.Empty || portInput.Text != string.Empty)
		{
			ip = ipInput.Text.ToLower().Trim();
			Int32.TryParse(portInput.Text.Trim(), out port);
			NetworkHandler.Instance.StartServer(ip, port);
		}
		else { NetworkHandler.Instance.StartServer(); }

		if (NetworkHandler.Instance.IsServer)
		{
			portOutput.Text =
			"Server hosted on port " + NetworkHandler.Instance.CurrentPort.ToString()
			+ " and ip is " + NetworkHandler.Instance.ServerIP.ToString();
		}
		else { portOutput.Text = "Connection failed, please check IP and port input"; }
	}
	
	/// <summary>
	/// Actions that occur when trying to connect to an existing server as a client
	/// </summary>
	public void OnClientPressed()
	{
		if (ipInput.Text != string.Empty || portInput.Text != string.Empty)
		{
			int port;
			string ip = ipInput.Text.ToLower().Trim();
			if (Int32.TryParse(portInput.Text.Trim(), out port))
			{
				NetworkHandler.Instance.StartClient(ip, port);
			}
			else
			{
				portOutput.Text = "ERROR, incorrect port input";
			}
		}
		else { NetworkHandler.Instance.StartClient(); }

		if (NetworkHandler.Instance.Connection != null)
		{
			portOutput.Text =
				"Joined server on port " + NetworkHandler.Instance.CurrentPort
				+ " and ip is " + NetworkHandler.Instance.ServerIP;
		}
		else { portOutput.Text = "ERROR, please check your ip and port input!"; }
	}
}
