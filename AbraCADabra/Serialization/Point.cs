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
    public partial class XmlPoint : XmlNamedType
    {
        public XmlVector3 Position { get; set; }

        private PointManager pointManager;

        public override TransformManager GetTransformManager(Dictionary<string, PointManager> points)
        {
            if (pointManager == null)
            {
                pointManager = new PointManager(this);
            }
            return pointManager;
        }
    }
}
