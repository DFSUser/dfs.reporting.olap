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
using System.Text.Json;
using System.Text.Json.Serialization;
using Dfs.Reporting.Olap.Models;

namespace Dfs.Reporting.Olap.Services.Saiku
{
    public class SaikuCellTypeConverter : JsonConverter<ReportCellType>
    {
        public override ReportCellType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                "DATA_CELL" => ReportCellType.DataCell,
                "ROW_HEADER" => ReportCellType.RowHeader,
                "COLUMN_HEADER" => ReportCellType.ColumnHeader,
                "ROW_HEADER_HEADER" => ReportCellType.RowHeaderHeader,
                _ => default
            };
        }

        public override void Write(Utf8JsonWriter writer, ReportCellType value, JsonSerializerOptions options)
        {
            var data = value switch
            {
                ReportCellType.DataCell => "DATA_CELL",
                ReportCellType.RowHeader => "ROW_HEADER",
                ReportCellType.ColumnHeader => "COLUMN_HEADER",
                ReportCellType.RowHeaderHeader => "ROW_HEADER_HEADER",
                _ => ""
            };

            writer.WriteStringValue(data);
        }
    }
}