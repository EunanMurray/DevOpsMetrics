using System;
using System.Collections.Generic;

namespace DevOpsMetrics.Core
{
    /// <summary>
    /// Mean time to restore (MTTR): How quickly we can restore production in an outage or degradation
    /// </summary>
    public class MeanTimeToRestore
    {
        public float ProcessMeanTimeToRestore(List<KeyValuePair<DateTime, TimeSpan>> meanTimeToRestoreList, int numberOfDays)
        {
            if (meanTimeToRestoreList == null || meanTimeToRestoreList.Count == 0)
            {
                return 0f;
            }

            // Filter by date and calculate total hours in a single pass
            DateTime cutoffDate = DateTime.Now.AddDays(-numberOfDays);
            double totalHours = 0;
            int count = 0;

            foreach (KeyValuePair<DateTime, TimeSpan> item in meanTimeToRestoreList)
            {
                if (item.Key > cutoffDate)
                {
                    totalHours += item.Value.TotalHours;
                    count++;
                }
            }

            if (count == 0)
            {
                return 0f;
            }

            float meanTimeForChanges = (float)(totalHours / count);
            return (float)Math.Round(meanTimeForChanges, 2);
        }

        public static string GetMeanTimeToRestoreRating(float meanTimeToRestoreInHours)
        {
            //float hourlyRestoration = 1f;
            float dailyRestoration = 24f;
            float weeklyRestoration = 24f * 7f;

            string rating = "";
            if (meanTimeToRestoreInHours <= 0)
            {
                rating = "None";
            }
            else if (meanTimeToRestoreInHours < dailyRestoration) //less than one day
            {
                rating = "High";
            }
            else if (meanTimeToRestoreInHours < weeklyRestoration) //less than one day and one week
            {
                rating = "Medium";
            }
            else if (meanTimeToRestoreInHours > weeklyRestoration) //more than one week (originally 1 week - 1 month)
            {
                rating = "Low";
            }
            //no rating else statement not required here, as all scenarios are covered above with < and >

            return rating;
        }
    }
}
