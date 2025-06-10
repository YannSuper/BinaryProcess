using BinaryProcess;
using System;

namespace Binary.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Random random = new Random();
            double[,] data1 = new double[10, 2];
            for (int i = 0; i < data1.GetLength(0); i++)
            {
                for (int j = 0; j < data1.GetLength(1); j++)
                {
                    data1[i, j] = random.Next(100);
                }
            }

            BinaryArray.Save("D:\\data1.data", data1);

            double[,] data1s = BinaryArray.Read<double[,]>("D:\\data1.data");

            double[] data2 = new double[10];

            for (int i = 0; i < data2.Length; i++)
            {
                data2[i] = random.Next(100);
            }

            BinaryArray.Save("D:\\data2.data", data2);
            double[] data2s = BinaryArray.Read<double[]>("D:\\data2.data");

        }
    }
}
