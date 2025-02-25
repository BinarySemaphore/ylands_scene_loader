extends Camera3D

var speed: float
var speed_ang: float

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	speed = get_meta("move_speed")
	speed_ang = get_meta("turn_speed")


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	var actual_speed = speed
	var glb_move = Vector3.ZERO;
	var rel_move = Vector3.ZERO;
	var rel_rotate = Vector3.ZERO;
	
	if Input.is_physical_key_pressed(KEY_W): rel_move.z = -1
	if Input.is_physical_key_pressed(KEY_S): rel_move.z = 1
	if Input.is_physical_key_pressed(KEY_A): rel_move.x = -1
	if Input.is_physical_key_pressed(KEY_D): rel_move.x = 1
	if Input.is_physical_key_pressed(KEY_Q): glb_move.y = 1
	if Input.is_physical_key_pressed(KEY_Z): glb_move.y = -1
	
	if Input.is_physical_key_pressed(KEY_UP): rel_rotate.x = 1
	if Input.is_physical_key_pressed(KEY_DOWN): rel_rotate.x = -1
	if Input.is_physical_key_pressed(KEY_LEFT): rel_rotate.y = 1
	if Input.is_physical_key_pressed(KEY_RIGHT): rel_rotate.y = -1
	
	if Input.is_physical_key_pressed(KEY_SHIFT): actual_speed *= 5
	if Input.is_physical_key_pressed(KEY_CTRL): actual_speed *= 0.25
	
	glb_move = (glb_move + (quaternion * rel_move)).normalized()
	rel_rotate = rel_rotate.normalized()
	
	position += glb_move * actual_speed * delta
	rotation += rel_rotate * speed_ang * delta
