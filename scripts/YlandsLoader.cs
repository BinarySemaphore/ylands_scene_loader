using Godot;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

public class YlandStandards
{
	static public float std_unit = 0.375f;
	static public float std_half_unit = 0.1875f;
}

public class YlandBlock
{
	public int id {get; set;}
	public string material {get; set;}
	public string shape {get; set;}
	public List<int> size {get; set;}
	public List<List<float>> colors {get; set;}
	[JsonPropertyName("bb-center-offset")]
	public List<float> bb_center_offset {get; set;}
	[JsonPropertyName("bb-dimensions")]
	public List<float> bb_dimensions {get; set;}
}

public class YlandScene
{
	public string type {get; set;}
	public string name {get; set;}
	[JsonPropertyName("block-ref")]
	public string block_ref {get; set;}
	public List<float> position {get; set;}
	public List<float> rotation {get; set;}
	public List<List<float>> colors {get; set;}
	[JsonPropertyName("bb-center-offset")]
	public List<float> bb_center_offset {get; set;}
	[JsonPropertyName("bb-dimensions")]
	public List<float> bb_dimensions {get; set;}
	public Dictionary<string, YlandScene> children {get; set;}
}

public partial class YlandsLoader : Node3D
{
	public bool load;
	public Dictionary<string, YlandBlock> blocks;
	public Dictionary<string, YlandScene> scene;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		string blockdef_file = Godot.ProjectSettings.GlobalizePath((string)this.GetMeta("blockdef_file"));
		string scene_file = Godot.ProjectSettings.GlobalizePath((string)this.GetMeta("scene_file"));

		string raw_data = File.ReadAllText(blockdef_file);
		JsonSerializerOptions options = new() {
			PropertyNameCaseInsensitive = true
		};
		this.blocks = JsonSerializer.Deserialize<Dictionary<string, YlandBlock>>(raw_data, options);

		raw_data = File.ReadAllText(scene_file);
		this.scene = JsonSerializer.Deserialize<Dictionary<string, YlandScene>>(raw_data, options);

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

	public void BuildScene(Node3D parent, Dictionary<string, YlandScene> root) {
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

	public Node3D GetNodeFromItem(YlandScene item) {
		Node3D node = null;
		MeshInstance3D mesh;

		if (item.type == "entity") {
			node = this.CreateNewEntityFromRef(item.block_ref);
			if (node != null) {
				if (item.colors != null && item.colors.Count >= 1) this.SetEntityColor(node, item.colors[0]);
			}
		} else if (item.type == "group") {
			node = new Node3D {
				Name = item.name
			};
		}

		if (node != null) {
			node.Name = item.name;
			node.Position = new Vector3(
				-item.position[0],
				item.position[1],
				item.position[2]
			);
			node.RotationDegrees = new Vector3(
				item.rotation[0],
				-item.rotation[1],
				-item.rotation[2]
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
		YlandBlock block_ref = this.blocks[ref_key];

		if (block_ref.shape == "STANDARD") {
			node = new Node3D();
			mesh = new MeshInstance3D {
				Name = "Mesh",
				Mesh = new BoxMesh() {
					Size = new Vector3(
						YlandStandards.std_unit,
						YlandStandards.std_unit,
						YlandStandards.std_unit
					),
					Material = new StandardMaterial3D()
				},
				Position = new Vector3(
					-block_ref.bb_center_offset[0] / block_ref.size[0],
					block_ref.bb_center_offset[1] / block_ref.size[1],
					block_ref.bb_center_offset[2] / block_ref.size[2]
				)
			};
			node.Scale = new Vector3(
				block_ref.size[0],
				block_ref.size[1],
				block_ref.size[2]
			);
			node.AddChild(mesh);
		} else if (block_ref.shape == "UNDEFINED") {
			if (ref_key == "MUSKET BALL 0x0x0") {
				node = new Node3D();
				mesh = new MeshInstance3D {
					Name = "Mesh",
					Mesh = new SphereMesh {
						Radius = 0.05f,
						Height = 0.1f,
						Material = new StandardMaterial3D()
					}
				};
				node.AddChild(mesh);
			}
		}

		if (node != null) {
			if (block_ref.colors != null && block_ref.colors.Count >= 1) this.SetEntityColor(node, block_ref.colors[0]);
		}

		return node;
	}

	public void SetEntityColor(Node3D entity, List<float> color) {
		MeshInstance3D mesh = entity.GetChildOrNull<MeshInstance3D>(0);
		if (mesh == null || color.Count < 3) return;
		StandardMaterial3D mat = (StandardMaterial3D)((PrimitiveMesh)mesh.Mesh).Material;
		mat.AlbedoColor = new Color(
			color[0],
			color[1],
			color[2]
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
