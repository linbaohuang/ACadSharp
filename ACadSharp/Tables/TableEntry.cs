﻿using ACadSharp.Attributes;
using System;

namespace ACadSharp.Tables
{
	[DxfSubClass(DxfSubclassMarker.TableRecord, true)]
	public abstract class TableEntry : CadObject, INamedCadObject
	{
		public event EventHandler<OnNameChangedArgs> OnNameChanged;

		/// <inheritdoc/>
		public override string SubclassMarker => DxfSubclassMarker.TableRecord;

		/// <summary>
		/// Specifies the name of the object
		/// </summary>
		[DxfCodeValue(2)]
		public string Name
		{
			get { return this._name; }
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentNullException(nameof(value), $"Table entry [{this.GetType().FullName}] must have a name");
				}

				OnNameChanged?.Invoke(this, new OnNameChangedArgs(this._name, value));
				this._name = value;
			}
		}

		/// <summary>
		/// Standard flags
		/// </summary>
		[DxfCodeValue(70)]
		public StandardFlags Flags { get; set; }

		private string _name = string.Empty;

		internal TableEntry() { }

		public TableEntry(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name), $"{this.GetType().Name} must have a name.");

			this.Name = name;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return $"{this.ObjectName}:{this.Name}";
		}

		internal void SetUnrestrictedName(string name)
		{
			// Needed to bypass invalid table entries with no name assigned
			this._name = name;
		}
	}
}
