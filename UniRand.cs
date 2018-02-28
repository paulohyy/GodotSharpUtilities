using System;
using System.Collections;

namespace SkipTheBadEngine
{
    /// <summary>
    /// Only random number generator to be used in the projects. Keeps the randomness of the projects deterministic.
    /// </summary>
    public static class UniRand
    {
        private static Random rand = new Random(DateTime.Now.Millisecond);

        public static int NextInt(float min, float max)
        {
            return rand.Next((int)min, (int)max);
        }

        /// <summary>
        /// Returns a float between min and max.
        /// </summary>
        public static float NextFloat(float min, float max)
        {
            return (float)rand.NextDouble() * (max - min) + min;
        }

        /// <summary>
        ///   Generates normally distributed numbers. Each operation makes two Gaussians for the price of one, and apparently they can be cached or something for better performance, but who cares.
        /// </summary>
        public static float NextGaussian(float mean = 0, float deviation = 1)
        {
            var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(rand.NextDouble())) *
                                Math.Sin(2.0 * Math.PI * rand.NextDouble());

            var rand_normal = mean + deviation * rand_std_normal;

            return (float)rand_normal;
        }

        /// <summary>
        ///   Generates values from a triangular distribution.
        /// </summary>
        public static float NextTriangular(this Random r, float minimum, float maximum, float mode)
        {
            var u = r.NextDouble();
            return (float)(u < (mode - minimum) / (maximum - minimum)
                       ? minimum + Math.Sqrt(u * (maximum - minimum) * (mode - minimum))
                       : maximum - Math.Sqrt((1 - u) * (maximum - minimum) * (maximum - mode)));
        }

        /// <summary>
        ///   Equally likely to return true or false.
        /// </summary>
        public static bool NextBoolean()
        {
            return rand.Next(2) > 0;
        }

        /// <summary>
        ///   Shuffles a list in O(n) time by using the Fisher-Yates/Knuth algorithm.
        /// </summary>
        public static void Shuffle(IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var j = rand.Next(0, i + 1);

                var temp = list[j];
                list[j] = list[i];
                list[i] = temp;
            }
        }

        /// <summary>
        /// Returns a random float between 0 and 1.
        /// </summary>
        public static float Next()
        {
            return (float)rand.NextDouble();
        }

        public static void SetSeed(int seed)
        {
            rand = new Random(seed);
        }
    }
}
