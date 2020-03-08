using System;
using System.Numerics;

namespace AbraCADabra
{
    public static class MathHelper
    {
        private static Matrix M;
        private static Matrix D;

        public static Vector3 GetNormal(double x, double y, double z)
        {
            double nx = 2 * M[0, 0] * x + (M[0, 1] + M[1, 0]) * y + (M[0, 2] + M[2, 0]) * z + (M[0, 3] + M[3, 0]);
            double ny = 2 * M[1, 1] * y + (M[0, 1] + M[1, 0]) * x + (M[1, 2] + M[2, 1]) * z + (M[1, 3] + M[3, 1]);
            double nz = 2 * M[2, 2] * z + (M[0, 2] + M[2, 0]) * x + (M[1, 2] + M[2, 1]) * y + (M[2, 3] + M[3, 2]);
            return Vector3.Normalize(new Vector3((float)nx, (float)ny, (float)nz));
        }

        public static Vector3 GetObserver(double x, double y, double z)
        {
            return Vector3.Normalize(new Vector3(-(float)x, -(float)y, -(float)z));
        }

        public static double GetIntensity(double x, double y, double z, double shine)
        {
            return Math.Pow(Math.Max(0, Vector3.Dot(GetObserver(x, y, z), GetNormal(x, y, z))), shine);
        }

        public static Matrix GetTranslationMatrix(double x, double y, double z)
        {
            return new Matrix(1, 0, 0, -x,
                              0, 1, 0, -y,
                              0, 0, 1, -z,
                              0, 0, 0, 1);
        }

        public static Matrix GetRotationMatrix(double x, double y, double z)
        {
            // inverse transform - matrices are transposed
            double sinx = Math.Sin(x), cosx = Math.Cos(x);
            Matrix rotX = new Matrix(1, 0, 0, 0,
                                     0, cosx, sinx, 0,
                                     0, -sinx, cosx, 0,
                                     0, 0, 0, 1);
            double siny = Math.Sin(y), cosy = Math.Cos(y);
            Matrix rotY = new Matrix(cosy, 0, -siny, 0,
                                     0, 1, 0, 0,
                                     siny, 0, cosy, 0,
                                     0, 0, 0, 1);
            double sinz = Math.Sin(z), cosz = Math.Cos(z);
            Matrix rotZ = new Matrix(cosz, sinz, 0, 0,
                                     -sinz, cosz, 0, 0,
                                     0, 0, 1, 0,
                                     0, 0, 0, 1);
            return rotX * rotY * rotZ;
        }

        public static Matrix GetScaleMatrix(double x, double y, double z)
        {
            return new Matrix(1 / x, 1 / y, 1 / z, 1);
        }

        public static void SetData(double ellA, double ellB, double ellC, Matrix translation, Matrix rotation, Matrix scale)
        {
            D = new Matrix(1 / (ellA * ellA), 1 / (ellB * ellB), 1 / (ellC * ellC), -1);
            Matrix transform = scale * rotation * translation; // reverse order!
            M = Matrix.Transpose(transform) * D * transform;
        }

        public static double? GetEllipsoidZ(double x, double y)
        {
            double a = M[2, 2];
            double b = (M[0, 2] + M[2, 0]) * x + 
                       (M[1, 2] + M[2, 1]) * y + 
                       (M[3, 2] + M[2, 3]);
            double c = (M[0, 0] * x + M[0, 3] + M[3, 0]) * x +
                       (M[1, 1] * y + M[1, 3] + M[3, 1]) * y +
                       (M[0, 1] + M[1, 0]) * x * y + M[3, 3];

            double delta = b * b - 4 * a * c;
            if (delta < 0)
            {
                return null;
            }
            else if (delta == 0)
            {
                return -b / (2 * a);
            }
            else
            {
                double sqrt = Math.Sqrt(delta);
                double sol1 = (-b + sqrt) / (2 * a), sol2 = (-b - sqrt) / (2 * a);
                // cropping negative Z
                if (sol1 <= 0)
                {
                    if (sol2 <= 0)
                    {
                        return null;
                    }
                    else
                    {
                        return sol2;
                    }
                }
                else if (sol2 <= 0)
                {
                    return sol1;
                }
                else
                {
                    return Math.Min(sol1, sol2);
                }
                // without cropping negative Z
                //if (Math.Abs(sol1) < Math.Abs(sol2))
                //{
                //    return sol1;
                //}
                //else
                //{
                //    return sol2;
                //}
            }
        }
    }
}
