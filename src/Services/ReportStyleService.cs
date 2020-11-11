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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dfs.Reporting.Olap.Models;
using DotLiquid;
using ExcelNumberFormat;

namespace Dfs.Reporting.Olap.Services
{
    public interface IReportStyleService
    {
        void SetStyle(ReportQuery query, ReportQueryResult queryResult, DatasourceMetadata metadata, bool offFormatting = false);
    }

    public class ReportStyleService : IReportStyleService
    {
        public void SetStyle(ReportQuery query, ReportQueryResult queryResult, DatasourceMetadata metadata, bool offFormatting = false)
        {
            ParseReportCaption(query, queryResult, metadata);
            CalculateSpan(queryResult, metadata);
            SetCommonStyle(queryResult, metadata);
            SetTableStyle(queryResult, metadata, offFormatting);
            if (!offFormatting)
                SetNumberFormat(queryResult, metadata);
        }

        private void ParseReportCaption(ReportQuery query, ReportQueryResult queryResult, DatasourceMetadata metadata)
        {
            if (query == null)
                return;

            queryResult.BaseCaption = metadata.Caption;
            if (string.IsNullOrEmpty(metadata.CaptionPattern))
            {
                queryResult.Caption = metadata.Caption;
                return;
            }

            var captions = query.Dimensions.Values
                .SelectMany(s => s)
                .Where(s => s.Selection?.Values?.Length > 0)
                .ToDictionary(s => s.Dimension.Trim('[', ']'), s => (object) s.Selection?.Values?.First().Caption);

            var template = Template.Parse(metadata.CaptionPattern);
            var result = template.Render(Hash.FromDictionary(captions));

            queryResult.Caption = result;
        }

        private void SetCommonStyle(ReportQueryResult queryResult, DatasourceMetadata metadata)
        {
            if (metadata.Styles == null)
                return;

            queryResult.Styles = metadata.Styles;
        }

        private void SetNumberFormat(ReportQueryResult queryResult, DatasourceMetadata metadata)
        {
            if (metadata.Table == null)
                return;

            foreach (var row in queryResult.Cells.Where(c => c != null))
            {
                foreach (var cell in row.Select((c, i) => new {c, i}).Where(c => c.c?.Type == ReportCellType.ColumnHeader))
                {
                    var tableColumn = metadata.Table.FirstOrDefault(t => t.Name == cell.c.Name);
                    if (string.IsNullOrEmpty(tableColumn?.Format))
                        continue;

                    foreach (var cellrow in queryResult.Cells.Where(c => c != null))
                    {
                        var datacell = cellrow[cell.i];
                        if (datacell == null)
                            continue;

                        if (!double.TryParse(datacell.Value?.ToString(), out var value)) 
                            continue;

                        var fmt = new NumberFormat(tableColumn.Format);
                        datacell.Value = fmt.Format(value, CultureInfo.GetCultureInfo("ru-RU"));
                        if (datacell.Value is string cellValue)
                        {
                            datacell.Value = cellValue.Trim();
                        }
                    }
                }
            }
        }

        private void SetTableStyle(ReportQueryResult queryResult, DatasourceMetadata metadata, bool offFormatting = false)
        {
            if (metadata.Table == null)
                return;

            foreach (var row in queryResult.Cells.Where(c => c != null))
            {
                foreach (var cell in row.Select((c, i) => new {c, i}).Where(c => c.c?.Type == ReportCellType.ColumnHeader))
                {
                    var tableColumn = metadata.Table.FirstOrDefault(t => t.Name == cell.c.Name);
                    cell.c.Width = tableColumn?.Width ?? 150;

                    if (tableColumn == null)
                        continue;

                    var styles = tableColumn.Styles;

                    if (tableColumn.Styles != null)
                    {
                        if (styles.ContainsKey("header"))
                        {
                            cell.c.Style = SetStyle(cell.c.Style, styles["header"]);
                        }
                    }

                    foreach (var cellrow in queryResult.Cells.Where(c => c != null))
                    {
                        var datacell = cellrow[cell.i];
                        if (datacell == null)
                            continue;

                        if (tableColumn.Styles != null)
                        {
                            if (styles.ContainsKey("data") && datacell.Type == ReportCellType.DataCell)
                            {
                                datacell.Style = SetStyle(datacell.Style, styles["data"]);
                            }

                            if (styles.ContainsKey("total") && datacell.Type == ReportCellType.TotalCell)
                            {
                                datacell.Style = SetStyle(datacell.Style, styles["total"]);
                            }
                        }
                    }
                }
            }
        }

        private void CalculateSpan(ReportQueryResult result, DatasourceMetadata metadata)
        {
            if (metadata == null) return;
            if (result?.Cells?.Any() != true) return;

            // Определяем строку данных.
            var dataRow = 0;
            foreach (var row in result.Cells)
                if (row?.FirstOrDefault()?.Type == ReportCellType.ColumnHeader)
                    dataRow++;
                else
                    break;

            // Нет данных для схлапывания.
            if (dataRow >= result.Cells.Length) return;

            // Формируем коллецию индексов столбцов по которым произойдет объединение.
            var columns = new List<int>();
            for (var index = 0; index < metadata.Table.Length; index++)
                if (metadata.Table[index].CanRowSpan == true)
                    columns.Add(index);

            if (columns.Any())
                Parallel.ForEach(columns, (col) =>
                {
                    var item = result.Cells[dataRow][col];
                    var coincidencesCount = 1;

                    foreach (var row in result.Cells.Skip(dataRow + 1))
                    {
                        if (Equals(item.Value, row[col].Value))
                        {
                            coincidencesCount++;
                            row[col].RowSpan = -1;
                        }
                        else
                        {
                            // Не меняем деволтное значение, если нет повторений.
                            // 0 и 1 - нет повторений.
                            if (coincidencesCount > 1)
                                item.RowSpan = coincidencesCount;

                            item = row[col];
                            coincidencesCount = 1;
                        }
                    }
                });
        }

        private Dictionary<string, string> SetStyle(Dictionary<string, string> source, Dictionary<string, string> union)
        {
            if (source == null)
                return union;

            var result = new Dictionary<string, string>(source);

            foreach (var (key, value) in union)
            {
                if (result.ContainsKey(key))
                    result.Remove(key);

                result.Add(key, value);
            }

            return result;
        }
    }
}
