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
