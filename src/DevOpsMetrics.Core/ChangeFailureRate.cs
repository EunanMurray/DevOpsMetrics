using System;
using System.Collections.Generic;
using System.Linq;

namespace DevOpsMetrics.Core
{
    /// <summary>
    /// Change failure rate: after a production deployment, was it successful? Or did we need to deploy a fix/rollback?
    /// </summary>
    public class ChangeFailureRate
    {
        public float ProcessChangeFailureRate(List<KeyValuePair<DateTime, bool>> changeFailureRateList, int numberOfDays)
        {
            if (changeFailureRateList == null || changeFailureRateList.Count == 0)
            {
                return -1;
            }

            // Filter by date and calculate failure rate in a single pass
            DateTime cutoffDate = DateTime.Now.AddDays(-numberOfDays);
            int totalCount = 0;
            int failureCount = 0;

            foreach (KeyValuePair<DateTime, bool> item in changeFailureRateList)
            {
                if (item.Key > cutoffDate)
                {
                    totalCount++;
                    if (!item.Value)
                    {
                        failureCount++;
                    }
                }
            }

            if (totalCount == 0)
            {
                return -1;
            }

            float changeFailureRate = (float)failureCount / totalCount;
            return (float)Math.Round(changeFailureRate, 4);
        }

        public static string GetChangeFailureRateRating(float changeFailureRate)
        {
            string rating;
            if (changeFailureRate < 0)
            {
                rating = "None";
            }
            else if (changeFailureRate <= 0.15f) //0-15%
            {
                rating = "High";
            }
            else if (changeFailureRate <= 0.30f) //16-30% 
            {
                rating = "Medium";
            }
            else if (changeFailureRate <= 1.00f) //46-60% (not a typo, overriding table from 46-60 to < 100% to create a range and capture all possible values)
            {
                rating = "Low";
            }
            else //no rating
            {
                rating = "None";
            }
            return rating;
        }
    }
}
