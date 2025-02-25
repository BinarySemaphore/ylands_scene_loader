extends Node3D

var load_scene: bool
var unsupported_draw: bool
var unsupported_transparency: float
var blocks: Dictionary
var scene: Dictionary

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	var raw_data: String
	var blockdef_file = get_meta("blockdef_file")
	var scene_file = get_meta("scene_file")
	unsupported_draw = get_meta("box_draw_unsupported", true)
	unsupported_transparency = get_meta("unsupported_transparency", 0.5)
	
	raw_data = FileAccess.get_file_as_string(blockdef_file)
	blocks = JSON.parse_string(raw_data)
	raw_data = FileAccess.get_file_as_string(scene_file)
	scene = JSON.parse_string(raw_data)
	
	YlandStandards.preload_lookups()
	
	load_scene = true

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	if load_scene:
		_build_scene(self, scene)
		load_scene = false

func _build_scene(parent: Node3D, root: Dictionary) -> void:
	var parent_inv_rotation = parent.quaternion.inverse()
	var new_block: Node3D = null
	
	for key in root:
		new_block = _get_node_from_item(root[key])
		if not new_block: continue
		
		new_block.name = "[%s] %s" % [key, new_block.name]
		new_block.position = parent_inv_rotation * (new_block.position - parent.position)
		new_block.quaternion = parent_inv_rotation * new_block.quaternion
		
		parent.add_child(new_block)

func _get_node_from_item(item: Dictionary) -> Node3D:
	var node: Node3D = null
	
	if item['type'] == "entity":
		node = _create_new_entity_from_ref(item['blockdef'])
		if node:
			if item.has('colors') and item['colors'].size() >= 1:
				_set_entity_color(node, item['colors'][0])
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
			_set_entity_color(node, block_ref['colors'][0])
	else:
		print("Unsupported Entity: %s" % block_ref)
	
	return node

func _set_entity_color(entity: Node3D, color: Array) -> void:
	var mesh = entity.get_child(0) as MeshInstance3D
	if not mesh or color.size() < 3: return
	
	var mat: StandardMaterial3D = mesh.get_surface_override_material(0)
	mat = mat if mat else mesh.mesh.surface_get_material(0)
	if not mat: return
	
	mat.albedo_color = Color(
		color[0],
		color[1],
		color[2],
		mat.albedo_color.a
	)
	if color.size() > 3 and color[3] > 0.001:
		mat.emission_enabled = true
		mat.emission = Color(
			color[0] * color[3],
			color[1] * color[3],
			color[2] * color[3]
		)
		mat.emission_energy_multiplier = color[3] * 20
		mat.rim_enabled = true
		mat.rim = 1
