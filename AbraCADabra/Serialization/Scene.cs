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
    [XmlRoot("Scene", Namespace = "http://mini.pw.edu.pl/mg1", IsNullable = false)]
    public partial class XmlScene
    {
        [XmlElement("BezierC0", typeof(XmlBezierC0))]
        [XmlElement("BezierC2", typeof(XmlBezierC2))]
        [XmlElement("BezierInter", typeof(XmlBezierInter))]
        [XmlElement("PatchC0", typeof(XmlPatchC0))]
        [XmlElement("PatchC2", typeof(XmlPatchC2))]
        [XmlElement("Point", typeof(XmlPoint))]
        [XmlElement("Torus", typeof(XmlTorus))]
        public XmlNamedType[] Items { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(Namespace = "http://mini.pw.edu.pl/mg1")]
    public abstract class XmlNamedType
    {
        [XmlAttribute]
        public string Name { get; set; }

        public abstract TransformManager GetTransformManager(Dictionary<string, PointManager> points);
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    //[System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial struct XmlVector3
    {
        [XmlAttribute]
        public float X { get; set; }

        [XmlAttribute]
        public float Y { get; set; }

        [XmlAttribute]
        public float Z { get; set; }

        public XmlVector3(OpenTK.Vector3 vec)
        {
            X = vec.X;
            Y = vec.Y;
            Z = vec.Z;
        }

        public OpenTK.Vector3 ToVector3()
        {
            return new OpenTK.Vector3(X, Y, Z);
        }
    }
}
