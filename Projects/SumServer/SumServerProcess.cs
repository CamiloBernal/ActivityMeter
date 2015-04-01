using System;
using System.Collections.Generic;
using System.Linq;

namespace SumServer
{
    public static class SumServerProcess
    {
        private static List<int> sumEntries = null;
        private static DateTime? lastSumDate = null;

        public static List<int> SumEntries
        {
            get
            {
                if (sumEntries == null)
                {
                    sumEntries = new List<int>();
                }
                return sumEntries;
            }
        }

        public static DateTime LastSumDate
        {
            get
            {
                if (!lastSumDate.HasValue)
                {
                    lastSumDate = DateTime.Now;
                }
                return lastSumDate.Value;
            }
        }

        public static int CurrentDateInterval
        {
            get
            {
                DateTime start = SumServerProcess.LastSumDate;
                DateTime now = DateTime.Now;
                TimeSpan diff = now.Subtract(start);
                return Convert.ToInt32(diff.TotalMilliseconds);
            }
        }

        public static int CurrentSum
        {
            get
            {
                int vRet = 0;
                vRet = SumEntries.Sum();
                return vRet;
            }
        }

        public static void ClearSum()
        {
            lastSumDate = DateTime.Now;
            sumEntries.Clear();
        }
    }
}