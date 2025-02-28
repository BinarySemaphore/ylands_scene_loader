using Godot;
using YlandsSceneLoader.scripts;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

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
	public bool load_scene;
	public bool combine_mesh;
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
		this.combine_mesh = (bool)this.GetMeta("mesh_combine_similar", false);

		raw_data = File.ReadAllText(blockdef_file);
		this.blocks = JsonSerializer.Deserialize<Dictionary<string, YlandBlockDef>>(raw_data, options);
		raw_data = File.ReadAllText(scene_file);
		this.scene = JsonSerializer.Deserialize<Dictionary<string, YlandSceneItem>>(raw_data, options);

		YlandStandards.PreloadLookups();

		this.load_scene = true;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (this.load_scene) {
			Stopwatch timer = new Stopwatch();
			timer.Start();
			this.BuildScene(this, this.scene);
			timer.Stop();
			GD.Print($"Load time: {1000*timer.ElapsedTicks/Stopwatch.Frequency:f2}ms");
			this.load_scene = false;
		}
	}

	public void BuildScene(Node3D parent, Dictionary<string, YlandSceneItem> root) {
		Node3D node;
		ComboMesh combo_mesh = new ComboMesh();
		Quaternion parent_inv_rotation = parent.Quaternion.Inverse();

		foreach (string key in root.Keys) {
			node = this.GetNodeFromItem(root[key]);
			if (node == null) continue;

			node.Name = $"[{key}] {node.Name}";
			node.Position = parent_inv_rotation * (node.Position - parent.Position);
			node.Quaternion = parent_inv_rotation * node.Quaternion;
			
			if (!this.combine_mesh) {
				parent.AddChild(node);
			// Ignore the rest of this method (if reviewing as simple example - combo_mesh is an advanced feature)
			} else {
				if (root[key].type == "group") {
					parent.AddChild(node);
					continue;
				}
				if (!combo_mesh.Append(node)) {
					parent.AddChild(combo_mesh.CommitToMesh(), true);
					combo_mesh.Append(node);
				}
				ComboMesh.ImmediateFreeNodeAndChildren(node);
			}
		}

		// Ignore this (if reviewing as simple example - combo_mesh is an advanced feature)
		if (this.combine_mesh) {
			parent.AddChild(combo_mesh.CommitToMesh(), true);
		}
	}

	public Node3D GetNodeFromItem(YlandSceneItem item) {
		Node3D node = null;

		if (item.type == "entity") {
			node = this.CreateNewEntityFromRef(item.blockdef);
			if (node != null) {
				if (item.colors != null && item.colors.Count >= 1) YlandStandards.SetEntityColor(node, item.colors[0]);
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
			if (block_ref.colors != null && block_ref.colors.Count >= 1) YlandStandards.SetEntityColor(node, block_ref.colors[0]);
		} else {
			GD.Print($"Unsupported Entity: {block_ref:s}");
		}

		return node;
	}
}
