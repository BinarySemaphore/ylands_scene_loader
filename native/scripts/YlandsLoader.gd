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
	var status: int
	var combo_mesh = {}
	var parent_inv_rotation = parent.quaternion.inverse()
	
	for key in root:
		node = _get_node_from_item(root[key])
		if not node: continue
		
		node.name = "[%s] %s" % [key, node.name]
		node.position = parent_inv_rotation * (node.position - parent.position)
		node.quaternion = parent_inv_rotation * node.quaternion
		
		if combine_mesh:
			status = _add_to_combo_mesh(combo_mesh, node)
			if status == -1:
				parent.add_child(node)
				continue
			elif status == 0:
				parent.add_child(_combo_mesh_to_node(combo_mesh))
				combo_mesh = {}
				status = _add_to_combo_mesh(combo_mesh, node)
				if status != 1:
					assert(false, "Shouldn't be able to reach this; something very wrong happened")
			_immidiate_free_node_and_children(node)
		else:
			parent.add_child(node)
	
	if combine_mesh:
		parent.add_child(_combo_mesh_to_node(combo_mesh))

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
	if color.size() < 3: return
	var mat = _get_entity_surface_material(entity)
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

func _get_entity_color_uid(entity: Node3D) -> String:
	var color_uid = ""
	var mat = _get_entity_surface_material(entity)
	if not mat: return ""
	
	color_uid += mat.albedo_color.to_html()
	if mat.emission_enabled:
		color_uid += " e:" + mat.emission.to_html()
	
	return color_uid

func _get_entity_surface_material(entity: Node3D) -> StandardMaterial3D:
	var mat: StandardMaterial3D = null
	
	var mesh = entity.get_child(0) as MeshInstance3D
	if not mesh: return null
	
	mat = mesh.get_surface_override_material(0)
	mat = mat if mat else mesh.mesh.surface_get_material(0)
	if not mat: return null
	
	return mat

func _add_to_combo_mesh(combo: Dictionary, node: Node3D) -> int:
	'''
	Return status: <int> -1: invalid | 0: combo full | 1: success
	'''
	var check_child: Node
	var mesh: MeshInstance3D
	
	# Check if valid mesh containing node
	check_child = node.get_child(0)
	if not check_child or not is_instance_of(check_child, MeshInstance3D): return -1
	mesh = check_child
	if not mesh.mesh: return -1
	
	var color_uid = _get_entity_color_uid(node)
	if not color_uid: return -1
	
	# Check if combined mesh is full
	if not color_uid in combo and combo.size() == RenderingServer.MAX_MESH_SURFACES:
		return 0
	
	# Apply node transform to local verts and normals
	var vert: Vector3
	var norm: Vector3
	var surface_data = mesh.mesh.surface_get_arrays(0)
	var local_verts = surface_data[Mesh.ARRAY_VERTEX]
	var local_normals = surface_data[Mesh.ARRAY_NORMAL]
	for index in range(local_verts.size()):
		vert = local_verts[index]
		norm = local_normals[index]
		vert *= mesh.scale
		vert = mesh.quaternion * vert
		vert += mesh.position
		vert *= node.scale
		vert = node.quaternion * vert
		vert += node.position
		norm = node.quaternion * mesh.quaternion * norm
		local_verts[index] = vert
		local_normals[index] = norm
	
	if color_uid not in combo:
		# Create new surface
		combo[color_uid] = [
			surface_data,
			_get_entity_surface_material(node)
		]
	else:
		# Add to existing surface
		var vert_count_orig = 0
		for index in range(combo[color_uid][0].size()):
			if index >= surface_data.size(): break
			if not surface_data[index]: continue
			if index == Mesh.ARRAY_VERTEX:
				vert_count_orig = combo[color_uid][0][index].size()
			if index == Mesh.ARRAY_INDEX and vert_count_orig:
				for mindex in range(surface_data[index].size()):
					surface_data[index][mindex] += vert_count_orig
			combo[color_uid][0][index].append_array(surface_data[index])
	
	return 1

func _combo_mesh_to_node(combo: Dictionary) -> Node3D:
	if not combo: return
	var node = Node3D.new()
	node.position = Vector3.ZERO
	node.quaternion = Quaternion.IDENTITY
	node.name = "[%d] Combined Mesh" % cm_count
	cm_count += 1
	
	# Create the actual combined mesh and add to node
	var mesh_container = MeshInstance3D.new()
	var new_mesh = ArrayMesh.new()
	var mesh_flags = Mesh.ARRAY_VERTEX | Mesh.ARRAY_NORMAL | Mesh.ARRAY_TANGENT | Mesh.ARRAY_FORMAT_TEX_UV | Mesh.ARRAY_INDEX
	var data: Array
	var mat: StandardMaterial3D
	var index = 0
	for key in combo:
		data = combo[key][0]
		mat = combo[key][1]
		new_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, data, [], {}, mesh_flags)
		new_mesh.surface_set_material(index, mat)
		index += 1
	mesh_container.mesh = new_mesh
	node.add_child(mesh_container)
	return node

func _immidiate_free_node_and_children(node: Node) -> void:
	var child: Node
	for index in range(node.get_child_count()):
		child = node.get_child(index)
		if not child or child.is_queued_for_deletion(): continue
		_immidiate_free_node_and_children(child)
	# Handle material override free issue: https://github.com/godotengine/godot/issues/85817
	if is_instance_of(node, MeshInstance3D):
		for index in range(node.get_surface_override_material_count()):
			node.set_surface_override_material(index, null)
	node.free()
