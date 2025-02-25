class_name YlandStandards

static var std_unit = 0.375
static var std_half_unit = 0.1875

static var shape_lookup = {}
static var type_lookup = {}
static var id_lookup = {}

static func preload_lookups() -> void:
	var base_dir = "res://scenes/packed/"
	
	YlandStandards.shape_lookup["STANDARD"] = load(base_dir + "ylands_block_std.tscn")
	YlandStandards.shape_lookup["SLOPE"] = load(base_dir + "ylands_block_slope.tscn")
	YlandStandards.shape_lookup["CORNER"] = load(base_dir + "ylands_block_corner.tscn")
	YlandStandards.shape_lookup["SPIKE"] = load(base_dir + "ylands_block_spike.tscn")
	
	YlandStandards.type_lookup["MUSKET BALL"] = load(base_dir + "ylands_type_musket_ball.tscn")
