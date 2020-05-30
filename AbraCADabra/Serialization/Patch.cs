using System;
using System.Xml.Serialization;

namespace AbraCADabra.Serialization
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [XmlType(Namespace = "http://mini.pw.edu.pl/mg1")]
    public enum WrapType
    {
        Row, Column, None
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial class XmlPatchPointRef
    {
        [XmlAttribute()]
        public string Name { get; set; }

        [XmlAttribute(DataType = "integer")]
        public int Row { get; set; }

        [XmlAttribute(DataType = "integer")]
        public int Column { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial class XmlPatchC0 : XmlNamedType
    {
        [XmlArrayItem("PointRef", IsNullable = false)]
        public XmlPatchPointRef[] Points { get; set; }

        [XmlAttribute()]
        public bool ShowControlPolygon { get; set; }

        [XmlAttribute()]
        public WrapType WrapDirection { get; set; }

        [XmlAttribute(DataType = "integer")]
        public int RowSlices { get; set; }

        [XmlAttribute(DataType = "integer")]
        public int ColumnSlices { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial class XmlPatchC2 : XmlNamedType
    {
        [XmlArrayItem("PointRef", IsNullable = false)]
        public XmlPatchPointRef[] Points { get; set; }

        [XmlAttribute()]
        public bool ShowControlPolygon { get; set; }

        [XmlAttribute()]
        public WrapType WrapDirection { get; set; }

        [XmlAttribute(DataType = "integer")]
        public int RowSlices { get; set; }

        [XmlAttribute(DataType = "integer")]
        public int ColumnSlices { get; set; }
    }
}
