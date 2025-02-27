extends Node3D

var load_scene: bool
var combine_mesh: bool
var unsupported_draw: bool
var unsupported_transparency: float
var cm_count: int
var blocks: Dictionary
var scene: Dictionary

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	var raw_data: String
	var blockdef_file = get_meta("blockdef_file")
	var scene_file = get_meta("scene_file")
	unsupported_draw = get_meta("box_draw_unsupported", true)
	unsupported_transparency = get_meta("unsupported_transparency", 0.5)
	combine_mesh = get_meta("mesh_combine_similar", false)
	
	raw_data = FileAccess.get_file_as_string(blockdef_file)
	blocks = JSON.parse_string(raw_data)
	raw_data = FileAccess.get_file_as_string(scene_file)
	scene = JSON.parse_string(raw_data)
	
	YlandStandards.preload_lookups()
	
	load_scene = true
	cm_count = 0

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	if load_scene:
		_build_scene(self, scene)
		load_scene = false

func _build_scene(parent: Node3D, root: Dictionary) -> void:
	var node: Node3D
	var combo_mesh = ComboMesh.new()
	var parent_inv_rotation = parent.quaternion.inverse()
	
	for key in root:
		node = _get_node_from_item(root[key])
		if not node: continue
		
		node.name = "[%s] %s" % [key, node.name]
		node.position = parent_inv_rotation * (node.position - parent.position)
		node.quaternion = parent_inv_rotation * node.quaternion
		
		if not combine_mesh:
			parent.add_child(node)
		# Ignore the rest of this function (if reviewing as simple example - combo_mesh is an advanced feature)
		else:
			if root[key]['type'] == "group":
				parent.add_child(node)
				continue
			if not combo_mesh.append(node):
				parent.add_child(combo_mesh.commit_to_node(), true)
				combo_mesh.append(node)
			ComboMesh.immidiate_free_node_and_children(node)
	
	# Ignore this (if reviewing as simple example - combo_mesh is an advanced feature)
	if combine_mesh:
		parent.add_child(combo_mesh.commit_to_node(), true)
		cm_count += 1

func _get_node_from_item(item: Dictionary) -> Node3D:
	var node: Node3D = null
	
	if item['type'] == "entity":
		node = _create_new_entity_from_ref(item['blockdef'])
		if node and item.has('colors') and item['colors'].size() >= 1:
			YlandStandards.set_entity_color(node, item['colors'][0])
	elif item['type'] == "group":
		node = Node3D.new()
	
	if node:
		node.name = item['name']
		node.position = Vector3(
			item['position'][0],
			item['position'][1],
			-item['position'][2]
		)
		node.rotation_degrees = Vector3(
			-item['rotation'][0],
			-item['rotation'][1],
			item['rotation'][2]
		)
		
		if item.has('children') and item['children'].size() > 0:
			_build_scene(node, item['children'])
	
	return node

func _create_new_entity_from_ref(ref_key: String) -> Node3D:
	var node: Node3D = null
	var mesh: MeshInstance3D
	
	if ref_key not in blocks:
		print("No block reference for \"%s\"" % ref_key)
		return null
	var block_ref = blocks[ref_key]
	
	if ref_key in YlandStandards.id_lookup:
		node = YlandStandards.id_lookup[ref_key].instantiate()
	elif block_ref['type'] in YlandStandards.type_lookup:
		node = YlandStandards.type_lookup[block_ref['type']].instantiate()
	elif block_ref['shape'] in YlandStandards.shape_lookup:
		node = YlandStandards.shape_lookup[block_ref['shape']].instantiate()
		if not node: return null
		
		mesh = node.get_child(0)
		if not mesh:
			node.queue_free()
			return null
		
		mesh.scale = Vector3(
			YlandStandards.std_unit,
			YlandStandards.std_unit,
			YlandStandards.std_unit
		)
		mesh.position = Vector3(
			block_ref['bb-center-offset'][0] / block_ref['size'][0],
			block_ref['bb-center-offset'][1] / block_ref['size'][1],
			-block_ref['bb-center-offset'][2] / block_ref['size'][2]
		)
		node.scale = Vector3(
			block_ref['size'][0],
			block_ref['size'][1],
			block_ref['size'][2]
		)
	elif unsupported_draw:
		node = Node3D.new()
		mesh = MeshInstance3D.new()
		mesh.mesh = BoxMesh.new()
		mesh.mesh.material = StandardMaterial3D.new()
		mesh.mesh.material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		mesh.mesh.material.albedo_color = Color(0, 0, 0, unsupported_transparency)
		mesh.mesh.size = Vector3(
			block_ref['bb-dimensions'][0],
			block_ref['bb-dimensions'][1],
			block_ref['bb-dimensions'][2]
		)
		mesh.position = Vector3(
			block_ref['bb-center-offset'][0],
			block_ref['bb-center-offset'][1],
			-block_ref['bb-center-offset'][2]
		)
		node.add_child(mesh)
	
	if node:
		if block_ref.has('colors') and block_ref['colors'].size() >= 1:
			YlandStandards.set_entity_color(node, block_ref['colors'][0])
	else:
		print("Unsupported Entity: %s" % block_ref)
	
	return node
