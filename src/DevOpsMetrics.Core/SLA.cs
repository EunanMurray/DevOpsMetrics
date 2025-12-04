using System;
using System.Collections.Generic;

namespace DevOpsMetrics.Core
{
    /// <summary>
    /// Service Level Agreement (SLA): An agreement about the uptime for a service provided to a customer 
    /// </summary>
    public class SLA
    {
        public float ProcessSLA(List<KeyValuePair<DateTime, TimeSpan>> slaList, int numberOfDays)
        {
            if (slaList == null || slaList.Count == 0)
            {
                return -1;
            }

            // Filter by date and calculate total outage hours in a single pass
            DateTime cutoffDate = DateTime.Now.AddDays(-numberOfDays);
            double totalHoursOutage = 0;
            int count = 0;

            foreach (KeyValuePair<DateTime, TimeSpan> item in slaList)
            {
                if (item.Key > cutoffDate)
                {
                    totalHoursOutage += item.Value.TotalHours;
                    count++;
                }
            }

            if (count == 0)
            {
                return -1;
            }

            // Total number of hours in the number of days
            int totalNumberOfHours = numberOfDays * 24;

            // Calculate the SLA
            float sla = (float)(totalNumberOfHours - totalHoursOutage) / totalNumberOfHours;
            return sla;
        }

        public static string GetSLARating(float SLAPercent)
        {
            //Adding the most commonly used SLA's
            float oneNine = 0.9f; //90.0%
            float oneNineFive = 0.95f; //95.0%
            float twoNines = 0.99f; //99.0%
            float threeNines = 0.999f; //99.9%
            float threeNinesFive = 0.995f; //99.95%
            float fourNines = 0.9999f; //99.99%
            float fiveNines = 0.99999f; //99.999%
            float sixNines = 0.999999f; //99.9999%

            string rating = "";
            if (SLAPercent == -1) //no rating
            {
                rating = "no data";
            }
            else if (SLAPercent < oneNine) //low rating
            {
                rating = "less than 90% SLA";
            }
            else if (SLAPercent >= sixNines)
            {
                rating = "over 99.9999% SLA";
            }
            else if (SLAPercent >= fiveNines)
            {
                rating = "over 99.999% SLA";
            }
            else if (SLAPercent >= fourNines)
            {
                rating = "over 99.99% SLA";
            }
            else if (SLAPercent >= threeNines)
            {
                rating = "over 99.9% SLA";
            }
            else if (SLAPercent >= threeNinesFive)
            {
                rating = "over 99.5% SLA";
            }
            else if (SLAPercent >= twoNines)
            {
                rating = "over 99.0% SLA";
            }
            else if (SLAPercent >= oneNineFive)
            {
                rating = "over 95.0% SLA";
            }
            else if (SLAPercent >= oneNine)
            {
                rating = "over 90.0% SLA";
            }

            return rating;
        }
    }
}
