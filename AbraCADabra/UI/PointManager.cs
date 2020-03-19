﻿using OpenTK;

namespace AbraCADabra
{
    class PointManager : TransformManager
    {
        public override string DefaultName => "Point";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        private Point point;

        public PointManager(Vector3 position) : this(new Point(position)) { }

        public PointManager(Point point) : base(point)
        {
            this.point = point;
        }

        public override void Update() { }
    }
}
