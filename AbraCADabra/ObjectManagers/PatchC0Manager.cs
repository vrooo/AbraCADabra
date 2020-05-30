namespace AbraCADabra
{
    public class PatchC0Manager : PatchManager
    {
        public override string DefaultName => "Patch C0";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public PatchC0Manager(PointManager[,] points, PatchType patchType, int patchCountX, int patchCountZ)
            : base(points, patchType, patchCountX, patchCountZ, 0) { }
    }
}
