using System;
using System.Collections.Generic;

namespace DevOpsMetrics.Core
{
    /// <summary>
    /// Deployment frequency: How often we deploy to production
    /// </summary>
    public class DeploymentFrequency
    {
        /// <summary>
        /// Calculate deployment frequency from a list of deployments
        /// </summary>
        /// <param name="deploymentFrequencyList"></param>
        /// <param name="numberOfDays"></param>
        /// <returns></returns>
        public float ProcessDeploymentFrequency(List<KeyValuePair<DateTime, DateTime>> deploymentFrequencyList, int numberOfDays)
        {
            if (deploymentFrequencyList == null || numberOfDays <= 0)
            {
                return 0f;
            }

            // Count items within date range in a single pass
            DateTime cutoffDate = DateTime.Now.AddDays(-numberOfDays);
            int count = 0;

            foreach (KeyValuePair<DateTime, DateTime> item in deploymentFrequencyList)
            {
                if (item.Key > cutoffDate)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return 0f;
            }

            float deploymentsPerDay = (float)count / numberOfDays;
            return (float)Math.Round(deploymentsPerDay, 4);
        }

        public static string GetDeploymentFrequencyRating(float deploymentsPerDay)
        {
            float dailyDeployment = 1f;
            //float weeklyDeployment = 1f / 7f;
            float monthlyDeployment = 1f / 30f;
            //float everySixMonthsDeployment = 1f / (6f * 30f); //Every 6 months

            string rating = "";
            if (deploymentsPerDay <= 0f)
            {
                rating = "None";
            }
            else if (deploymentsPerDay >= dailyDeployment) //NOTE: Assumes on-demand deployments are <1 day
            {
                rating = "High";
            }
            else if (deploymentsPerDay < dailyDeployment && deploymentsPerDay >= monthlyDeployment) //Captures days to months
            {
                rating = "Medium";
            }
            else if (deploymentsPerDay < monthlyDeployment)
            {
                rating = "Low";
            }
            return rating;
        }
    }
}
