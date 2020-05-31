using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AbraCADabra.Serialization
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial class XmlBezierPointRef
    {
        [XmlAttribute()]
        public string Name { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial class XmlBezierC0 : XmlNamedType
    {
        [XmlArrayItem("PointRef", IsNullable = false)]
        public XmlBezierPointRef[] Points { get; set; }

        [XmlAttribute()]
        public bool ShowControlPolygon { get; set; }

        public override TransformManager GetTransformManager(Dictionary<string, PointManager> points)
        {
            return new Bezier3C0Manager(this, points);
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial class XmlBezierC2 : XmlNamedType
    {
        [XmlArrayItem("PointRef", IsNullable = false)]
        public XmlBezierPointRef[] Points { get; set; }

        [XmlAttribute()]
        public bool ShowBernsteinPoints { get; set; }

        [XmlAttribute()]
        public bool ShowBernsteinPolygon { get; set; }

        [XmlAttribute()]
        public bool ShowDeBoorPolygon { get; set; }

        public override TransformManager GetTransformManager(Dictionary<string, PointManager> points)
        {
            return new Bezier3C2Manager(this, points);
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial class XmlBezierInter : XmlNamedType
    {
        [XmlArrayItem("PointRef", IsNullable = false)]
        public XmlBezierPointRef[] Points { get; set; }

        [XmlAttribute()]
        public bool ShowControlPolygon { get; set; }

        public override TransformManager GetTransformManager(Dictionary<string, PointManager> points)
        {
            return new Bezier3InterManager(this, points);
        }
    }
}
