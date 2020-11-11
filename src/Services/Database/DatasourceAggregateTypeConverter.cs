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

namespace Dfs.Reporting.Olap.Services.Database
{
    public class DatasourceAggregateTypeConverter : JsonConverter<DatasourceAggregateType>
    {
        public override DatasourceAggregateType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                "none" => DatasourceAggregateType.None,
                "sum" => DatasourceAggregateType.Sum,
                "avg" => DatasourceAggregateType.Avg,
                "max" => DatasourceAggregateType.Max,
                "min" => DatasourceAggregateType.Min,
                _ => default
            };
        }

        public override void Write(Utf8JsonWriter writer, DatasourceAggregateType value, JsonSerializerOptions options)
        {
            var data = value switch
            {
                DatasourceAggregateType.None => "none",
                DatasourceAggregateType.Sum => "sum",
                DatasourceAggregateType.Avg => "avg",
                DatasourceAggregateType.Max => "max",
                DatasourceAggregateType.Min => "min",
                _ => ""
            };

            writer.WriteStringValue(data);
        }
    }
}
