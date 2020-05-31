using System;
using System.Collections.Generic;
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
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial class XmlPatchPointRef
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public int Row { get; set; }

        [XmlAttribute]
        public int Column { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial class XmlPatchC0 : XmlNamedType
    {
        [XmlArrayItem("PointRef", IsNullable = false)]
        public XmlPatchPointRef[] Points { get; set; }

        [XmlAttribute]
        public bool ShowControlPolygon { get; set; }

        [XmlAttribute]
        public WrapType WrapDirection { get; set; }

        [XmlAttribute]
        public int RowSlices { get; set; }

        [XmlAttribute]
        public int ColumnSlices { get; set; }

        public override TransformManager GetTransformManager(Dictionary<string, PointManager> points)
        {
            return new PatchC0Manager(this, points);
        }
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

        [XmlAttribute]
        public bool ShowControlPolygon { get; set; }

        [XmlAttribute]
        public WrapType WrapDirection { get; set; }

        [XmlAttribute]
        public int RowSlices { get; set; }

        [XmlAttribute]
        public int ColumnSlices { get; set; }

        public override TransformManager GetTransformManager(Dictionary<string, PointManager> points)
        {
            return new PatchC2Manager(this, points);
        }
    }
}
