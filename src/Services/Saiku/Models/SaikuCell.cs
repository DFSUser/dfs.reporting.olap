﻿/*
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

using System.Text.Json.Serialization;
using Dfs.Reporting.Olap.Models;

namespace Dfs.Reporting.Olap.Services.Saiku.Models
{
    public class SaikuCell
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
        [JsonPropertyName("type")]
        [JsonConverter(typeof(SaikuCellTypeConverter))]
        public ReportCellType Type { get; set; }
        [JsonPropertyName("properties")]
        public SaikuCellProperties Properties { get; set; }
    }
}