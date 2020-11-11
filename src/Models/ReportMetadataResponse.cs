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
    public class ReportMetadataResponse
    {
        public string Name { get; set; }
        public string Caption { get; set; }
        public Dictionary<string, ReportMetadataDimension[]> Dimensions { get; set; }
        public ReportQueryResultCell[][] Cells { get; set; }
        [JsonPropertyName("styles")]
        public Dictionary<string, Dictionary<string, string>> Styles { get; set; }
    }

    public class ReportMetadataDimension
    {
        public string Name { get; set; }
        public string Caption { get; set; }
        public DimensionSelectionType Selection { get; set; }
        public DimensionControlType Type { get; set; }
        public bool Required { get; set; }
        public string[] Depends { get; set; }
        public bool IsStatic { get; set; } = false;
        public DimensionElement[] StaticValues { get; set; }
        public DimensionSearchMode SearchMode { get; set; }
        public DimensionMetadataRestrict Restricts { get; set; }
    }

    public class DimensionMetadataRestrict
    {
        public DimensionMetadataRestrictShift Shift { get; set; }
    }

    public class DimensionMetadataRestrictShift
    {
        public string Parent { get; set; }
        public int Days { get; set; }
    }

    public enum DimensionSelectionType
    {
        Single = 0,
        Multiple = 1
    }

    public enum DimensionControlType
    {
        ComboBox = 0,
        DatePicker = 1,
        TextBox = 2
    }

    public enum DimensionSearchMode
    {
        Client = 0,
        Server = 1
    }
}
