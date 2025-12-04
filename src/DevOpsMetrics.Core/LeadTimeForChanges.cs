using System;
using System.Collections.Generic;

namespace DevOpsMetrics.Core
{
    /// <summary>
    /// Lead time for changes: Time from committing a change to deployment to production
    /// </summary>
    public class LeadTimeForChanges
    {
        public float ProcessLeadTimeForChanges(List<KeyValuePair<DateTime, TimeSpan>> leadTimeForChangesList, int numberOfDays)
        {
            if (leadTimeForChangesList == null || leadTimeForChangesList.Count == 0)
            {
                return 0f;
            }

            // Filter by date and calculate total hours in a single pass
            DateTime cutoffDate = DateTime.Now.AddDays(-numberOfDays);
            double totalHours = 0;
            int count = 0;

            foreach (KeyValuePair<DateTime, TimeSpan> item in leadTimeForChangesList)
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

            float leadTimeForChanges = (float)(totalHours / count);
            return (float)Math.Round(leadTimeForChanges, 4);
        }

        public static string GetLeadTimeForChangesRating(float leadTimeForChangesInHours)
        {
            //float dailyDeployment = 24f;
            float weeklyDeployment = 24f * 7f;
            float monthlyDeployment = 24f * 30f;
            //float sixMonthDeployment = 24f * 30f * 6f;

            string rating = "";
            if (leadTimeForChangesInHours <= 0f) //no rating
            {
                rating = "None";
            }
            else if (leadTimeForChangesInHours <= weeklyDeployment) //between one day and one week/ or once a week or faster
            {
                rating = "High";
            }
            else if (leadTimeForChangesInHours > weeklyDeployment && leadTimeForChangesInHours <= monthlyDeployment) //between one week and one month
            {
                rating = "Medium";
            }
            else if (leadTimeForChangesInHours > monthlyDeployment) //more than once every month
            {
                rating = "Low";
            }
            //no rating else statement not required here, as all scenarios are covered above with < and >

            return rating;
        }
    }
}
