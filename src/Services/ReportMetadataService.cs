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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Dfs.Reporting.Olap.Models;
using Microsoft.EntityFrameworkCore;

namespace Dfs.Reporting.Olap.Services
{
    public interface IReportMetadataService
    {
        Task<DatasourceMetadata> GetMetadata(ReportMetadataRequest metadata);
        Task<ReportMetadataResponse> GetReportMetadata(ReportMetadataRequest metadataRequest);
        Task Push(DatasourceMetadata metadata);
        Task<DatasourceMetadata> Pull(ReportMetadataRequest metadata);
        Task<T> GetMetaObject<T>(string code);
    }
    public class ReportMetadataService : IReportMetadataService
    {
        private readonly MetadataDatabaseContext _context;
        private readonly IReportStyleService _styleService;

        public ReportMetadataService(MetadataDatabaseContext context, IReportStyleService styleService)
        {
            _context = context;
            _styleService = styleService;
        }

        public async Task<DatasourceMetadata> GetMetadata(ReportMetadataRequest metadata)
        {
            var meta = await GetMetaObject<DatasourceMetadata>(metadata.Name);

            if (meta == null)
                throw new Exception($"Отчетная форма {metadata.Name} не найдена");

            foreach (var dimension in meta.Dimensions.SelectMany(d => d.Value).Where(d => !string.IsNullOrEmpty(d.RefObject)))
            {
                var refObject = await GetMetaObject<DatasourceMetadataDimension>(dimension.RefObject);
                CloneDimension(refObject, dimension);
            }

            return meta;
        }

        private void CloneDimension(DatasourceMetadataDimension source, DatasourceMetadataDimension target)
        {
            target.RefObject = null;
            target.Type = source.Type;
            target.Caption = source.Caption;
            target.Query = source.Query;
            target.Dimension = source.Dimension;
            target.IsStatic = source.IsStatic;
            target.StaticValues = source.StaticValues;
            target.Level = source.Level;
            target.Name = source.Name;
            target.Required = source.Required;
            target.SelectionMode = source.SelectionMode;
        }

        public async Task<ReportMetadataResponse> GetReportMetadata(ReportMetadataRequest metadataRequest)
        {
            var meta = await GetMetadata(metadataRequest);

            return new ReportMetadataResponse
            {
                Name = meta.Name,
                Caption = meta.Caption,
                Dimensions = meta.Dimensions
                    .ToDictionary(
                        d => d.Key,
                        d => d.Value.Select(m => new ReportMetadataDimension
                        {
                            Name = m.Name,
                            Caption = m.Caption,
                            Required = m.Required,
                            Type = m.Type,
                            Selection = m.SelectionMode,
                            Depends = m.Query?.Parameters?.Select(p => p.Source).ToArray(),
                            IsStatic = m.IsStatic,
                            StaticValues = m.StaticValues,
                            SearchMode = m.SearchMode,
                            Restricts = m.Restricts
                        }).ToArray()),
                Cells = meta.SourceType == DatasourceMetadataSourceType.Database ? ExampleTable(meta) : ExampleOlap(),
                Styles = meta.Styles
            };
        }

        public async Task Push(DatasourceMetadata metadata)
        {
            var meta = await _context.NavObjects
                .FirstOrDefaultAsync(i => i.Code == metadata.Name);

            if (meta == null)
                throw new Exception($"Отчетная форма {metadata.Name} не найдена");

            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                IgnoreNullValues = true,
                WriteIndented = true
            });

            meta.ObjectMeta = json;
            _context.NavObjects.Update(meta);

            await _context.SaveChangesAsync();
        }

        public async Task<DatasourceMetadata> Pull(ReportMetadataRequest metadata)
        {
            var meta = await GetMetaObject<DatasourceMetadata>(metadata.Name);

            if (meta == null)
                throw new Exception($"Отчетная форма {metadata.Name} не найдена");

            return meta;
        }

        public async Task<T> GetMetaObject<T>(string code)
        {
            var meta = await _context.NavObjects
                .FirstOrDefaultAsync(i => i.Code == code);

            if (meta == null)
                return default;

            var result = JsonSerializer.Deserialize<T>(meta.ObjectMeta);

            return result;
        }

        private ReportQueryResultCell[][] ExampleOlap()
        {
            return new[]
            {
                new[]
                {
                    new ReportQueryResultCell
                    {
                        Value = "нет данных",
                        Type = ReportCellType.DataCell,
                        Name = "[нет данных]"
                    }
                }
            };
        }

        private ReportQueryResultCell[][] ExampleTable(DatasourceMetadata metadata)
        {
            if (metadata.Table == null)
                return null;

            var dataRows = Enumerable.Range(0, metadata.Table.Length)
                .Select(c => new ReportQueryResultCell
                {
                    Type = ReportCellType.DataCell,
                    ColSpan = -1
                }).ToArray();
            dataRows[0].Value = "нет данных";
            dataRows[0].ColSpan = dataRows.Length;
            dataRows[0].Style = new Dictionary<string, string>
            {
                {"textAlign", "left"}
            };

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
                    }.Concat(new[] {dataRows})
                    .ToArray()
            };

            _styleService.SetStyle(null, result, metadata);

            dataRows[0].Style["textAlign"] = "left";

            return result.Cells;
        }
    }
}
