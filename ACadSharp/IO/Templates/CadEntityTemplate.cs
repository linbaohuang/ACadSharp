﻿using ACadSharp.Entities;
using ACadSharp.Tables;

namespace ACadSharp.IO.Templates
{
	internal class CadEntityTemplate : CadTemplate<Entity>
	{
		public byte EntityMode { get; set; }

		public byte? LtypeFlags { get; set; }

		public ulong? LayerHandle { get; set; }

		public string LayerName { get; set; }

		public ulong? LineTypeHandle { get; set; }

		public string LineTypeName { get; set; }

		public ulong? PrevEntity { get; set; }

		public ulong? NextEntity { get; set; }

		public ulong? ColorHandle { get; set; }

		public ulong? MaterialHandle { get; set; }

		public CadEntityTemplate(Entity entity) : base(entity) { }
		
		public override void Build(CadDocumentBuilder builder)
		{
			base.Build(builder);

			if(this.getTableReference(builder, this.LayerHandle, this.LayerName, out Layer layer))
			{
				this.CadObject.Layer = layer;
			}

			//Handle the line type for this entity
			if (this.LtypeFlags.HasValue)
			{
				switch (this.LtypeFlags)
				{
					case 0:
						//Get the linetype by layer
						this.CadObject.LineType = builder.LineTypes["ByLayer"];
						break;
					case 1:
						//Get the linetype by block
						this.CadObject.LineType = builder.LineTypes["ByBlock"];
						break;
					case 2:
						//Get the linetype by continuous
						this.CadObject.LineType = builder.LineTypes["Continuous"];
						break;
					case 3:
						this.applyLineType(builder);
						break;
				}
			}
			else if (this.LineTypeHandle.HasValue)
			{
				this.applyLineType(builder);
			}
			else if (!string.IsNullOrEmpty(this.LineTypeName) && builder.DocumentToBuild.LineTypes.TryGetValue(this.LineTypeName, out LineType ltype))
			{
				this.CadObject.LineType = ltype;
			}
			else if (!string.IsNullOrEmpty(this.LineTypeName) || this.LineTypeHandle.HasValue)
			{
				builder.Notify($"Could not assign the line type to entity | handle : {this.LineTypeHandle} | name : {this.LineTypeName}", NotificationType.Warning);
			}

			if (this.ColorHandle.HasValue)
			{
				var dwgColor = builder.GetCadObject<DwgColorTemplate.DwgColor>(this.ColorHandle.Value);

				if (dwgColor != null)
					this.CadObject.Color = dwgColor.Color;
			}
			else
			{
				//TODO: Set color by name, only for dxf?
			}
		}

		private void applyLineType(CadDocumentBuilder builder)
		{
			if (builder.TryGetCadObject<LineType>(this.LineTypeHandle, out LineType ltype))
			{
				this.CadObject.LineType = ltype;
			}
		}
	}

	internal class CadEntityTemplate<T> : CadEntityTemplate
			where T : Entity, new()
	{
		public new T CadObject { get { return (T)base.CadObject; } set { base.CadObject = value; } }

		public CadEntityTemplate() : base(new T()) { }

		public CadEntityTemplate(T entity) : base(entity)
		{
		}
	}
}
