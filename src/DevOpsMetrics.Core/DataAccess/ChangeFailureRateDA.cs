using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevOpsMetrics.Core.DataAccess.Common;
using DevOpsMetrics.Core.DataAccess.TableStorage;
using DevOpsMetrics.Core.Models.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevOpsMetrics.Core.DataAccess
{
    public class ChangeFailureRateDA
    {
        public static async Task<ChangeFailureRateModel> GetChangeFailureRate(bool getSampleData, TableStorageConfiguration tableStorageConfig,
                DevOpsPlatform targetDevOpsPlatform, string organization_owner, string project_repo, string branch, string buildName_workflowName,
                int numberOfDays, int maxNumberOfItems)
        {
            ListUtility<ChangeFailureRateBuild> utility = new();
            ChangeFailureRate changeFailureRate = new();
            if (getSampleData == false)
            {
                //Gets a list of change failure rate builds from Azure storage
                AzureTableStorageDA daTableStorage = new();
                JArray list = await daTableStorage.GetTableStorageItemsFromStorage(tableStorageConfig, tableStorageConfig.TableChangeFailureRate, PartitionKeys.CreateBuildWorkflowPartitionKey(organization_owner, project_repo, buildName_workflowName));
                List<ChangeFailureRateBuild> initialBuilds = JsonConvert.DeserializeObject<List<ChangeFailureRateBuild>>(list.ToString());

                // Pre-calculate cutoff date and check platform once
                DateTime cutoffDate = DateTime.Now.AddDays(-numberOfDays);
                bool isAzureDevOps = targetDevOpsPlatform == DevOpsPlatform.AzureDevOps;
                
                // Build the filtered list and date list in a single pass
                List<ChangeFailureRateBuild> builds = new(initialBuilds.Count);
                List<KeyValuePair<DateTime, bool>> dateList = new(initialBuilds.Count);
                
                foreach (ChangeFailureRateBuild item in initialBuilds)
                {
                    if (item.Branch == branch && item.StartTime > cutoffDate)
                    {
                        // Construct URL for Azure DevOps
                        if (isAzureDevOps)
                        {
                            item.Url = $"https://dev.azure.com/{organization_owner}/{project_repo}/_build/results?buildId={item.Id}&view=results";
                        }
                        builds.Add(item);
                        dateList.Add(new KeyValuePair<DateTime, bool>(item.StartTime, item.DeploymentWasSuccessful));
                    }
                }

                // Calculate the metric on all of the results
                float changeFailureRateMetric = changeFailureRate.ProcessChangeFailureRate(dateList, numberOfDays);

                // Filter the results to return the last n (maxNumberOfItems) and find max build duration in one pass
                List<ChangeFailureRateBuild> uiBuilds = utility.GetLastNItems(builds, maxNumberOfItems);
                float maxBuildDuration = 0f;
                foreach (ChangeFailureRateBuild item in uiBuilds)
                {
                    if (item.BuildDuration > maxBuildDuration)
                    {
                        maxBuildDuration = item.BuildDuration;
                    }
                }
                
                // Calculate build duration percentages
                if (maxBuildDuration > 0f)
                {
                    foreach (ChangeFailureRateBuild item in uiBuilds)
                    {
                        float interiumResult = (item.BuildDuration / maxBuildDuration) * 100f;
                        item.BuildDurationPercent = Scaling.ScaleNumberToRange(interiumResult, 0, 100, 20, 100);
                    }
                }

                ChangeFailureRateModel model = new()
                {
                    TargetDevOpsPlatform = targetDevOpsPlatform,
                    DeploymentName = buildName_workflowName,
                    ChangeFailureRateBuildList = uiBuilds,
                    ChangeFailureRateMetric = changeFailureRateMetric,
                    ChangeFailureRateMetricDescription = ChangeFailureRate.GetChangeFailureRateRating(changeFailureRateMetric),
                    NumberOfDays = numberOfDays,
                    MaxNumberOfItems = uiBuilds.Count,
                    TotalItems = builds.Count
                };
                return model;
            }
            else
            {
                //Get sample data
                List<ChangeFailureRateBuild> sampleBuilds = utility.GetLastNItems(GetSampleBuilds(), maxNumberOfItems);
                ChangeFailureRateModel model = new()
                {
                    TargetDevOpsPlatform = targetDevOpsPlatform,
                    DeploymentName = buildName_workflowName,
                    ChangeFailureRateBuildList = sampleBuilds,
                    ChangeFailureRateMetric = 2f / 10f,
                    ChangeFailureRateMetricDescription = ChangeFailureRate.GetChangeFailureRateRating(2f / 10f),
                    NumberOfDays = numberOfDays,
                    MaxNumberOfItems = sampleBuilds.Count,
                    TotalItems = sampleBuilds.Count
                };
                return model;
            }
        }

        public static async Task<bool> UpdateChangeFailureRate(TableStorageConfiguration tableStorageConfig,
               string organization_owner, string project_repo, string buildName_workflowName,
               int percentComplete, int numberOfDays)
        {
            //Gets a list of change failure rate builds
            AzureTableStorageDA daTableStorage = new();
            string partitionKey = PartitionKeys.CreateBuildWorkflowPartitionKey(organization_owner, project_repo, buildName_workflowName);
            JArray list = await daTableStorage.GetTableStorageItemsFromStorage(tableStorageConfig, tableStorageConfig.TableChangeFailureRate, partitionKey);
            List<ChangeFailureRateBuild> initialBuilds = JsonConvert.DeserializeObject<List<ChangeFailureRateBuild>>(list.ToString());

            // Pre-calculate cutoff date
            DateTime cutoffDate = DateTime.Now.AddDays(-numberOfDays);
            
            // Get the list of items we are going to process, within the date/day range
            List<ChangeFailureRateBuild> builds = new(initialBuilds.Count);
            foreach (ChangeFailureRateBuild item in initialBuilds)
            {
                if (item.StartTime > cutoffDate)
                {
                    builds.Add(item);
                }
            }

            (List<ChangeFailureRateBuild> positiveBuilds, List<ChangeFailureRateBuild> negativeBuilds) = GetPositiveAndNegativeLists(percentComplete, builds);

            //Make the updates
            TableStorageCommonDA tableChangeFailureRateDA = new(tableStorageConfig.StorageAccountConnectionString, tableStorageConfig.TableChangeFailureRate);
            foreach (ChangeFailureRateBuild item in positiveBuilds)
            {
                item.DeploymentWasSuccessful = true;
                await daTableStorage.UpdateChangeFailureRate(tableChangeFailureRateDA, item, partitionKey, true);
            }
            foreach (ChangeFailureRateBuild item in negativeBuilds)
            {
                item.DeploymentWasSuccessful = false;
                await daTableStorage.UpdateChangeFailureRate(tableChangeFailureRateDA, item, partitionKey, true);
            }

            return true;
        }

        public static (List<ChangeFailureRateBuild>, List<ChangeFailureRateBuild>) GetPositiveAndNegativeLists(int percent, List<ChangeFailureRateBuild> builds)
        {
            // Find the midpoint in the list based on the percent
            int midPoint = (int)(((double)percent / 100) * builds.Count);
            
            // Prepare two lists with estimated capacity
            List<ChangeFailureRateBuild> positiveBuilds = new(midPoint);
            List<ChangeFailureRateBuild> negativeBuilds = new(builds.Count - midPoint);

            // Split the list in a single pass
            for (int i = 0; i < builds.Count; i++)
            {
                if (i < midPoint)
                {
                    positiveBuilds.Add(builds[i]);
                }
                else
                {
                    negativeBuilds.Add(builds[i]);
                }
            }

            return (positiveBuilds, negativeBuilds);
        }

        public static IEnumerable<IEnumerable<ChangeFailureRateBuild>> Partition(IEnumerable<ChangeFailureRateBuild> items, int partitionSize)
        {
            if (partitionSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(partitionSize));
            }

            int innerListCounter = 0;
            int numberOfPackets = 0;
            foreach (var item in items)
            {
                innerListCounter++;
                if (innerListCounter == partitionSize)
                {
                    yield return items.Skip(numberOfPackets * partitionSize).Take(partitionSize);
                    innerListCounter = 0;
                    numberOfPackets++;
                }
            }

            if (innerListCounter > 0)
            {
                yield return items.Skip(numberOfPackets * partitionSize);
            }
        }

        //Return a sample dataset to help with testing
        private static List<ChangeFailureRateBuild> GetSampleBuilds()
        {
            List<ChangeFailureRateBuild> results = new();
            ChangeFailureRateBuild item1 = new()
            {
                StartTime = DateTime.Now.AddDays(-7).AddMinutes(-4),
                EndTime = DateTime.Now.AddDays(-7).AddMinutes(0),
                BuildDurationPercent = 70,
                BuildNumber = "1",
                Branch = "main",
                Status = "completed",
                Url = "https://dev.azure.com/samsmithnz/samlearnsazure/1",
                DeploymentWasSuccessful = true
            };
            results.Add(item1);
            results.Add(item1);
            results.Add(item1);
            results.Add(item1);
            ChangeFailureRateBuild item2 = new()
            {
                StartTime = DateTime.Now.AddDays(-5).AddMinutes(-5),
                EndTime = DateTime.Now.AddDays(-5).AddMinutes(0),
                BuildDurationPercent = 40,
                BuildNumber = "2",
                Branch = "main",
                Status = "completed",
                Url = "https://dev.azure.com/samsmithnz/samlearnsazure/2",
                DeploymentWasSuccessful = true
            };
            results.Add(item2);
            results.Add(item2);
            results.Add(item2);
            results.Add(item2);
            ChangeFailureRateBuild item3 = new()
            {
                StartTime = DateTime.Now.AddDays(-4).AddMinutes(-1),
                EndTime = DateTime.Now.AddDays(-4).AddMinutes(0),
                BuildDurationPercent = 20,
                BuildNumber = "3",
                Branch = "main",
                Status = "failed",
                Url = "https://dev.azure.com/samsmithnz/samlearnsazure/3",
                DeploymentWasSuccessful = false
            };
            results.Add(item3);
            results.Add(item3);
            ChangeFailureRateBuild item4 = new()
            {
                StartTime = DateTime.Now.AddDays(-3).AddMinutes(-4),
                EndTime = DateTime.Now.AddDays(-3).AddMinutes(0),
                BuildDurationPercent = 50,
                BuildNumber = "4",
                Branch = "main",
                Status = "completed",
                Url = "https://dev.azure.com/samsmithnz/samlearnsazure/4",
                DeploymentWasSuccessful = true
            };
            results.Add(item4);
            results.Add(item4);
            results.Add(item4);
            results.Add(item4);
            ChangeFailureRateBuild item5 = new()
            {
                StartTime = DateTime.Now.AddDays(-2).AddMinutes(-7),
                EndTime = DateTime.Now.AddDays(-2).AddMinutes(0),
                BuildDurationPercent = 60,
                BuildNumber = "5",
                Branch = "main",
                Status = "completed",
                Url = "https://dev.azure.com/samsmithnz/samlearnsazure/5",
                DeploymentWasSuccessful = true
            };
            results.Add(item5);
            results.Add(item5);
            results.Add(item5);
            results.Add(item5);
            ChangeFailureRateBuild item6 = new()
            {
                StartTime = DateTime.Now.AddDays(-1).AddMinutes(-5),
                EndTime = DateTime.Now.AddDays(-1).AddMinutes(0),
                BuildDurationPercent = 70,
                BuildNumber = "6",
                Branch = "main",
                Status = "inProgress",
                Url = "https://dev.azure.com/samsmithnz/samlearnsazure/6",
                DeploymentWasSuccessful = false
            };
            results.Add(item6);
            results.Add(item6);
            results.Add(item6);

            return results;
        }

    }
}
