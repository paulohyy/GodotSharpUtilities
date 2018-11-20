using Godot;
using SkipTheBadMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SkipTheBadRandom
{
    public class LockedUnboundRng : IDisposable
    {
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private LockedUnboundRng(int seed)
        {
            semaphore.Wait();
            Rng.Unbound(seed);
        }

        public static LockedUnboundRng LockAndSeed(int seed) => new LockedUnboundRng(seed);

        public void Dispose()
        {
            Rng.Deterministic();
            semaphore.Release();
        }
    }

    /// <summary>
    /// Only random number generator to be used in the projects. Keeps the randomness of the projects deterministic.
    /// </summary>
    public static class Rng
    {
        public static bool FlipCoin { get { return B(); } }
        public static float Probability { get { return F(0f, 1f); } }

        private static Random rand = new Random(DateTime.Now.Millisecond);
        private static Random Waiter = new Random(DateTime.Now.Millisecond);
        private static List<Func<object>> functions = new List<Func<object>>();

        /// <summary>
        /// [UNSAFE] Register a function for later use. Must keep track of the index to call it safely.
        /// </summary>
        public static int RegisterFunction(Func<object> func)
        {
            functions.Add(func);
            return functions.Count - 1;
        }

        /// <summary>
        /// [UNSAFE] Calls a registered function by its index.
        /// </summary>
        public static T Func<T>(int index) => (T)functions[index]();

        /// <summary>
        ///
        /// </summary>
        /// <param name="min">Inclusive</param>
        /// <param name="max">Exclusive</param>
        /// <returns></returns>
        public static int IUncapped(float min, float max)
        {
            return rand.Next((int)min, (int)max + 1);
        }

        public static int Int => rand.Next(int.MinValue, int.MaxValue);

        public static int I(int min, int max)
        {
            return rand.Next(min, max);
        }

        public static int GI(int min, int max, float mean = 0.5f)
        {
            var value = NormalizedG(mean);
            return (int)(min + ((max - min) * value));
        }

        /// <summary>
        /// Returns a float between min and max.
        /// </summary>
        public static float F(float min, float max)
        {
            return (float)rand.NextDouble() * (max - min) + min;
        }

        public static float Float => (float)rand.NextDouble();

        public static float GF(float min, float max, float mean = 0.5f)
        {
            return min + (NormalizedG(mean) * (max - min));
        }

        /// <summary>
        ///   Generates normally distributed numbers. Each operation makes two Gaussians for the price of one, and apparently they can be cached or something for better performance, but who cares.
        /// </summary>
        public static float Gaussian(float mean = 0, float deviation = 1)
        {
            var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(rand.NextDouble())) *
                                Math.Sin(2.0 * Math.PI * rand.NextDouble());

            var rand_normal = mean + deviation * rand_std_normal;

            return (float)rand_normal;
        }

        public static float NormalizedG(float mean = 0)
        {
            return Mathf.Clamp(Gaussian(mean, 0.25f), 0f, 1f);
        }

        /// <summary>
        ///   Generates values from a triangular distribution.
        /// </summary>
        public static float Triangular(this Random r, float minimum, float maximum, float mode)
        {
            var u = r.NextDouble();
            return (float)(u < (mode - minimum) / (maximum - minimum)
                       ? minimum + Math.Sqrt(u * (maximum - minimum) * (mode - minimum))
                       : maximum - Math.Sqrt((1 - u) * (maximum - minimum) * (maximum - mode)));
        }

        /// <summary>
        ///   Equally likely to return true or false.
        /// </summary>
        public static bool B(float chance = 0.5f)
        {
            return rand.NextDouble() < chance;
        }

        /// <summary>
        /// Has a specified chance of returning a positive sign
        /// </summary>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static short Sign(float threshold)
        {
            return (short)(rand.NextDouble() < threshold ? 1 : -1);
        }

        /// <summary>
        ///   Shuffles a list in O(n) time by using the Fisher-Yates/Knuth algorithm.
        /// </summary>
        public static List<T> Shuffle<T>(this List<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var j = rand.Next(0, i + 1);

                var temp = list[j];
                list[j] = list[i];
                list[i] = temp;
            }
            return list;
        }

        public static T[] Shuffle<T>(this T[] list)
        {
            var count = list.Count();
            for (var i = 0; i < count; i++)
            {
                var j = rand.Next(0, i + 1);

                var temp = list[j];
                list[j] = list[i];
                list[i] = temp;
            }
            return list;
        }

        /// <summary>
        /// Returns a random float between 0 and 1.
        /// </summary>
        public static float Next()
        {
            return (float)rand.NextDouble();
        }

        public static byte VaryWithin(this byte value, byte min, byte max, byte step) => value = (byte)MathSKP.Clamp(value + IUncapped(-step, step), min, max);
        public static int VaryWithin(this int value, int min, int max, int step) => value = MathSKP.Clamp(value + IUncapped(-step, step), min, max);
        public static float VaryWithin(this float value, float min, float max, float step) => value = MathSKP.Clamp(value + IUncapped(-step, step), min, max);

        public static T Enum<T>()
        {
            var v = System.Enum.GetValues(typeof(T));
            return (T)v.GetValue(I(0, int.MaxValue) % v.Length);
        }

        public static T GaussianEnum<T>(float pivot = -100f)
        {
            var v = System.Enum.GetValues(typeof(T));
            if (pivot == -100f)
                return (T)(v.GetValue(MathSKP.Clamp(GI(0, v.Length, Rng.F(0f, 0.15f)), 0, v.Length - 1)));
            pivot = MathSKP.Clamp(GF(pivot - 0.5f, pivot + 0.5f), 0f, 1f);
            var index = MathSKP.Clamp(GI(0, v.Length, pivot), 0, v.Length - 1);
            return (T)(v.GetValue(index));
        }

        public static T Enum<T>(params T[] negate)
        {
            var v = System.Enum.GetValues(typeof(T));
            var list = new List<T>();
            for (int i = 0; i < v.Length; i++)
            {
                var value = (T)v.GetValue(i);
                if (!negate.Contains(value))
                    list.Add(value);
            }
            return list.Value();
        }

        public static T Value<T>(this T[] source)
        {
            if (source.Length == 0)
                return default(T);

            return source[I(0, source.Length) % source.Length];
        }

        public static T Value<T>(this T[] source, params T[] exclude)
        {
            if (source.Length == 0)
                return default(T);

            var value = source[I(0, source.Length) % source.Length];
            while (exclude.Contains(value))
                value = source[I(0, source.Length) % source.Length];
            return value;
        }

        public static T Value<T>(this List<T> source)
        {
            return source[I(0, source.Count)];
        }

        public static T GaussianValue<T>(this T[] source, float mean = 0.5f)
        {
            var index = GI(0, source.Length - 1, mean);
            return source[index];
        }

        public static T GaussianValue<T>(this T[] source, float mean = 0.5f, params T[] exclude)
        {
            var value = source[GI(0, source.Length - 1, mean)];
            while (exclude.Contains(value))
                value = source[GI(0, source.Length - 1, mean)];
            return value;
        }

        public static T GaussianValue<T>(this List<T> source, float mean = 0.5f)
        {
            var index = GI(0, source.Count - 1, mean);
            return source[index];
        }

        public static T[] GaussianValues<T>(this List<T> source, int count, float mean = 0.5f)
        {
            return new bool[count].Select(b => source[GI(0, source.Count - 1, mean)]).ToArray();
        }

        public static T[] Values<T>(this T[] source, int count, bool tryDistinc = false)
        {
            var list = new List<T>();
            var maxTries = count * count;
            for (int i = 0; i < count; i++)
            {
                var item = source[I(0, source.Length)];
                int tries = 0;
                while (tryDistinc && list.Contains(item) && tries < maxTries)
                {
                    tries++;
                    item = source[I(0, source.Length)];
                }

                list.Add(item);
            }
            return list.ToArray();
        }

        public static T[] Values<T>(this List<T> source, int count, bool tryDistinc = false)
        {
            var list = new List<T>();
            var maxTries = count * count;
            for (int i = 0; i < count; i++)
            {
                var item = source[I(0, source.Count)];
                int tries = 0;
                while (tryDistinc && list.Contains(item) && tries < maxTries)
                {
                    tries++;
                    item = source[I(0, source.Count)];
                }

                list.Add(item);
            }
            return list.ToArray();
        }

        public static T Decide<T>(T A, T B, float chanceA = 0.5f)
        {
            return Rng.B(chanceA) ? A : B;
        }

        public static T PickAny<T>(params T[] values)
        {
            return values.Value();
        }

        public static T PickAny<T>(this T[] values, float[] probabilities)
        {
            var sum = probabilities.Sum();
            var pick = Probability;
            for (int i = 0; i < probabilities.Length; i++)
                probabilities[i] = i == 0 ? (probabilities[i] / sum) : ((probabilities[i] / sum) + probabilities[i - 1]);
            for (int i = 0; i < probabilities.Length; i++)
            {
                if (probabilities[i] != 0 && pick <= probabilities[i])
                    return values[i];
            }
            return values.Value();
        }

        public static int[] PseudoPerlinIntFrom(this int[] source, int count, int step, float noStepChance = 0f)
        {
            if (source == null)
                return new[] { 0 };

            if (source.Length == 1)
                return new int[count].Select(i => source[0]).ToArray();

            var indexes = PseudoPerlinInt(0, source.Length - 1, count, step, noStepChance);
            return indexes.Select(i => source[i]).ToArray();
        }

        public static int[] PseudoPerlinInt(int min, int max, int count, int step, float noStepChance = 0f)
        {
            var array = new int[count];

            var current = IUncapped(min, max);
            var sign = 1;
            float mid = (max - min) / 2;
            var extent = mid * 2;
            var twoThirds = (extent + mid) / 2;
            var countDown = 0;

            for (int i = 0; i < array.Length; i++)
            {
                countDown--;
                var distanceFromMid = Math.Abs(current - mid);
                if (B(distanceFromMid / twoThirds) && countDown <= 0)
                    sign *= -1;

                if (!B(noStepChance))
                    current += (IUncapped(0, step) * sign);

                if (current >= max)
                {
                    countDown = 10;
                    current = max;
                    sign = -1;
                }
                else if (current <= min)
                {
                    countDown = 10;
                    current = min;
                    sign = 1;
                }

                array[i] = current;
            }

            return array;
        }

        public static int Pow(int min, int max, int floor = 2)
        {
            if (floor == 1) return 1;
            if (floor == 2) return 1 << I(min, max + 1);
            return (int)Math.Pow(floor, I(min, max + 1));
        }

        public static int GaussianPow(int min, int max, float pivot, int floor = 2)
        {
            if (floor == 1) return 1;
            if (floor == 2) return 1 << GI(min, max, pivot);
            return (int)Math.Pow(floor, GI(min, max, pivot));
        }

        public static PairOf<T> ShuffleOrder<T>(T A, T B)
        {
            if (FlipCoin)
                return new PairOf<T>(A, B);
            return new PairOf<T>(B, A);
        }

        public static RngColor RGBA()
        {
            return new RngColor((byte)I(0, 256), (byte)I(0, 256), (byte)I(0, 256), (byte)I(0, 256));
        }

        public static RngColor VaryColor(this RngColor color, byte step)
        {
            color.R = color.R.VaryWithin(0, 255, step);
            color.G = color.G.VaryWithin(0, 255, step);
            color.B = color.B.VaryWithin(0, 255, step);
            return color;
        }

        public static void SetSeed(int seed)
        {
            rand = new Random(seed);
        }

        public static void SetSeedIfNotZero(ref int seed)
        {
            if (seed == 0)
            {
                SetSeed(DateTime.Now.Millisecond);
                seed = I(int.MinValue, int.MaxValue);
            }
            SetSeed(seed);
        }

        public static void Unbound(int seed = 0)
        {
            Waiter = rand;
            rand = new Random(seed == 0 ? DateTime.Now.Millisecond : seed);
        }

        public static void Deterministic()
        {
            rand = Waiter;
            Waiter = null;
        }

        private static readonly object rndLock = new object();

        public static void RunUnboundLocked(Action action)
        {
            lock (rndLock)
            {
                Rng.Unbound();
                action();
                Rng.Deterministic();
            }
        }

        public static void RunLocked(Action action)
        {
            lock (rndLock)
                action();
        }
    }
}
