@tool
extends EditorPlugin


func _enter_tree() -> void:
	# Initialization of the plugin goes here.
	pass


func _exit_tree() -> void:
	# Clean-up of the plugin goes here.
	pass

func _has_main_screen() -> bool:
	return true

func _make_visible(visible: bool) -> void:
	pass

func _get_plugin_name() -> String:
	return "XRP AR Kart Track Builder"

func _get_plugin_icon() -> Texture2D:
	return EditorInterface.get_editor_theme().get_icon("Node", "EditorIcons")
