extends Node
@export var ui_node: Control;

func _on_server_pressed() -> void:
	LowLevelNetworkHandler.start_server();

func _on_client_pressed() -> void:
	LowLevelNetworkHandler.start_client();
