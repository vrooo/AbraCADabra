﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AbraCADabra.Serialization
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [Serializable]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlType(AnonymousType = true, Namespace = "http://mini.pw.edu.pl/mg1")]
    public partial class XmlTorus : XmlNamedType
    {
        public XmlVector3 Position { get; set; }

        public XmlVector3 Rotation { get; set; }

        public XmlVector3 Scale { get; set; }

        [XmlAttribute]
        public float MajorRadius { get; set; }

        [XmlAttribute]
        public float MinorRadius { get; set; }

        [XmlAttribute]
        public int VerticalSlices { get; set; }

        [XmlAttribute]
        public int HorizontalSlices { get; set; }

        public override TransformManager GetTransformManager(Dictionary<string, PointManager> points)
        {
            return new TorusManager(this);
        }
    }
}
