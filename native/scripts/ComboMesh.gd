class_name ComboMesh

var _surface_count: int
var _cmesh: Dictionary

func _init() -> void:
	_cmesh = {}
	_surface_count = 0

func append(node: Node3D) -> bool:
	'''
	Return bool; true if added successfully; false if full or invalid
	'''
	var check_child: Node
	var mesh: MeshInstance3D
	
	# Check if valid mesh containing node
	check_child = node.get_child(0)
	if not check_child or not is_instance_of(check_child, MeshInstance3D): return false
	mesh = check_child
	if not mesh.mesh: return false
	
	var color_uid = _get_entity_color_uid(node)
	if not color_uid: return false
	
	# Check if combined mesh is full
	if not color_uid in _cmesh and _surface_count == RenderingServer.MAX_MESH_SURFACES:
		return false
	
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
	
	if color_uid not in _cmesh:
		# Add new surface data
		_cmesh[color_uid] = [
			surface_data,
			YlandStandards.get_entity_surface_material(node)
		]
	else:
		# Add to existing surface data
		var vert_count_orig = 0
		for index in range(_cmesh[color_uid][0].size()):
			if index >= surface_data.size(): break
			if not surface_data[index]: continue
			if index == Mesh.ARRAY_VERTEX:
				vert_count_orig = _cmesh[color_uid][0][index].size()
			if index == Mesh.ARRAY_INDEX and vert_count_orig:
				for mindex in range(surface_data[index].size()):
					surface_data[index][mindex] += vert_count_orig
			_cmesh[color_uid][0][index].append_array(surface_data[index])
	
	_surface_count += 1
	
	return true

func commit_to_mesh(id: int = -1) -> MeshInstance3D:
	if _surface_count == 0: return null
	
	# Create the actual mesh
	var new_mesh = ArrayMesh.new()
	var mesh_flags = Mesh.ARRAY_VERTEX | Mesh.ARRAY_NORMAL | Mesh.ARRAY_TANGENT | Mesh.ARRAY_FORMAT_TEX_UV | Mesh.ARRAY_INDEX
	var data: Array
	var mat: StandardMaterial3D
	var index = 0
	for key in _cmesh:
		data = _cmesh[key][0]
		mat = _cmesh[key][1]
		new_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, data, [], {}, mesh_flags)
		new_mesh.surface_set_material(index, mat)
		index += 1
	
	var mesh_container = MeshInstance3D.new()
	mesh_container.mesh = new_mesh
	mesh_container.position = Vector3.ZERO
	mesh_container.quaternion = Quaternion.IDENTITY
	if id > -1:
		mesh_container.name = "[%d] Combined Mesh" % id
	else:
		mesh_container.name = "Combined Mesh"
	
	_cmesh.clear()
	_surface_count = 0
	
	return mesh_container

func _get_entity_color_uid(entity: Node3D) -> String:
	var color_uid = ""
	var mat = YlandStandards.get_entity_surface_material(entity)
	if not mat: return ""
	
	color_uid += mat.albedo_color.to_html()
	if mat.emission_enabled:
		color_uid += " e:" + mat.emission.to_html()
	
	return color_uid

static func immediate_free_node_and_children(node: Node) -> void:
	var child: Node
	for index in range(node.get_child_count()):
		child = node.get_child(index)
		if not child or child.is_queued_for_deletion(): continue
		ComboMesh.immediate_free_node_and_children(child)
	# Handle material override free issue: https://github.com/godotengine/godot/issues/85817
	if is_instance_of(node, MeshInstance3D):
		for index in range(node.get_surface_override_material_count()):
			node.set_surface_override_material(index, null)
	node.free()
