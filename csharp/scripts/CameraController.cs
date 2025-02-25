using Godot;

public partial class CameraController : Camera3D
{
	public float speed;
	public float speed_ang;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.speed = (float)this.GetMeta("move_speed");
		this.speed_ang = (float)this.GetMeta("turn_speed");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		float speed = this.speed;
		Vector3 glb_move = Vector3.Zero;
		Vector3 rel_move = Vector3.Zero;
		Vector3 rel_rotate = Vector3.Zero;

		if (Godot.Input.IsPhysicalKeyPressed(Key.W)) rel_move.Z = -1f;
		if (Godot.Input.IsPhysicalKeyPressed(Key.S)) rel_move.Z = 1f;
		if (Godot.Input.IsPhysicalKeyPressed(Key.A)) rel_move.X = -1f;
		if (Godot.Input.IsPhysicalKeyPressed(Key.D)) rel_move.X = 1f;
		if (Godot.Input.IsPhysicalKeyPressed(Key.Q)) glb_move.Y = 1f;
		if (Godot.Input.IsPhysicalKeyPressed(Key.Z)) glb_move.Y = -1f;

		if (Godot.Input.IsPhysicalKeyPressed(Key.Up)) rel_rotate.X = 1f;
		if (Godot.Input.IsPhysicalKeyPressed(Key.Down)) rel_rotate.X = -1f;
		if (Godot.Input.IsPhysicalKeyPressed(Key.Left)) rel_rotate.Y = 1f;
		if (Godot.Input.IsPhysicalKeyPressed(Key.Right)) rel_rotate.Y = -1f;
		
		if (Godot.Input.IsPhysicalKeyPressed(Key.Shift)) speed *= 5f;
		if (Godot.Input.IsPhysicalKeyPressed(Key.Ctrl)) speed *= 0.25f;

		glb_move = (glb_move + (this.Quaternion * rel_move)).Normalized();
		rel_rotate = rel_rotate.Normalized();

		this.Position += glb_move * speed * (float)delta;
		this.Rotation += rel_rotate * this.speed_ang * (float)delta;
	}
}
