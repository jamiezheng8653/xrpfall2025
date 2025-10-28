extends Node

@export var charbody2d: CharacterBody2D;

const SPEED: float = 500

var is_authority: bool:
	get: return !LowLevelNetworkHandler.is_server && owner_id == ClientNetworkGlobals.id;

var owner_id: int;

func _enter_tree() -> void:
	ServerNetworkGlobals.handle_player_position.connect(server_handle_player_position);
	ClientNetworkGlobals.handle_player_position.connect(client_handle_player_position);

func _exit_tree() -> void:
	ServerNetworkGlobals.handle_player_position.disconnect(server_handle_player_position);
	ClientNetworkGlobals.handle_player_position.disconnect(client_handle_player_position);

func _physics_process(_delta: float) -> void:
	if !is_authority: return;

	charbody2d.velocity = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down") * SPEED;

	charbody2d.move_and_slide();

	PlayerPosition.create(owner_id, charbody2d.global_position).send(LowLevelNetworkHandler.server_peer);

func server_handle_player_position(peer_id: int, player_position: PlayerPosition) -> void:
	if owner_id != peer_id: return;
	charbody2d.global_position = player_position.position;
	PlayerPosition.create(owner_id, charbody2d.global_position).broadcast(LowLevelNetworkHandler.connection);

func client_handle_player_position(player_position: PlayerPosition) -> void:
	if is_authority || owner_id != player_position.id: 
		return;

	charbody2d.global_position = player_position.position;
