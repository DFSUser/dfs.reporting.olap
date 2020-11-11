/*
 * Digital Future Systems LLC, Russia, Perm
 * 
 * This file is part of dfs.reporting.olap.
 * 
 * dfs.reporting.olap is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * dfs.reporting.olap is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with dfs.reporting.olap.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfs.Reporting.Olap.Models;
using Dfs.Reporting.Olap.Services.Saiku.Models;
using Microsoft.AspNetCore.Http;

namespace Dfs.Reporting.Olap.Services.Saiku
{
    public interface IReportSaikuService
    {
        Task<IEnumerable<DimensionElement>> GetElements(DimensionElementRequest request, DatasourceMetadata metadata);
        Task<ReportQueryResult> ExecuteQuery(ReportQuery query, DatasourceMetadata metadata);
        Task CalculateTotals(ReportQueryResult result, DatasourceMetadata metadata);
    }

    public class ReportSaikuService : IReportSaikuService
    {
        private readonly ISaikuHttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContext;

        public ReportSaikuService(ISaikuHttpClient httpClient, IHttpContextAccessor httpContext)
        {
            _httpClient = httpClient;
            _httpContext = httpContext;
        }

        public async Task<IEnumerable<DimensionElement>> GetElements(DimensionElementRequest request, DatasourceMetadata metadata)
        {
            var postfix = SwitchMetadata(metadata);
            //await CheckSession(metadata);

            var dimension = metadata.Dimensions.SelectMany(d => d.Value)
                .First(d => d.Name == request.Dimension);

            var splitDimensionName = dimension.Name.Split(':').First();

            var elements = await _httpClient.GetDimensionElements(metadata, metadata.Name, splitDimensionName, dimension.Level, request.Search);

            foreach (var dimensionElement in elements)
            {
                dimensionElement.Name = dimensionElement.UniqueName;
                dimensionElement.UniqueName = null;
            }

            return elements;
        }

        public async Task<ReportQueryResult> ExecuteQuery(ReportQuery query, DatasourceMetadata metadata)
        {
            var postfix = SwitchMetadata(metadata);
            await CheckSession(metadata);

            var saikuQuery = new SaikuQuery
            {
                Cube = metadata.Saiku,
                Name = metadata.Name,
                QueryModel = ConvertModel(query, metadata)
            };

            var result = await _httpClient.ExecuteQuery(saikuQuery);

            return new ReportQueryResult
            {
                Cells = result.Cells
                    .Select(r => r.Select(c => new ReportQueryResultCell
                    {
                        Type = c.Type,
                        Value = c.Value,
                        Name = c.Type == ReportCellType.ColumnHeader || c.Type == ReportCellType.RowHeader ? c.Properties.UniqueName : null
                    }).ToArray())
                    .ToArray()
            };
        }

        public async Task CalculateTotals(ReportQueryResult result, DatasourceMetadata metadata)
        {
            await Task.Run(() => { });
        }

        private async Task CheckSession(DatasourceMetadata metadata)
        {
            await _httpClient.SetSession(new SaikuSetSession
            {
                Name = $"{metadata.Name}_{metadata.Saiku.Connection}",
                Cube = metadata.Saiku
            });
        }

        private string SwitchMetadata(DatasourceMetadata metadata)
        {
            var httpRequest = _httpContext.HttpContext.Request;
            var yearHeader = httpRequest.Headers["year"];

            if (yearHeader.Count <= 0) return null;
            
            var postfix = yearHeader.First().Replace("epos_report", "");

            metadata.Saiku.Connection += postfix;
            metadata.Saiku.Catalog += postfix;
            metadata.Saiku.Schema += postfix;

            return postfix;
        }

        private SaikuQueryModel ConvertModel(ReportQuery query, DatasourceMetadata metadata)
        {
            var axies = new List<SaikuQueryModelAxis>();
            foreach (var dimension in query.Dimensions.Where(d=>d.Key != "measure"))
            {
                var hierarchies = new List<SaikuQueryAxisHierarchie>();
                foreach (var elementRequest in dimension.Value)
                {
                    var metaDimension = metadata.Dimensions.SelectMany(d => d.Value)
                        .First(d => d.Name == elementRequest.Dimension);

                    var selection = elementRequest.Selection?.Values?.Any() == true
                        ? new SaikuQueryAxisHierarchiesLevelSelection
                        {
                            Members = elementRequest.Selection.Values
                                .Select(s => new SaikuQueryNameBase
                                {
                                    Name = SplitValue(PrepareSplitName(s.Name)),
                                    UniqueName = PrepareSplitName(s.Name),
                                    Caption = s.Caption
                                }).ToArray()
                        }
                        : null;

                    var level = new SaikuQueryAxisHierarchiesLevel
                    {
                        Caption = metaDimension.Caption,
                        Name = metaDimension.Level,
                        Selection = selection
                    };
                    var splitDimensionName = metaDimension.Name.Split(':').First();

                    var hierarchy = new SaikuQueryAxisHierarchie
                    {
                        Name = splitDimensionName,
                        Caption = metaDimension.Caption,
                        Dimension = metaDimension.Dimension,
                        Levels = new Dictionary<string, SaikuQueryAxisHierarchiesLevel>
                        {
                            {metaDimension.Level, level},
                        }
                    };
                    hierarchies.Add(hierarchy);
                }

                var saikuAxis = new SaikuQueryModelAxis
                {
                    Location = dimension.Key.ToUpper(),
                    Hierarchies = hierarchies.ToArray(),
                    NonEmpty = dimension.Key != "filter"
                };
                axies.Add(saikuAxis);

            }

            var details = new SaikuQueryModelDetails();

            var measure = query.Dimensions.FirstOrDefault(k => k.Key == "measure");
            if (measure.Value.Any())
            {
                var dimensionElements = measure.Value.First().Selection?.Values;
                details.Measures = dimensionElements?.Select(d =>
                    new SaikuQueryModelDetailsMeasure
                    {
                        Caption = d.Caption,
                        Name = d.Name,
                        UniqueName = d.UniqueName
                    }
                ).ToArray();
            }

            var model = new SaikuQueryModel
            {
                Axes = axies.ToDictionary(a=>a.Location, a=>a),
                Details = details
            };
            
            return model;
        }

        private string SplitValue(string value)
        {
            return value.Split('.').Last().Trim('[', ']');
        }

        private string PrepareSplitName(string value)
        {
            if (!value.Contains("]:["))
                return value;

            var index = value.IndexOf("]:[", StringComparison.InvariantCulture);

            return value.Substring(index + 2, value.Length - index - 2);
        }
    }
}
