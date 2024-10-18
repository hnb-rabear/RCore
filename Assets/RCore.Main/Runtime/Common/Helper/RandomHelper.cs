﻿/**
 * Author RadBear - Nguyen Ba Hung - nbhung71711@gmail.com 
 **/

using System;
using System.Collections.Generic;

namespace RCore.Common
{
    static class RandomExtension
    {
        public static void Shuffle<T>(this T[] array)
        {
            var rng = new Random();
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                var temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        public static void Shuffle<T>(this List<T> array)
        {
            var rng = new Random();
            int n = array.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                var temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
    }

    public class Randomize
    {
        /// <summary>
        /// Return a random index from an array of chances
        /// NOTE: total of chances value does not need to match 100
        /// </summary>
        public static int GetRandomIndexOfChances(float[] chances)
        {
            int index = 0;
            float totalRatios = 0;
            for (int i = 0; i < chances.Length; i++)
                totalRatios += chances[i];

            float random = UnityEngine.Random.Range(0, totalRatios);
            float temp2 = 0;
            for (int i = 0; i < chances.Length; i++)
            {
                if (chances[i] <= 0)
                    continue;

                temp2 += chances[i];
                if (temp2 > random)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        public static int GetRandomIndexOfChances(int[] chances)
        {
            int index = 0;
            int totalRatios = 0;
            for (int i = 0; i < chances.Length; i++)
                totalRatios += chances[i];

            int random = UnityEngine.Random.Range(0, totalRatios + 1);
            int temp2 = 0;
            for (int i = 0; i < chances.Length; i++)
            {
                if (chances[i] <= 0)
                    continue;

                temp2 += chances[i];
                if (temp2 > random)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        /// <summary>
        /// Return a random index from a list of chances
        /// NOTE: total of chances value does not need to match 100
        /// </summary>
        public static int GetRandomIndexOfChances(List<float> chances)
        {
            int index = 0;
            float totalRatios = 0;
            for (int i = 0; i < chances.Count; i++)
                totalRatios += chances[i];

            float random = UnityEngine.Random.Range(0, totalRatios);
            float temp2 = 0;
            for (int i = 0; i < chances.Count; i++)
            {
                if (chances[i] <= 0)
                    continue;

                temp2 += chances[i];
                if (temp2 > random)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        public static int GetRandomIndexOfChances(List<int> chances)
        {
            int index = 0;
            int totalRatios = 0;
            for (int i = 0; i < chances.Count; i++)
                totalRatios += chances[i];

            int random = UnityEngine.Random.Range(0, totalRatios + 1);
            int temp2 = 0;
            for (int i = 0; i < chances.Count; i++)
            {
                if (chances[i] <= 0)
                    continue;

                temp2 += chances[i];
                if (temp2 > random)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        public static int GetRandomIndexOfChances(List<int> chances, int totalRatios)
        {
            int index = 0;
            int random = UnityEngine.Random.Range(0, totalRatios);
            int temp2 = 0;
            for (int i = 0; i < chances.Count; i++)
            {
                if (chances[i] <= 0)
                    continue;

                temp2 += chances[i];
                if (temp2 > random)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
    }
}