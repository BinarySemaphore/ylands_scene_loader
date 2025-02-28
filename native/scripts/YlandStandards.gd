class_name YlandStandards

static var std_unit = 0.375
static var std_half_unit = 0.1875

static var shape_lookup = {}
static var type_lookup = {}
static var id_lookup = {}

static func preload_lookups() -> void:
	var base_dir = "res://scenes/packed/"
	
	YlandStandards.shape_lookup['STANDARD'] = load(base_dir + "ylands_block_std.tscn")
	YlandStandards.shape_lookup['SLOPE'] = load(base_dir + "ylands_block_slope.tscn")
	YlandStandards.shape_lookup['CORNER'] = load(base_dir + "ylands_block_corner.tscn")
	YlandStandards.shape_lookup['SPIKE'] = load(base_dir + "ylands_block_spike.tscn")
	
	YlandStandards.type_lookup['MUSKET BALL'] = load(base_dir + "ylands_type_musket_ball.tscn")
	
	YlandStandards.id_lookup['3966'] = load(base_dir + "ylands_block_glass_window_1x1x1_3966.tscn")
	YlandStandards.id_lookup['2756'] = load(base_dir + "ylands_block_glass_window_2x2x1_2756.tscn")
	YlandStandards.id_lookup['5617'] = load(base_dir + "ylands_block_glass_window_2x4x1_5617.tscn")
	YlandStandards.id_lookup['5618'] = load(base_dir + "ylands_block_glass_window_4x4x1_5618.tscn")
	YlandStandards.id_lookup['3978'] = load(base_dir + "ylands_ship_hull_wooden_large_3978.tscn")

static func set_entity_color(entity: Node3D, color: Array) -> void:
	if color.size() < 3: return
	var mat = YlandStandards.get_entity_surface_material(entity)
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

static func get_entity_surface_material(entity: Node3D) -> StandardMaterial3D:
	var mat: StandardMaterial3D = null
	
	var mesh = entity.get_child(0) as MeshInstance3D
	if not mesh: return null
	
	mat = mesh.get_surface_override_material(0)
	mat = mat if mat else mesh.mesh.surface_get_material(0)
	
	return mat
