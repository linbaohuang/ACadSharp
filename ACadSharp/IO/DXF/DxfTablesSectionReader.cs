﻿using ACadSharp.Exceptions;
using ACadSharp.IO.Templates;
using ACadSharp.Tables;
using System;
using System.Diagnostics;

namespace ACadSharp.IO.DXF
{
	internal class DxfTablesSectionReader : DxfSectionReaderBase
	{
		public DxfTablesSectionReader(IDxfStreamReader reader, DxfDocumentBuilder builder, NotificationEventHandler notification = null)
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

				if (this._reader.LastValueAsString == DxfFileToken.TableEntry)
					this.readTable();
				else
					throw new DxfException($"Unexpected token at the begining of a table: {this._reader.LastValueAsString}", this._reader.Line);


				if (this._reader.LastValueAsString == DxfFileToken.EndTable)
					this._reader.ReadNext();
				else
					throw new DxfException($"Unexpected token at the end of a table: {this._reader.LastValueAsString}", this._reader.Line);
			}
		}

		private void readTable()
		{
			Debug.Assert(this._reader.LastValueAsString == DxfFileToken.TableEntry);

			//Read the table name
			this._reader.ReadNext();

			int nentries = 0;

			this.readCommonObjectData(out string name, out ulong handle, out ulong? ownerHandle);

			Debug.Assert(this._reader.LastValueAsString == DxfSubclassMarker.Table);

			this._reader.ReadNext();

			while (this._reader.LastDxfCode != DxfCode.Start)
			{
				switch (this._reader.LastCode)
				{
					//Maximum number of entries in table
					case 70:
						nentries = this._reader.LastValueAsInt;
						break;
					case 100 when this._reader.LastValueAsString == DxfSubclassMarker.DimensionStyleTable:
						while (this._reader.LastDxfCode != DxfCode.Start)
						{
							//Dimstyle has the code 71 for the count of entries
							//Also has 340 codes for each entry with the handles
							this._reader.ReadNext();
						}
						break;
					default:
						this._notification?.Invoke(null, new NotificationEventArgs($"Unhandeled dxf code {this._reader.LastCode} at line {this._reader.Line}."));
						break;
				}

				if (this._reader.LastDxfCode == DxfCode.Start)
					break;

				this._reader.ReadNext();
			}

			DwgTemplate template = null;

			switch (name)
			{
				case DxfFileToken.TableAppId:
					template = new DwgTableTemplate<AppId>(this._builder.DocumentToBuild.AppIds);
					this.readEntries((DwgTableTemplate<AppId>)template);
					break;
				case DxfFileToken.TableBlockRecord:
					template = new DwgBlockCtrlObjectTemplate(this._builder.DocumentToBuild.BlockRecords);
					this.readEntries((DwgBlockCtrlObjectTemplate)template);
					break;
				case DxfFileToken.TableVport:
					template = new DwgTableTemplate<VPort>(this._builder.DocumentToBuild.VPorts);
					this.readEntries((DwgTableTemplate<VPort>)template);
					break;
				case DxfFileToken.TableLinetype:
					template = new DwgTableTemplate<LineType>(this._builder.DocumentToBuild.LineTypes);
					this.readEntries((DwgTableTemplate<LineType>)template);
					break;
				case DxfFileToken.TableLayer:
					template = new DwgTableTemplate<Layer>(this._builder.DocumentToBuild.Layers);
					this.readEntries((DwgTableTemplate<Layer>)template);
					break;
				case DxfFileToken.TableStyle:
					template = new DwgTableTemplate<TextStyle>(this._builder.DocumentToBuild.TextStyles);
					this.readEntries((DwgTableTemplate<TextStyle>)template);
					break;
				case DxfFileToken.TableView:
					template = new DwgTableTemplate<View>(this._builder.DocumentToBuild.Views);
					this.readEntries((DwgTableTemplate<View>)template);
					break;
				case DxfFileToken.TableUcs:
					template = new DwgTableTemplate<UCS>(this._builder.DocumentToBuild.UCSs);
					this.readEntries((DwgTableTemplate<UCS>)template);
					break;
				case DxfFileToken.TableDimstyle:
					template = new DwgTableTemplate<DimensionStyle>(this._builder.DocumentToBuild.DimensionStyles);
					this.readEntries((DwgTableTemplate<DimensionStyle>)template);
					break;
				default:
					throw new DxfException($"Unknown table name {name}");
			}

			template.CadObject.Handle = handle;

			Debug.Assert(ownerHandle == null || ownerHandle.Value == 0);

			//Add the object and the template to the builder
			this._builder.Templates[template.CadObject.Handle] = template;
		}


		private void readEntries<T>(DwgTableTemplate<T> tableTemplate)
			where T : TableEntry
		{
			//Read all the entries until the end of the table
			while (this._reader.LastValueAsString != DxfFileToken.EndTable)
			{
				this.readCommonObjectData(out string name, out ulong handle, out ulong? ownerHandle);

				Debug.Assert(this._reader.LastValueAsString == DxfSubclassMarker.TableRecord);
				Debug.Assert(this._reader.LastValueAsString == DxfSubclassMarker.TableRecord);

				this._reader.ReadNext();

				DwgTemplate template = null;

				//Get the entry
				switch (name)
				{
					case DxfFileToken.TableAppId:
						AppId appid = new AppId();
						template = new DwgTableEntryTemplate<AppId>(appid);
						this.readMapped<AppId>(appid, template);
						break;
					case DxfFileToken.TableBlockRecord:
						BlockRecord record = new BlockRecord();
						template = new DwgBlockRecordTemplate(record);
						this.readMapped<BlockRecord>(record, template);
						break;
					case DxfFileToken.TableDimstyle:
						DimensionStyle dimStyle = new DimensionStyle();
						template = new DwgDimensionStyleTemplate(dimStyle);
						this.readMapped<DimensionStyle>(dimStyle, template);
						break;
					case DxfFileToken.TableLayer:
						Layer layer = new Layer();
						template = new DwgLayerTemplate(layer);
						this.readMapped<Layer>(layer, template);
						break;
					case DxfFileToken.TableLinetype:
						LineType ltype = new LineType();
						template = new CadLineTypeTemplate(ltype);
						this.readMapped<LineType>(ltype, template);
						break;
					case DxfFileToken.TableStyle:
						TextStyle style = new TextStyle();
						template = new DwgTableEntryTemplate<TextStyle>(style);
						this.readMapped<TextStyle>(style, template);
						break;
					case DxfFileToken.TableUcs:
						UCS ucs = new UCS();
						template = new CadUcsTemplate(ucs);
						this.readMapped<UCS>(ucs, template);
						break;
					case DxfFileToken.TableView:
						View view = new View();
						template = new DwgTableEntryTemplate<View>(view);
						this.readMapped<View>(view, template);
						break;
					case DxfFileToken.TableVport:
						VPort vport = new VPort();
						template = new DwgVPortTemplate(vport);
						this.readMapped<VPort>(vport, template);
						break;
					default:
						Debug.Fail($"Unhandeled table {name}.");
						break;
				}

				//Setup the common fields
				template.CadObject.Handle = handle;
				template.OwnerHandle = ownerHandle;
				tableTemplate.CadObject.Add((T)template.CadObject);

				//Add the object and the template to the builder
				this._builder.Templates[template.CadObject.Handle] = template;
			}
		}
	}
}