#if !TPS_BYTE_DECIMAL || !TPS_DECIMAL_BYTE
namespace tpsByteByte
{
	// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
	public partial class data
	{
		private dataStruct structField;

		private decimal versionField;

		/// <remarks/>
		public dataStruct @struct
		{
			get { return this.structField; }
			set { this.structField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public decimal version
		{
			get { return this.versionField; }
			set { this.versionField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStruct
	{
		private object[] itemsField;

		private ItemsChoiceType3[] itemsElementNameField;

		private string typeField;

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("QSize", typeof(dataStructQSize))]
		[System.Xml.Serialization.XmlElementAttribute("array", typeof(dataStructArray))]
		[System.Xml.Serialization.XmlElementAttribute("enum", typeof(dataStructEnum))]
		[System.Xml.Serialization.XmlElementAttribute("false", typeof(object))]
		[System.Xml.Serialization.XmlElementAttribute("filename", typeof(object))]
		[System.Xml.Serialization.XmlElementAttribute("int", typeof(byte))]
		[System.Xml.Serialization.XmlElementAttribute("key", typeof(string))]
		[System.Xml.Serialization.XmlElementAttribute("map", typeof(dataStructMap))]
		[System.Xml.Serialization.XmlElementAttribute("string", typeof(string))]
		[System.Xml.Serialization.XmlElementAttribute("struct", typeof(dataStructStruct))]
		[System.Xml.Serialization.XmlElementAttribute("true", typeof(object))]
		[System.Xml.Serialization.XmlElementAttribute("uint", typeof(ushort))]
		[System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
		public object[] Items
		{
			get { return this.itemsField; }
			set { this.itemsField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
		[System.Xml.Serialization.XmlIgnoreAttribute()]
		public ItemsChoiceType3[] ItemsElementName
		{
			get { return this.itemsElementNameField; }
			set { this.itemsElementNameField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type
		{
			get { return this.typeField; }
			set { this.typeField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructQSize
	{
		private object[] itemsField;

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("int", typeof(short))]
		[System.Xml.Serialization.XmlElementAttribute("key", typeof(string))]
		public object[] Items
		{
			get { return this.itemsField; }
			set { this.itemsField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructArray
	{
		private string[] filenameField;

		private dataStructArrayStruct structField;

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("filename")]
		public string[] filename
		{
			get { return this.filenameField; }
			set { this.filenameField = value; }
		}

		/// <remarks/>
		public dataStructArrayStruct @struct
		{
			get { return this.structField; }
			set { this.structField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructArrayStruct
	{
		private object[] itemsField;

		private ItemsChoiceType[] itemsElementNameField;

		private string typeField;

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("QSize", typeof(dataStructArrayStructQSize))]
		[System.Xml.Serialization.XmlElementAttribute("double", typeof(byte))]
		[System.Xml.Serialization.XmlElementAttribute("false", typeof(object))]
		[System.Xml.Serialization.XmlElementAttribute("key", typeof(string))]
		[System.Xml.Serialization.XmlElementAttribute("string", typeof(object))]
		[System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
		public object[] Items
		{
			get { return this.itemsField; }
			set { this.itemsField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
		[System.Xml.Serialization.XmlIgnoreAttribute()]
		public ItemsChoiceType[] ItemsElementName
		{
			get { return this.itemsElementNameField; }
			set { this.itemsElementNameField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type
		{
			get { return this.typeField; }
			set { this.typeField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructArrayStructQSize
	{
		private object[] itemsField;

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("int", typeof(sbyte))]
		[System.Xml.Serialization.XmlElementAttribute("key", typeof(string))]
		public object[] Items
		{
			get { return this.itemsField; }
			set { this.itemsField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(IncludeInSchema = false)]
	public enum ItemsChoiceType
	{
		/// <remarks/>
		QSize,

		/// <remarks/>
		@double,

		/// <remarks/>
		@false,

		/// <remarks/>
		key,

		/// <remarks/>
		@string,
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructEnum
	{
		private string typeField;

		private string valueField;

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type
		{
			get { return this.typeField; }
			set { this.typeField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlTextAttribute()]
		public string Value
		{
			get { return this.valueField; }
			set { this.valueField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructMap
	{
		private object[] itemsField;

		private string typeField;

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("key", typeof(dataStructMapKey))]
		[System.Xml.Serialization.XmlElementAttribute("struct", typeof(dataStructMapStruct))]
		public object[] Items
		{
			get { return this.itemsField; }
			set { this.itemsField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type
		{
			get { return this.typeField; }
			set { this.typeField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructMapKey
	{
		private string typeField;

		private string valueField;

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type
		{
			get { return this.typeField; }
			set { this.typeField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlTextAttribute()]
		public string Value
		{
			get { return this.valueField; }
			set { this.valueField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructMapStruct
	{
		private object[] itemsField;

		private ItemsChoiceType2[] itemsElementNameField;

		private string typeField;

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("double", typeof(byte))]
		[System.Xml.Serialization.XmlElementAttribute("false", typeof(object))]
		[System.Xml.Serialization.XmlElementAttribute("filename", typeof(string))]
		[System.Xml.Serialization.XmlElementAttribute("key", typeof(string))]
		[System.Xml.Serialization.XmlElementAttribute("point_f", typeof(string))]
		[System.Xml.Serialization.XmlElementAttribute("rect", typeof(string))]
		[System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
		public object[] Items
		{
			get { return this.itemsField; }
			set { this.itemsField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
		[System.Xml.Serialization.XmlIgnoreAttribute()]
		public ItemsChoiceType2[] ItemsElementName
		{
			get { return this.itemsElementNameField; }
			set { this.itemsElementNameField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type
		{
			get { return this.typeField; }
			set { this.typeField = value; }
		}
		
		public dataStructMapStruct()
		{
			
		}

		public dataStructMapStruct(dataStructMapStruct clone)
		{
			typeField = clone.typeField;
			
			itemsElementNameField = new ItemsChoiceType2[clone.itemsElementNameField.Length];
			for (var i = 0; i < clone.itemsElementNameField.Length; i++)
				itemsElementNameField[i] = clone.itemsElementNameField[i];

			itemsField = new object[clone.itemsField.Length];
			for (var i = 0; i < clone.itemsField.Length; i++)
				itemsField[i] = clone.itemsField[i];
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(IncludeInSchema = false)]
	public enum ItemsChoiceType2
	{
		/// <remarks/>
		@double,

		/// <remarks/>
		@false,

		/// <remarks/>
		filename,

		/// <remarks/>
		key,

		/// <remarks/>
		point_f,

		/// <remarks/>
		rect,
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructStruct
	{
		private object[] itemsField;

		private ItemsChoiceType1[] itemsElementNameField;

		private string typeField;

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("double", typeof(byte))]
		[System.Xml.Serialization.XmlElementAttribute("enum", typeof(dataStructStructEnum))]
		[System.Xml.Serialization.XmlElementAttribute("false", typeof(object))]
		[System.Xml.Serialization.XmlElementAttribute("int", typeof(byte))]
		[System.Xml.Serialization.XmlElementAttribute("key", typeof(string))]
		[System.Xml.Serialization.XmlElementAttribute("point_f", typeof(string))]
		[System.Xml.Serialization.XmlElementAttribute("string", typeof(object))]
		[System.Xml.Serialization.XmlElementAttribute("struct", typeof(dataStructStructStruct))]
		[System.Xml.Serialization.XmlElementAttribute("true", typeof(object))]
		[System.Xml.Serialization.XmlElementAttribute("uint", typeof(byte))]
		[System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
		public object[] Items
		{
			get { return this.itemsField; }
			set { this.itemsField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
		[System.Xml.Serialization.XmlIgnoreAttribute()]
		public ItemsChoiceType1[] ItemsElementName
		{
			get { return this.itemsElementNameField; }
			set { this.itemsElementNameField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type
		{
			get { return this.typeField; }
			set { this.typeField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructStructEnum
	{
		private string typeField;

		private string valueField;

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type
		{
			get { return this.typeField; }
			set { this.typeField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlTextAttribute()]
		public string Value
		{
			get { return this.valueField; }
			set { this.valueField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructStructStruct
	{
		private object[] itemsField;

		private string typeField;

		/// <remarks/>
		[System.Xml.Serialization.XmlElementAttribute("enum", typeof(dataStructStructStructEnum))]
		[System.Xml.Serialization.XmlElementAttribute("key", typeof(string))]
		[System.Xml.Serialization.XmlElementAttribute("uint", typeof(byte))]
		public object[] Items
		{
			get { return this.itemsField; }
			set { this.itemsField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type
		{
			get { return this.typeField; }
			set { this.typeField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
	public partial class dataStructStructStructEnum
	{
		private string typeField;

		private string valueField;

		/// <remarks/>
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type
		{
			get { return this.typeField; }
			set { this.typeField = value; }
		}

		/// <remarks/>
		[System.Xml.Serialization.XmlTextAttribute()]
		public string Value
		{
			get { return this.valueField; }
			set { this.valueField = value; }
		}
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(IncludeInSchema = false)]
	public enum ItemsChoiceType1
	{
		/// <remarks/>
		@double,

		/// <remarks/>
		@enum,

		/// <remarks/>
		@false,

		/// <remarks/>
		@int,

		/// <remarks/>
		key,

		/// <remarks/>
		point_f,

		/// <remarks/>
		@string,

		/// <remarks/>
		@struct,

		/// <remarks/>
		@true,

		/// <remarks/>
		@uint,
	}

	/// <remarks/>
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(IncludeInSchema = false)]
	public enum ItemsChoiceType3
	{
		/// <remarks/>
		QSize,

		/// <remarks/>
		array,

		/// <remarks/>
		@enum,

		/// <remarks/>
		@false,

		/// <remarks/>
		filename,

		/// <remarks/>
		@int,

		/// <remarks/>
		key,

		/// <remarks/>
		map,

		/// <remarks/>
		@string,

		/// <remarks/>
		@struct,

		/// <remarks/>
		@true,

		/// <remarks/>
		@uint,
	}
}
#endif