using Godot;
using System;

public partial class ServerSetupUi : Node
{
	//exports from tree
	[Export] private LineEdit ipInput;
	[Export] private LineEdit portInput;
	[Export] private RichTextLabel portOutput;

	public void _OnServerPressed()
	{
		String ip = null;
		int port = 0;
		if (ipInput.Text != string.Empty || portInput.Text != string.Empty)
		{
			ip = ipInput.Text.ToLower().Trim();
			Int32.TryParse(portInput.Text.Trim(), out port);
			NetworkHandler.Instance.StartServer(ip, port);
		}
		else NetworkHandler.Instance.StartServer();

		if (NetworkHandler.Instance.IsServer) portOutput.Text = "Server hosted on port " + port.ToString() + " and ip is " + ip;
		else portOutput.Text = "ERROR, please check your ip and port input!";
	}
	
	public void _OnClientPressed()
	{
		if (ipInput.Text != string.Empty || portInput.Text != string.Empty)
		{
			int port;
			string ip = ipInput.Text.ToLower().Trim();
			Int32.TryParse(portInput.Text.Trim(), out port);
			NetworkHandler.Instance.StartClient(ip, port);
		}
		else NetworkHandler.Instance.StartClient();
	}
}
