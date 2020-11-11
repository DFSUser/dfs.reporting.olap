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

namespace Dfs.Reporting.Olap.Models
{
    public class ReportQueryResultCell
    {
        [JsonPropertyName("value")]
        public object Value { get; set; }
        [JsonPropertyName("type")]
        public ReportCellType Type { get; set; }
        [JsonPropertyName("style")]
        public Dictionary<string,string> Style { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("rowSpan")]
        public int RowSpan { get; set; }
        [JsonPropertyName("colSpan")]
        public int ColSpan { get; set; }
        [JsonPropertyName("width")]
        public int? Width { get; set; }
        [JsonPropertyName("isHTML")]
        public bool IsHtml { get; set; }
    }
}