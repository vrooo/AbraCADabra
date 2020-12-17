using System;
using System.Collections.Generic;
using OpenTK;

namespace AbraCADabra
{
    public enum PatchType
    {
        Simple, Cylinder
    }

    public static class CerealFactory
    {
        public static (PatchC0Manager, PointManager[,]) CreatePatchC0(Vector3 position,
            PatchType patchType, float widrad, float height, int countX, int countZ)
        {
            float ws = widrad / (countX * 6.0f);
            float hs = height / (countZ * 6.0f);
            PointManager[,] points = null;
            switch(patchType)
            {
                case PatchType.Cylinder:
                    {
                        var pointLayer = new List<Vector4>();

                        if (countX == 1)
                        {
                            float radMult = widrad * 5.0f / 3.0f;
                            Vector4 v1 = new Vector4(widrad, 0.0f, 0.0f, 1.0f);
                            Vector4 v2 = new Vector4(-radMult, -(radMult + widrad), 0.0f, 1.0f);
                            Vector4 v3 = new Vector4(-radMult, radMult + widrad, 0.0f, 1.0f);
                            pointLayer.AddMany(v1, v2, v3);
                        }
                        else
                        {
                            double angle = Math.PI / countX, angleTwo = 2 * angle;

                            // prepare first segments
                            double x = widrad * Math.Cos(angle);
                            double y = widrad * Math.Sin(angle);
                            double xx = (4 * widrad - x) / 3;
                            double yy = (widrad - x) * (3 * widrad - x) / (3 * y);
                            Vector4 v1 = new Vector4((float)x, (float)y, 0.0f, 1.0f);
                            Vector4 v2 = new Vector4((float)xx, (float)yy, 0.0f, 1.0f);
                            Vector4 v3 = new Vector4((float)xx, -(float)yy, 0.0f, 1.0f);
                            pointLayer.AddMany(v1, v2, v3);

                            // create remaining segments using rotations
                            for (int i = 1; i < countX; i++)
                            {
                                Matrix4 rot = Matrix4.CreateRotationZ((float)(i * angleTwo));
                                pointLayer.AddMany(rot * v1, rot * v2, rot * v3);
                            }
                        }

                        points = new PointManager[pointLayer.Count, 3 * countZ + 1];
                        // add translated layers
                        for (int j = 0, zds = -3 * countZ; zds <= 3 * countZ; j++, zds += 2)
                        {
                            float z = position.Z + zds * hs;
                            for (int i = 0; i < pointLayer.Count; i++)
                            {
                                var p = pointLayer[i];
                                points[i, j] = new PointManager(new Vector3(p.X + position.X, p.Y + position.Y, z), true);
                            }
                        }
                    }
                    break;

                case PatchType.Simple:
                    {
                        points = new PointManager[3 * countX + 1, 3 * countZ + 1];
                        float y = position.Y;
                        for (int j = 0, zds = -3 * countZ; zds <= 3 * countZ; j++, zds += 2)
                        {
                            float z = position.Z + zds * hs;
                            for (int i = 0, xds = -3 * countX; xds <= 3 * countX; i++, xds += 2)
                            {
                                float x = position.X + xds * ws;
                                points[i, j] = new PointManager(new Vector3(x, y, z), true);
                            }
                        }
                    }
                    break;
            }
            if (points == null)
            {
                points = new PointManager[0, 0];
            }
            var patch = new PatchC0Manager(points, patchType, countX, countZ);
            return (patch, points);
        }


        public static (PatchC2Manager, PointManager[,]) CreatePatchC2(Vector3 position,
            PatchType patchType, float widrad, float height, int countX, int countZ)
        {
            float ws = widrad / (countX * 2.0f);
            float hs = height / (countZ * 2.0f);
            PointManager[,] points = null;
            switch (patchType)
            {
                case PatchType.Cylinder:
                    {
                        var pointLayer = new List<Vector4>();

                        double angle = 2 * Math.PI / countX;
                        double x = widrad, y = 0;
                        Vector4 v = new Vector4((float)x, (float)y, 0.0f, 1.0f);
                        pointLayer.Add(v);
                        for (int i = 1; i < countX; i++)
                        {
                            Matrix4 rot = Matrix4.CreateRotationZ((float)(i * angle));
                            pointLayer.Add(rot * v);
                        }

                        points = new PointManager[pointLayer.Count, countZ + 3];
                        // add translated layers
                        for (int j = 0, zds = -countZ -2; zds <= countZ + 2; j++, zds += 2)
                        {
                            float z = position.Z + zds * hs;
                            for (int i = 0; i < pointLayer.Count; i++)
                            {
                                var p = pointLayer[i];
                                points[i, j] = new PointManager(new Vector3(p.X + position.X, p.Y + position.Y, z), true);
                            }
                        }
                    }
                    break;

                case PatchType.Simple:
                    {
                        points = new PointManager[countX + 3, countZ + 3];
                        float y = position.Y;
                        for (int j = 0, zds = -countZ - 2; zds <= countZ + 2; j++, zds += 2)
                        {
                            float z = position.Z + zds * hs;
                            for (int i = 0, xds = -countX - 2; xds <= countX + 2; i++, xds += 2)
                            {
                                float x = position.X + xds * ws;
                                points[i, j] = new PointManager(new Vector3(x, y, z), true);
                            }
                        }
                    }
                    break;
            }
            if (points == null)
            {
                points = new PointManager[0, 0];
            }
            var patch = new PatchC2Manager(points, patchType, countX, countZ);
            return (patch, points);
        }
    }
}
