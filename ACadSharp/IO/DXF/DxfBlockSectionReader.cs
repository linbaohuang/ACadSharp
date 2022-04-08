﻿using ACadSharp.Blocks;
using ACadSharp.Entities;
using ACadSharp.Exceptions;
using ACadSharp.IO.Templates;
using ACadSharp.Tables;
using System.Diagnostics;

namespace ACadSharp.IO.DXF
{
	internal class DxfBlockSectionReader : DxfSectionReaderBase
	{
		public DxfBlockSectionReader(IDxfStreamReader reader, DxfDocumentBuilder builder, NotificationEventHandler notification = null)
			: base(reader, builder, notification)
		{
		}

		public override void Read()
		{
			//Advance to the first value in the section
			this._reader.ReadNext();

			//Loop until the section ends
			while (this._reader.LastValueAsString != DxfFileToken.EndSection)
			{
				if (this._reader.LastValueAsString == DxfFileToken.Block)
					this.readBlock();
				else
					throw new DxfException($"Unexpected token at the begining of a table: {this._reader.LastValueAsString}", this._reader.Line);
			}
		}

		private void readBlock()
		{
			Debug.Assert(this._reader.LastValueAsString == DxfFileToken.Block);

			//Read the table name
			this._reader.ReadNext();

			this.readCommonObjectData(out string name, out ulong handle, out ulong? ownerHandle);

			if (!this._builder.TryGetCadObject(ownerHandle, out BlockRecord record))
			{
				throw new System.Exception();
			}

			DwgEntityTemplate template = new DwgEntityTemplate(record.BlockEntity);

			Debug.Assert(this._reader.LastValueAsString == DxfSubclassMarker.Entity);

			this.readMapped<Entity>(record.BlockEntity, template);

			Debug.Assert(this._reader.LastValueAsString == DxfSubclassMarker.BlockBegin);

			this.readMapped<Block>(record.BlockEntity, template);

			while (this._reader.LastValueAsString != DxfFileToken.EndBlock)
			{
				DwgEntityTemplate entityTemplate = this.readEntity();

				if (entityTemplate == null)
					continue;

				//Add the object and the template to the builder
				this._builder.Templates[entityTemplate.CadObject.Handle] = entityTemplate;
				record.Entities.Add(entityTemplate.CadObject);
			}

			this.readBlockEnd(record.BlockEnd);
			this._builder.Templates[template.CadObject.Handle] = template;
		}

		private void readBlockEnd(BlockEnd block)
		{
			DwgEntityTemplate template = new DwgEntityTemplate(block);

			this.readCommonObjectData(template);

			this.readMapped<Entity>(block, template);

			this.readMapped<BlockEnd>(block, template);

			this._builder.Templates[template.CadObject.Handle] = template;
		}
	}
}