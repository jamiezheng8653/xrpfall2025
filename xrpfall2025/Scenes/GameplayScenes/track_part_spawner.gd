extends Button

@export var spawned_piece : PackedScene

func _on_button_down() -> void:
	print(owner)
	owner.spawn_part(spawned_piece) # I think owner is effectively root?
