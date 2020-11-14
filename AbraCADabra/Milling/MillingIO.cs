using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AbraCADabra.Milling
{
    public static class MillingIO
    {
        private static Regex verificationRegex = new Regex("^([XYZ]-?[0-9]+\\.[0-9]{3})+$");
        private static Regex extractionRegex = new Regex("([XYZ])(-?[0-9]+\\.[0-9]{3})");

        public static (MillingPath path, ToolData toolData) ReadFile(string path)
        {
            string ext = Path.GetExtension(path);
            if (ext.Length != 4)
            {
                throw new ArgumentException("Incorrect file extension.");
            }
            var toolData = new ToolData();

            switch(ext[1])
            {
                case 'k':
                    toolData.IsFlat = false;
                    break;
                case 'f':
                    toolData.IsFlat = true;
                    break;
                default:
                    throw new ArgumentException("Incorrect file extension.");
            }

            char[] extChars = { ext[2], ext[3] };
            bool res = int.TryParse(new string(extChars), out toolData.Diameter);
            if (!res || toolData.Diameter <= 0)
            {
                throw new ArgumentException("Incorrect file extension.");
            }

            StreamReader sr;
            try
            {
                sr = new StreamReader(path);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Failed to open file.", e);
            }

            var moves = new List<Vector3>();
            for (int i = 0; !sr.EndOfStream; i++)
            {
                var line = sr.ReadLine();
                var split = line.Split(new string[]{ "G01" }, StringSplitOptions.None);
                if (split.Length == 2)
                {
                    if (verificationRegex.IsMatch(split[1]))
                    {
                        var matches = extractionRegex.Matches(split[1]);
                        if (matches.Count > 0)
                        {
                            var move = i > 0 ? moves[i - 1] : Vector3.Zero;
                            foreach (Match m in matches)
                            {
                                TransformAndSetCoordRead(m.Groups[1].Value, float.Parse(m.Groups[2].Value), ref move);
                            }
                            moves.Add(move);
                        }
                        else
                        {
                            throw new ArgumentException($"Incorrect instruction at line {i + 1}.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Incorrect instruction at line {i + 1}.");
                    }
                }
            }

            sr.Close();
            return (new MillingPath(moves), toolData);
        }

        public static void SaveFile(List<Vector3> points, ToolData toolData, string location, string name, int startMove)
        {
            string filename = name + "." + (toolData.IsFlat ? "f" : "k") + toolData.Diameter.ToString("D2");
            StreamWriter sw = new StreamWriter(Path.Combine(location, filename));

            foreach (var origPoint in points)
            {
                var point = TransformCoordsWrite(origPoint);
                sw.WriteLine("N{0}G01X{1:F3}Y{2:F3}Z{3:F3}", startMove, point.X, point.Y, point.Z);
                startMove++;
            }

            sw.Close();
        }

        private static void TransformAndSetCoordRead(string name, float val, ref Vector3 move)
        {
            switch (name)
            {
                case "X":
                    move.X = val * MillingPath.SCALE;
                    break;
                case "Y":
                    move.Z = -val * MillingPath.SCALE;
                    break;
                case "Z":
                    move.Y = val * MillingPath.SCALE;
                    break;
            }
        }

        private static Vector3 TransformCoordsWrite(Vector3 move)
        {
            var res = new Vector3(move.X, -move.Z, move.Y);
            res /= MillingPath.SCALE;
            return res;
        }
    }
}
