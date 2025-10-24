extends Control

func _on_home_button_pressed() -> void:
	get_tree().change_scene_to_file("res://Scenes/GameplayScenes/title_screen.tscn")
	pass # Replace with function body.


func _on_restart_button_pressed() -> void:
	get_tree().change_scene_to_file("res://Scenes/GameplayScenes/main.tscn")
	pass # Replace with function body.
