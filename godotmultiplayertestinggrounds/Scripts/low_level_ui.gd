extends Node
@export var ip_input: LineEdit;
@export var port_input: LineEdit;
@export var port_output: RichTextLabel;

func _on_server_pressed() -> void:
	var ip: String;
	var port: int;
	if(!ip_input.text.is_empty() || !port_input.text.is_empty()):
		ip = sanitize_input(ip_input.text);
		port = sanitize_input(port_input.text) as int;
		LowLevelNetworkHandler.start_server(ip, port);

	else: LowLevelNetworkHandler.start_server();

	if (LowLevelNetworkHandler.is_server): 
		port_output.text = str("Server hosted on port ", port, " and ip is ", ip);
	else: 
		port_output.text = str("ERROR, please check your ip and port input")

	#LowLevelNetworkHandler.start_server();

func _on_client_pressed() -> void:
	if(!ip_input.text.is_empty() || !port_input.text.is_empty()):
		var ip = sanitize_input(ip_input.text);
		var port: int = sanitize_input(port_input.text)as int;
		LowLevelNetworkHandler.start_client(ip, port);
	else: LowLevelNetworkHandler.start_client();

func sanitize_input(input: String) -> String:
	return input.to_lower().strip_edges(true, true);

 
