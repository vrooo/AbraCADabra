namespace AbraCADabra
{
    public class PatchC2Manager : PatchManager
    {
        public override string DefaultName => "Patch C2";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public PatchC2Manager(PointManager[,] points, PatchType patchType, int patchCountX, int patchCountZ)
            : base(points, patchType, patchCountX, patchCountZ, 2) { }
    }
}
