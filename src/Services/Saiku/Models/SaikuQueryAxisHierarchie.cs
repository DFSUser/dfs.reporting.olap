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

namespace Dfs.Reporting.Olap.Services.Saiku.Models
{
    public class SaikuQueryAxisHierarchie
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("caption")]
        public string Caption { get; set; }
        [JsonPropertyName("dimension")]
        public string Dimension { get; set; }
        [JsonPropertyName("levels")]
        public Dictionary<string, SaikuQueryAxisHierarchiesLevel> Levels { get; set; }
    }
}