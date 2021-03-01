using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace serialPortStd.Methods
{
    class Helper
    {

        public static UInt16 checkSum(byte[] data)
        {
            UInt16 sum = 0;
            unchecked
            {
                foreach (byte b in data)
                {
                    sum += b;
                }
            }
            return sum;
        }

        public static bool GetControlSum(byte[] array)
        {
            bool result = false;

            byte[] last2 = new byte[2];
            lock (array)
            {
                try
                {
                    Array.Copy(array, array.Length - 2, last2, 0, 2);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " array length: " + array.Length);
                }
                var controlSum = BitConverter.ToString(last2).Replace("-", string.Empty);
                var checkSum = Helper.checkSum(array.Take(array.Length - 2).ToArray()).ToString("X2");

                if (checkSum.Contains(controlSum))
                {
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            return result;
        }

        public static string GetName()
        {

            if (PlatformHelper.IsWindows())
            {
                return "COM3";

            }
            else if (PlatformHelper.IsLinux())
            {
                return "/dev/ttyUSB0";
            }
            else
            {
                return null;
            }
        }
    }
}
