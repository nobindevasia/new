﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using D2G.Iris.ML.Core.Enums;
using D2G.Iris.ML.Core.Models;
using D2G.Iris.ML.Core.Interfaces;

namespace D2G.Iris.ML.FeatureEngineering
{
    public class PCAFeatureSelector : IFeatureSelector
    {
        private readonly MLContext _mlContext;
        private readonly StringBuilder _report;

        public PCAFeatureSelector(MLContext mlContext)
        {
            _mlContext = mlContext;
            _report = new StringBuilder();
        }

        public Task<(IDataView transformedData, string[] selectedFeatures, string report)> SelectFeatures(
            MLContext mlContext,
            IDataView data,
            string[] candidateFeatures,
            ModelType modelType,
            string targetField,
            FeatureEngineeringConfig config)
        {
            _report.Clear();
            _report.AppendLine("\nPCA Feature Selection Results:");
            _report.AppendLine("----------------------------------------------");

            try
            {
                int numberOfComponents = config.NumberOfComponents;
                if (numberOfComponents <= 0 || numberOfComponents > candidateFeatures.Length)
                {
                    _report.AppendLine($"Warning: Invalid number of components ({numberOfComponents}). " +
                                      $"Using {Math.Min(candidateFeatures.Length, 3)} instead.");
                    numberOfComponents = Math.Min(candidateFeatures.Length, 3);
                }

                _report.AppendLine($"Applying PCA with {numberOfComponents} components");
                _report.AppendLine($"Original feature count: {candidateFeatures.Length}");

                var pipeline = mlContext.Transforms
                    .Concatenate("FeaturesTemp", candidateFeatures)
                    .Append(mlContext.Transforms.NormalizeMinMax("FeaturesNormalized", "FeaturesTemp"));

                var pcaPipeline = pipeline
                    .Append(mlContext.Transforms.ProjectToPrincipalComponents(
                        outputColumnName: "Features",
                        inputColumnName: "FeaturesNormalized",
                        rank: numberOfComponents));

                var transformedData = pcaPipeline.Fit(data).Transform(data);

                string[] pcaFeatureNames = Enumerable.Range(1, numberOfComponents)
                    .Select(i => $"PCA_Component_{i}")
                    .ToArray();

                _report.AppendLine("\nPCA transformation completed successfully.");
                _report.AppendLine("\nPCA Components:");
                foreach (var name in pcaFeatureNames)
                {
                    _report.AppendLine($"  - {name}");
                }

                return Task.FromResult((transformedData, pcaFeatureNames, _report.ToString()));
            }
            catch (Exception ex)
            {
                _report.AppendLine($"Error during PCA analysis: {ex.Message}");
                Console.WriteLine($"Full error details: {ex}");
                throw;
            }
        }
    }
}