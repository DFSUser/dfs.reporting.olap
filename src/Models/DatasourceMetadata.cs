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

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Dfs.Reporting.Olap.Services.Database;
using Dfs.Reporting.Olap.Services.Saiku.Models;

namespace Dfs.Reporting.Olap.Models
{
    public class DatasourceMetadata
    {
        [JsonPropertyName("caption")]
        public string Caption { get; set; }
        [JsonPropertyName("captionPattern")]
        public string CaptionPattern { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("sourceType")]
        public DatasourceMetadataSourceType SourceType { get; set; }
        [JsonPropertyName("query")]
        public DatasourceMetadataQuery Query { get; set; }
        [JsonPropertyName("dimensions")]
        public Dictionary<string, DatasourceMetadataDimension[]> Dimensions { get; set; }
        [JsonPropertyName("table")]
        public DatasourceMetadataTableColumn[] Table { get; set; }
        [JsonPropertyName("saiku")]
        public SaikuCube Saiku { get; set; }
        [JsonPropertyName("styles")]
        public Dictionary<string, Dictionary<string, string>> Styles { get; set; }
    }

    public enum DatasourceMetadataSourceType
    {
        Database = 1,
        Saiku = 2
    }

    public class DatasourceMetadataQuery
    {
        [JsonPropertyName("sql")]
        public string Sql { get; set; }
        [JsonPropertyName("keys")]
        public string[] Keys { get; set; }
        [JsonPropertyName("parameters")]
        public DatasourceMetadataQueryParameter[] Parameters { get; set; }
    }

    public class DatasourceMetadataQueryParameter
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("isArray")]
        public bool IsArray { get; set; }
        [JsonPropertyName("dataType")]
        public string DataType { get; set; }
        [JsonPropertyName("source")]
        public string Source { get; set; }
    }

    public class DatasourceMetadataDimension
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("caption")]
        public string Caption { get; set; }
        [JsonPropertyName("level")]
        public string Level { get; set; }
        [JsonPropertyName("dimension")]
        public string Dimension { get; set; }
        [JsonPropertyName("type")]
        public DimensionControlType Type { get; set; }
        [JsonPropertyName("required")]
        public bool Required { get; set; }
        [JsonPropertyName("selectionMode")]
        public DimensionSelectionType SelectionMode { get; set; }
        [JsonPropertyName("query")]
        public DatasourceMetadataQuery Query { get; set; }
        [JsonPropertyName("isStatic")]
        public bool IsStatic { get; set; } = false;
        [JsonPropertyName("staticValues")]
        public DimensionElement[] StaticValues { get; set; }
        [JsonPropertyName("ref")]
        public string RefObject { get; set; }
        [JsonPropertyName("aggregators")]
        public DatasourceAggregateType[] Aggregators { get; set; }
        [JsonPropertyName("searchMode")]
        public DimensionSearchMode SearchMode { get; set; }
        [JsonPropertyName("sourceType")]
        public DatasourceMetadataSourceType SourceType { get; set; }
        [JsonPropertyName("restricts")]
        public DimensionMetadataRestrict Restricts { get; set; }
    }

    public class DatasourceMetadataTableColumn
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("caption")]
        public string Caption { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("styles")]
        public Dictionary<string, Dictionary<string, string>> Styles { get; set; }

        [JsonPropertyName("aggregate")]
        [JsonConverter(typeof(DatasourceAggregateTypeConverter))]
        public DatasourceAggregateType Aggregate { get; set; }
        [JsonPropertyName("aggregateNonZero")]
        public bool AggregateNonZero { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        /// <summary>
        /// Признак возможности объединения одинаковых ячеек.
        /// </summary>
        [JsonPropertyName("canRowSpan")]
        public bool? CanRowSpan { get; set; }
    }

    public enum DatasourceAggregateType
    {
        None,
        Sum,
        Avg,
        Min,
        Max
    }
}
