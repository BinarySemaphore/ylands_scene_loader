using Godot;
using Godot.Collections;

namespace YlandsSceneLoader.scripts {

public class ComboMeshItem
{
	public Array surface_data;
	public StandardMaterial3D material;

	public ComboMeshItem(Array surface_data, StandardMaterial3D material) {
		this.surface_data = surface_data;
		this.material = material;
	}
}

public class ComboMesh
{
	private int surface_count;
	private System.Collections.Generic.Dictionary<string, ComboMeshItem> cmesh;

	public ComboMesh() {
		this.cmesh = new System.Collections.Generic.Dictionary<string, ComboMeshItem>();
	}

	/// <param name="node"></param>
	/// <returns><see cref="bool"/>; true if added successfully; false if full or invalid</returns>
	public bool Append(Node3D node) {
		int index;
		MeshInstance3D mesh;

		// Check if valid mesh containing node
		mesh = node.GetChildOrNull<MeshInstance3D>(0);
		if (mesh == null || mesh.Mesh == null) return false;

		string color_uid = this.GetEntityColorUid(node);
		if (color_uid.Length == 0) return false;

		// Check if combined mesh is full
		if (!this.cmesh.ContainsKey(color_uid) && this.surface_count == RenderingServer.MaxMeshSurfaces) {
			return false;
		}

		// Apply node transfoorm to local verts and normals
		Vector3 vert;
		Vector3 norm;
		Array surface_data = mesh.Mesh.SurfaceGetArrays(0);
		Vector3[] local_verts = (Vector3[])surface_data[(int)Mesh.ArrayType.Vertex];
		Vector3[] local_normals = (Vector3[])surface_data[(int)Mesh.ArrayType.Normal];
		for (index = 0; index < local_verts.Length; index++) {
			vert = local_verts[index];
			norm = local_normals[index];
			vert *= mesh.Scale;
			vert = mesh.Quaternion * vert;
			vert += mesh.Position;
			vert *= node.Scale;
			vert = node.Quaternion * vert;
			vert += node.Position;
			norm = node.Quaternion * mesh.Quaternion * norm;
			local_verts[index] = vert;
			local_normals[index] = norm;
		}
		surface_data[(int)Mesh.ArrayType.Vertex] = local_verts;
		surface_data[(int)Mesh.ArrayType.Normal] = local_normals;

		if (!this.cmesh.ContainsKey(color_uid)) {
			// Add new surface data
			this.cmesh[color_uid] = new ComboMeshItem(
				surface_data,
				YlandStandards.GetEntitySurfaceMaterial(node)
			);
		} else {
			// Add to existing surface data
			Array source;
			Array target;
			int vert_count_orig = 0;
			for (index = 0; index < this.cmesh[color_uid].surface_data.Count; index++) {
				if (index >= surface_data.Count) break;
				source = (Array)surface_data[index];
				target = (Array)this.cmesh[color_uid].surface_data[index];
				if (source == null || source.Count == 0) continue;
				if (index == (int)Mesh.ArrayType.Vertex) {
					vert_count_orig = target.Count;
				}
				if (index == (int)Mesh.ArrayType.Index && vert_count_orig > 0) {
					for (int mindex = 0; mindex < source.Count; mindex++) {
						source[mindex] = (int)source[mindex] + vert_count_orig;
					}
				}
				target.AddRange<Variant>(source);
				this.cmesh[color_uid].surface_data[index] = target;
			}

			// Ensure modified generics are retyped properly so Godot C++ backend is a happy camper :)
			// Note: Must correspond with ComboMesh.CommitToMesh.mesh_flags
			this.cmesh[color_uid].surface_data[(int)Mesh.ArrayType.Vertex] =
			this.cmesh[color_uid].surface_data[(int)Mesh.ArrayType.Vertex].As<Vector3[]>();
			this.cmesh[color_uid].surface_data[(int)Mesh.ArrayType.Normal] =
			this.cmesh[color_uid].surface_data[(int)Mesh.ArrayType.Normal].As<Vector3[]>();
			this.cmesh[color_uid].surface_data[(int)Mesh.ArrayType.Tangent] =
			this.cmesh[color_uid].surface_data[(int)Mesh.ArrayType.Tangent].As<float[]>();
			this.cmesh[color_uid].surface_data[(int)Mesh.ArrayType.TexUV] =
			this.cmesh[color_uid].surface_data[(int)Mesh.ArrayType.TexUV].As<Vector2[]>();
			this.cmesh[color_uid].surface_data[(int)Mesh.ArrayType.Index] =
			this.cmesh[color_uid].surface_data[(int)Mesh.ArrayType.Index].As<int[]>();
		}

		this.surface_count += 1;

		return true;
	}

	public MeshInstance3D CommitToMesh(int id=0) {
		if (this.surface_count == 0) return null;

		// Create the actual mesh
		ArrayMesh new_mesh = new ArrayMesh();
		Mesh.ArrayFormat mesh_flags = Mesh.ArrayFormat.FormatVertex | Mesh.ArrayFormat.FormatNormal | Mesh.ArrayFormat.FormatTangent | Mesh.ArrayFormat.FormatTexUV | Mesh.ArrayFormat.FormatIndex;
		Array data;
		StandardMaterial3D mat;
		int index = 0;
		foreach (string key in this.cmesh.Keys) {
			data = this.cmesh[key].surface_data;
			mat = this.cmesh[key].material;
			new_mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data, null, null, mesh_flags);
			new_mesh.SurfaceSetMaterial(index, mat);
			index += 1;
		}

		MeshInstance3D mesh_container = new MeshInstance3D();
		mesh_container.Mesh = new_mesh;
		mesh_container.Position = Vector3.Zero;
		mesh_container.Quaternion = Quaternion.Identity;
		if (id > -1) {
			mesh_container.Name = $"[{id:d}] Combined Mesh";
		} else {
			mesh_container.Name = "Combined Mesh";
		}

		this.cmesh.Clear();
		this.surface_count = 0;

		return mesh_container;
	}

	private string GetEntityColorUid(Node3D entity) {
		string color_uid = "";
		StandardMaterial3D mat = YlandStandards.GetEntitySurfaceMaterial(entity);
		if (mat == null) return "";

		color_uid += mat.AlbedoColor.ToHtml();
		if (mat.EmissionEnabled) {
			color_uid += " e:" + mat.Emission.ToHtml();
		}

		return color_uid;
	}

	static public void ImmediateFreeNodeAndChildren(Node node) {
		Node child;
		for (int index = 0; index < node.GetChildCount(); index++) {
			child = node.GetChild(index);
			if (child == null || child.IsQueuedForDeletion()) continue;
			ComboMesh.ImmediateFreeNodeAndChildren(child);
		}
		node.Free();
	}
}

}