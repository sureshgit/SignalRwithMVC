using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetNow.SampleApp
{
    public class Utils
    {
        public static Random random = new Random();
        //public static float NextFloat()
        //{
        //    double mantissa = (random.NextDouble() * 2.0) - 1.0;
        //    double exponent = Math.Pow(2.0, random.Next(-126, 128));
        //    return (float)(mantissa * exponent);
        //}
        public static string RandomString()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}