using System;

namespace AbraCADabra
{
    public class Matrix
    {
        private readonly double[,] _values;

        public Matrix()
        {
            _values = new double[4, 4];
        }

        public Matrix(double[,] values)
        {
            if (values.GetLength(0) != 4 || values.GetLength(1) != 4)
                throw new ArgumentException("Array must be size 4 by 4.");
            _values = values;
        }

        public Matrix(double m00, double m01, double m02, double m03, double m10, double m11, double m12, double m13, double m20, double m21, double m22, double m23, double m30, double m31, double m32, double m33)
        {
            _values = new double[,] { { m00, m01, m02, m03 },
                                      { m10, m11, m12, m13 },
                                      { m20, m21, m22, m23 },
                                      { m30, m31, m32, m33 } };
        }

        public Matrix(double m00, double m11, double m22, double m33)
        {
            _values = new double[,] { { m00, 0, 0, 0 },
                                      { 0, m11, 0, 0 },
                                      { 0, 0, m22, 0 },
                                      { 0, 0, 0, m33 } };
        }

        public double this[int i, int j]
        {
            get { return _values[i, j]; }
            set { _values[i, j] = value; }
        }

        public static Matrix Transpose(Matrix m)
        {
            Matrix res = new Matrix();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    res[i, j] = m[j, i];
            return res;
        }

        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            Matrix res = new Matrix();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    res[i, j] = m1[i, j] + m2[i, j];
            return res;
        }

        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            Matrix res = new Matrix();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 4; k++)
                        res[i, j] += m1[i, k] * m2[k, j];
            return res;
        }
    }
}
