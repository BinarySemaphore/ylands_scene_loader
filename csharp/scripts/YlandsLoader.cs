using Godot;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public class YlandStandards
{
	static public float std_unit = 0.375f;
	static public float std_half_unit = 0.1875f;
	
	static public Dictionary<string, PackedScene> shape_lookup = new Dictionary<string, PackedScene>();
	static public Dictionary<string, PackedScene> type_lookup = new Dictionary<string, PackedScene>();
	static public Dictionary<string, PackedScene> id_lookup = new Dictionary<string, PackedScene>();

	static public void Preload() {
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
}

public class YlandBlockDef
{
	public string type {get; set;}
	public List<int> size {get; set;}
	public string shape {get; set;}
	public string material {get; set;}
	public List<List<float>> colors {get; set;}
	[JsonPropertyName("bb-center-offset")]
	public List<float> bb_center_offset {get; set;}
	[JsonPropertyName("bb-dimensions")]
	public List<float> bb_dimensions {get; set;}

	
	override public string ToString() {
		string output = "";
		JsonSerializerOptions options = new() {
			WriteIndented = true
		};
		output = $"<YlandBlockDef: {JsonSerializer.Serialize(this, options)}>";
		return output;
	}
}

public class YlandSceneItem
{
	public string type {get; set;}
	public string name {get; set;}
	public string blockdef {get; set;}
	public List<float> position {get; set;}
	public List<float> rotation {get; set;}
	public List<List<float>> colors {get; set;}
	[JsonPropertyName("bb-center-offset")]
	public List<float> bb_center_offset {get; set;}
	[JsonPropertyName("bb-dimensions")]
	public List<float> bb_dimensions {get; set;}
	public Dictionary<string, YlandSceneItem> children {get; set;}
}

public partial class YlandsLoader : Node3D
{
	public bool load;
	public bool unsupported_draw;
	public float unsupported_transparency;
	public Dictionary<string, YlandBlockDef> blocks;
	public Dictionary<string, YlandSceneItem> scene;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		string raw_data;
		JsonSerializerOptions options = new() {
			PropertyNameCaseInsensitive = true
		};
		
		string blockdef_file = Godot.ProjectSettings.GlobalizePath((string)this.GetMeta("blockdef_file"));
		string scene_file = Godot.ProjectSettings.GlobalizePath((string)this.GetMeta("scene_file"));
		this.unsupported_draw = (bool)this.GetMeta("box_draw_unsupported", true);
		this.unsupported_transparency = (float)this.GetMeta("unsupported_transparency", 0.5f);

		raw_data = File.ReadAllText(blockdef_file);
		this.blocks = JsonSerializer.Deserialize<Dictionary<string, YlandBlockDef>>(raw_data, options);
		raw_data = File.ReadAllText(scene_file);
		this.scene = JsonSerializer.Deserialize<Dictionary<string, YlandSceneItem>>(raw_data, options);

		YlandStandards.Preload();

		this.load = true;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (this.load) {
			this.BuildScene(this, this.scene);
			this.load = false;
		}
	}

	public void BuildScene(Node3D parent, Dictionary<string, YlandSceneItem> root) {
		Quaternion parent_inv_rotation = parent.Quaternion.Inverse();
		Node3D new_block = null;

		foreach (string key in root.Keys) {
			new_block = this.GetNodeFromItem(root[key]);
			if (new_block == null) continue;

			new_block.Name = $"[{key}] {new_block.Name}";
			new_block.Position = parent_inv_rotation * (new_block.Position - parent.Position);
			new_block.Quaternion = parent_inv_rotation * new_block.Quaternion;
			
			parent.AddChild(new_block);
		}
	}

	public Node3D GetNodeFromItem(YlandSceneItem item) {
		Node3D node = null;

		if (item.type == "entity") {
			node = this.CreateNewEntityFromRef(item.blockdef);
			if (node != null) {
				if (item.colors != null && item.colors.Count >= 1) this.SetEntityColor(node, item.colors[0]);
			}
		} else if (item.type == "group") {
			node = new Node3D();
		}

		if (node != null) {
			node.Name = item.name;
			node.Position = new Vector3(
				item.position[0],
				item.position[1],
				-item.position[2]
			);
			node.RotationDegrees = new Vector3(
				-item.rotation[0],
				-item.rotation[1],
				item.rotation[2]
			);

			if (item.children != null && item.children.Count > 0) this.BuildScene(node, item.children);
		}

		return node;
	}

	public Node3D CreateNewEntityFromRef(string ref_key) {
		Node3D node = null;
		MeshInstance3D mesh;

		if (!this.blocks.ContainsKey(ref_key)) {
			GD.Print($"No block reference for \"{ref_key}\"");
			return null;
		}
		YlandBlockDef block_ref = this.blocks[ref_key];

		if (YlandStandards.id_lookup.ContainsKey(ref_key)) {
			node = YlandStandards.id_lookup[ref_key].Instantiate<Node3D>();
		} else if (YlandStandards.type_lookup.ContainsKey(block_ref.type)) {
			node = YlandStandards.type_lookup[block_ref.type].InstantiateOrNull<Node3D>();
		} else if (YlandStandards.shape_lookup.ContainsKey(block_ref.shape)) {
			node = YlandStandards.shape_lookup[block_ref.shape].InstantiateOrNull<Node3D>();
			if (node == null) return null;

			mesh = node.GetChildOrNull<MeshInstance3D>(0);
			if (mesh == null) {
				node.QueueFree();
				return null;
			}

			mesh.Scale = new Vector3(
				YlandStandards.std_unit,
				YlandStandards.std_unit,
				YlandStandards.std_unit
			);
			mesh.Position = new Vector3(
				block_ref.bb_center_offset[0] / block_ref.size[0],
				block_ref.bb_center_offset[1] / block_ref.size[1],
				-block_ref.bb_center_offset[2] / block_ref.size[2]
			);
			node.Scale = new Vector3(
				block_ref.size[0],
				block_ref.size[1],
				block_ref.size[2]
			);
		} else if (this.unsupported_draw) {
			node = new Node3D();
			mesh = new MeshInstance3D {
				Mesh = new BoxMesh {
					Material = new StandardMaterial3D {
						Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
						AlbedoColor = new Color(0, 0, 0, this.unsupported_transparency)
					},
					Size = new Vector3(
						block_ref.bb_dimensions[0],
						block_ref.bb_dimensions[1],
						block_ref.bb_dimensions[2]
					)
				},
				Position = new Vector3(
					block_ref.bb_center_offset[0],
					block_ref.bb_center_offset[1],
					-block_ref.bb_center_offset[2]
				)
			};
			node.AddChild(mesh);
		}

		if (node != null) {
			if (block_ref.colors != null && block_ref.colors.Count >= 1) this.SetEntityColor(node, block_ref.colors[0]);
		} else {
			GD.Print($"Unsupported Entity: {block_ref:s}");
		}

		return node;
	}

	public void SetEntityColor(Node3D entity, List<float> color) {
		MeshInstance3D mesh = entity.GetChildOrNull<MeshInstance3D>(0);
		if (mesh == null || color.Count < 3) return;

		StandardMaterial3D mat = (StandardMaterial3D)mesh.GetSurfaceOverrideMaterial(0);
		mat ??= (StandardMaterial3D)mesh.Mesh.SurfaceGetMaterial(0);
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
}
