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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Dfs.Reporting.Olap.Services.Database
{
    public interface IReportDatabaseService
    {
        Task<IEnumerable<DimensionElement>> GetElements(DimensionElementRequest request, DatasourceMetadataDimension dimension);
        Task<ReportQueryResult> ExecuteQuery(ReportQuery query, DatasourceMetadata metadata);
        Task CalculateTotals(ReportQueryResult result, DatasourceMetadata metadata);
    }

    public class ReportDatabaseService : IReportDatabaseService
    {
        private readonly ILogger<ReportDatabaseService> _logger;
        private readonly MetadataDatabaseContext _context;
        private readonly ICookieService _cookieService;

        public ReportDatabaseService(ILogger<ReportDatabaseService> logger, MetadataDatabaseContext context, ICookieService cookieService)
        {
            _logger = logger;
            _context = context;
            _cookieService = cookieService;
        }

        public async Task<IEnumerable<DimensionElement>> GetElements(DimensionElementRequest request, DatasourceMetadataDimension dimension)
        {
            var cookie = _cookieService.ReadClaims();
            var data = await InternalGetElements(request, dimension);

            var result = data.Select(d => new DimensionElement
            {
                Name = d.Name,
                Caption = d.Caption
            });

            try
            {
                if (cookie == null)
                    return result;

                switch (request.Dimension)
                {
                    case "District" when cookie.ContainsKey("ATTR_DISTRICT"):
                    {
                        result = FilterDistrict(cookie, result);
                        break;
                    }
                    case "School" when cookie.ContainsKey("ATTR_SCHOOL"):
                    {
                        result = FilterSchool(cookie, result);
                        break;
                    }
                    case "Class":
                        result = FilterClass(cookie, result);
                        break;
                    case "Teacher":
                        result = FilterTeacher(cookie, result);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return result;
        }

        private IEnumerable<DimensionElement> FilterDistrict(Dictionary<string, string> cookie, IEnumerable<DimensionElement> result)
        {
            var roles = cookie["roles"];
            if (roles.Contains("admin"))
                return result;

            var district = cookie["ATTR_DISTRICT"];

            if (string.IsNullOrEmpty(district)) 
                return result;

            _logger.LogDebug($"*** User district: {district}");

            var splitDistrics = district.Split(',').Select(int.Parse).ToList();

            var parents = _context.DistrictParents
                .Where(d => splitDistrics.Contains((int)d.Id))
                .Select(c => new DimensionElement { Name = c.DistrictParentId.ToString() });

            foreach (var parent in parents)
            {
                _logger.LogInformation($"*** parent DISTRICT: {parent.Name}/{parent.Caption}");
            }

            var subresult = new List<DimensionElement>();
            foreach (var parent in parents)
            {
                var extresult = result.FirstOrDefault(r => r.Name.Equals(parent.Name));
                if (extresult != null)
                    subresult.Add(extresult);
            }

            result = subresult;

            return result;
        }

        private IEnumerable<DimensionElement> FilterSchool(Dictionary<string, string> cookie, IEnumerable<DimensionElement> result)
        {
            var school = cookie["ATTR_SCHOOL"];

            if (string.IsNullOrEmpty(school)) 
                return result;

            _logger.LogDebug($"*** User school: {school}");

            var splitSchools = school.Split(',').Select(c => new DimensionElement { Name = c });

            _logger.LogInformation($"*** school before count {result.Count()}");

            var subresult = new List<DimensionElement>();
            foreach (var parent in splitSchools)
            {
                var extresult = result.FirstOrDefault(r => r.Name.Equals(parent.Name));
                if (extresult != null)
                    subresult.Add(extresult);
            }

            result = subresult;
            _logger.LogInformation($"*** school after count {result.Count()}");

            return result;
        }

        private IEnumerable<DimensionElement> FilterClass(Dictionary<string, string> cookie, IEnumerable<DimensionElement> result)
        {
            var roles = cookie["roles"];
            var sub = cookie["sub"];

            if (!roles.Contains("teacher"))
                return result;

            var query = "select distinct g.class_unit_id from group_teacher_assignments gta inner join groups g on g.id = gta.group_id "+
            "where gta.teacher_id in (select profiles from report.mv_teacher_rsaa mtr where mtr.rsaa_id = @rsaa)";

            var data = _context.TeacherClasses.FromSqlRaw(query, new NpgsqlParameter("@rsaa", sub));

            if (!data.Any())
                return result;

            var subresult = new List<DimensionElement>();
            foreach (var parent in data)
            {
                var extresult = result.FirstOrDefault(r => r.Name.Equals(parent.ClassUnitId.ToString()));
                if (extresult != null)
                    subresult.Add(extresult);
            }

            return subresult;
        }

        private IEnumerable<DimensionElement> FilterTeacher(Dictionary<string, string> cookie, IEnumerable<DimensionElement> result)
        {
            var roles = cookie["roles"];
            var sub = cookie["sub"];

            if (!roles.Contains("teacher"))
                return result;

            var query = "select profiles from report.mv_teacher_rsaa mtr where mtr.rsaa_id = @rsaa";

            var data = _context.TeacherRsaa.FromSqlRaw(query, new NpgsqlParameter("@rsaa", sub));

            if (!data.Any())
                return result;

            var subresult = new List<DimensionElement>();
            foreach (var parent in data)
            {
                var extresult = result.FirstOrDefault(r => r.Name.Equals(parent.Profiles.ToString()));
                if (extresult != null)
                    subresult.Add(extresult);
            }

            return !subresult.Any() ? result : subresult;
        }

        private async Task<DimensionElement[]> InternalGetElements(DimensionElementRequest request, DatasourceMetadataDimension dimension)
        {
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = dimension.Query.Sql;

            if (dimension.Query.Parameters != null)
                foreach (var dimensionParameter in dimension.Query.Parameters)
                {
                    var queryDimension = request.Selections.FirstOrDefault(d => d.Dimension == dimensionParameter.Source);
                    command.Parameters.Add(BuildParameter(dimensionParameter, queryDimension?.Selection.Values));
                }

            await _context.Database.GetDbConnection().OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            var result = new List<DimensionElement>();
            while (await reader.ReadAsync())
            {
                result.Add(new DimensionElement
                {
                    Name = reader.GetString(0),
                    Caption = reader.GetString(1)
                });
            }

            await _context.Database.GetDbConnection().CloseAsync();

            return result.ToArray();
        }

        public async Task<ReportQueryResult> ExecuteQuery(ReportQuery query, DatasourceMetadata metadata)
        {
            var dataRows = await InternalExecuteQuery(query, metadata);

            var result = new ReportQueryResult
            {
                Cells = new[]
                {
                    metadata.Table.Select(t => new ReportQueryResultCell
                    {
                        Type = ReportCellType.ColumnHeader,
                        Value = t.Caption,
                        Name = t.Name
                    }).ToArray()
                }.Concat(dataRows)
                    .ToArray()
            };

            return result;
        }

        public async Task CalculateTotals(ReportQueryResult result, DatasourceMetadata metadata)
        {
            await Task.Run(() =>
            {
                var aggregateColumns = metadata.Table.Select((t, i) => new {t, i}).Where(c => c.t.Aggregate != DatasourceAggregateType.None);
                if (!aggregateColumns.Any())
                    return;

                var row = Enumerable.Range(0, metadata.Table.Length)
                    .Select(r => new ReportQueryResultCell
                    {
                        Type = ReportCellType.TotalCell
                    }).ToArray();

                foreach (var column in aggregateColumns)
                {
                    var values = result.Cells.Select(c => c[column.i])
                        .Where(c => c != null && c.Type == ReportCellType.DataCell)
                        .Select(c => Convert.ToDouble(c.Value));

                    if (!values.Any())
                        continue;

                    if (column.t.AggregateNonZero)
                        values = values.Where(c => c > 0);

                    if (!values.Any())
                        continue;

                    double? value = column.t.Aggregate switch
                    {
                        DatasourceAggregateType.Sum => values.Sum(),
                        DatasourceAggregateType.Avg => values.Average(),
                        DatasourceAggregateType.Min => values.Min(),
                        DatasourceAggregateType.Max => values.Max(),
                        _ => default(double?)
                    };
                    if (value != null)
                        value = Math.Round((double) value, 2);

                    row[column.i].Value = value;
                }

                result.Cells = result.Cells.Concat(new[] {row}).ToArray();
            });
        }

        private async Task<IEnumerable<ReportQueryResultCell[]>> InternalExecuteQuery(ReportQuery query, DatasourceMetadata metadata)
        {
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = metadata.Query.Sql;

            foreach (var dimension in metadata.Query.Parameters)
            {
                var queryDimension = query.Dimensions.SelectMany(d => d.Value).FirstOrDefault(d => d.Dimension == dimension.Source);
                command.Parameters.Add(BuildParameter(dimension, queryDimension?.Selection.Values));
            }

            await _context.Database.GetDbConnection().OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            var result = new List<ReportQueryResultCell[]>();
            while (await reader.ReadAsync())
            {
                var row = new ReportQueryResultCell[metadata.Table.Length];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var fieldName = reader.GetName(i);
                    var columnIndex = Array.FindIndex(metadata.Table, t => t.Name == fieldName);
                    if (columnIndex == -1)
                        continue;

                    var value = await reader.IsDBNullAsync(i) ? null : reader[i];
                    if (value is bool boolValue)
                    {
                        value = boolValue ? "Да" : "Нет";
                    }

                    row[columnIndex] = new ReportQueryResultCell
                    {
                        Value = value
                    };
                }
                result.Add(row);
            }

            await _context.Database.GetDbConnection().CloseAsync();

            return result;
        }

        private NpgsqlParameter BuildParameter(DatasourceMetadataQueryParameter parameter, DimensionElement[] elements)
        {
            if (elements == null || !elements.Any())
                return new NpgsqlParameter(parameter.Name, DBNull.Value);

            var value = parameter.DataType switch
            {
                "Int32" => parameter.IsArray ? (object) elements.Select(s => ConvertValue<int>(Extractid(s.Name))).ToArray() : ConvertValue<int>(Extractid(elements.First().Name)),
                "DateTime" => parameter.IsArray ? (object) elements.Select(s => ConvertValue<DateTime>(Extractid(s.Name))).ToArray() : ConvertValue<DateTime>(Extractid(elements.First().Name)),
                _ => null
            };

            return new NpgsqlParameter(parameter.Name, DBNull.Value)
            {
                Value = value
            };
        }

        private string Extractid(string value)
        {
            if (value[0] == '[')
            {
                return value.Substring(1, value.IndexOf(']')-1);
            }

            return value;
        }

        private T ConvertValue<T>(string value)
        {
            return (T) Convert.ChangeType(value, typeof(T));
        }
    }

    public class DimensionComparer : IEqualityComparer<DimensionElement>
    {
        public bool Equals(DimensionElement x, DimensionElement y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(DimensionElement obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
