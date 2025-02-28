using Godot;
using System.Collections.Generic;

namespace YlandsSceneLoader.scripts {

public partial class YlandStandards
{
	public const float std_unit = 0.375f;
	public const float std_half_unit = 0.1875f;
	
	static public Dictionary<string, PackedScene> shape_lookup = new Dictionary<string, PackedScene>();
	static public Dictionary<string, PackedScene> type_lookup = new Dictionary<string, PackedScene>();
	static public Dictionary<string, PackedScene> id_lookup = new Dictionary<string, PackedScene>();

	static public void PreloadLookups() {
		string base_dir = "res://scenes/packed/";

		YlandStandards.shape_lookup["STANDARD"] = GD.Load<PackedScene>(base_dir + "ylands_block_std.tscn");
		YlandStandards.shape_lookup["SLOPE"] = GD.Load<PackedScene>(base_dir + "ylands_block_slope.tscn");
		YlandStandards.shape_lookup["CORNER"] = GD.Load<PackedScene>(base_dir + "ylands_block_corner.tscn");
		YlandStandards.shape_lookup["SPIKE"] = GD.Load<PackedScene>(base_dir + "ylands_block_spike.tscn");

		YlandStandards.type_lookup["MUSKET BALL"] = GD.Load<PackedScene>(base_dir + "ylands_type_musket_ball.tscn");

		YlandStandards.id_lookup["3966"] = GD.Load<PackedScene>(base_dir + "ylands_block_glass_window_1x1x1_3966.tscn");
		YlandStandards.id_lookup["2756"] = GD.Load<PackedScene>(base_dir + "ylands_block_glass_window_2x2x1_2756.tscn");
		YlandStandards.id_lookup["5617"] = GD.Load<PackedScene>(base_dir + "ylands_block_glass_window_2x4x1_5617.tscn");
		YlandStandards.id_lookup["5618"] = GD.Load<PackedScene>(base_dir + "ylands_block_glass_window_4x4x1_5618.tscn");
		YlandStandards.id_lookup["3978"] = GD.Load<PackedScene>(base_dir + "ylands_ship_hull_wooden_large_3978.tscn");
	}

	static public void SetEntityColor(Node3D entity, List<float> color) {
		if (color.Count < 3) return;
		StandardMaterial3D mat = YlandStandards.GetEntitySurfaceMaterial(entity);
		if (mat == null) return;

		mat.AlbedoColor = new Color(
			color[0],
			color[1],
			color[2],
			mat.AlbedoColor.A
		);
		if (color.Count > 3 && color[3] > 0.001f) {
			mat.EmissionEnabled = true;
			mat.Emission = new Color(
				color[0] * color[3],
				color[1] * color[3],
				color[2] * color[3]
			);
			mat.EmissionEnergyMultiplier = color[3] * 20f;
			mat.RimEnabled = true;
			mat.Rim = 1.0f;
		} 
	}

	static public StandardMaterial3D GetEntitySurfaceMaterial(Node3D entity) {
		StandardMaterial3D mat = null;

		MeshInstance3D mesh = entity.GetChildOrNull<MeshInstance3D>(0);
		if (mesh == null) return null;

		mat = (StandardMaterial3D)mesh.GetSurfaceOverrideMaterial(0);
		mat ??= (StandardMaterial3D)mesh.Mesh.SurfaceGetMaterial(0);

		return mat;
	}
}

}