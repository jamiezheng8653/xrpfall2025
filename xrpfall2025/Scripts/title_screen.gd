extends Control

func _on_start_button_pressed() -> void:
	get_tree().change_scene_to_file("res://Scenes/GameplayScenes/customization_screen.tscn")
	
	pass # Replace with function body.


func _on_quickstart_pressed() -> void:
	get_tree().change_scene_to_file("res://Scenes/GameplayScenes/main.tscn")
	pass # Replace with function body.
