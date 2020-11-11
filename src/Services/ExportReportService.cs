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

using System.Linq;
using System.Threading.Tasks;
using Dfs.Reporting.Olap.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Dfs.Reporting.Olap.Services
{
    public interface IExportReportService
    {
        Task<(byte[] buffer, string fileName)> Export(ReportQuery query);
    }

    public class ExportReportService: IExportReportService
    {
        private readonly IReportQueryService _queryService;

        public ExportReportService(IReportQueryService queryService)
        {
            _queryService = queryService;
        }

        public async Task<(byte[] buffer, string fileName)> Export(ReportQuery query)
        {
            var data = await _queryService.Query(query, true);

            return (await GenerateReport(data), $"{data.BaseCaption}.xlsx");
        }

        private async Task<byte[]> GenerateReport(ReportQueryResult queryResult)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var excelPackage = new ExcelPackage();

            var wb = excelPackage.Workbook;
            CreateHeaderStyle(wb);
            CreateCellStyles(wb);

            var sheets = wb.Worksheets;
            var sheet = sheets.Add("Отчет");

            var headerCell = sheet.Cells[1, 1];
            headerCell.Value = queryResult.Caption;

            const int firstRow = 3;

            foreach (var rows in queryResult.Cells.Select((row, r) => new { row, r }))
            {
                foreach (var cell in rows.row.Select((cell, c) => new { cell, c }))
                {
                    if (cell.cell.Type == ReportCellType.ColumnHeader)
                    {
                        sheet.Column(cell.c + 1).Width = PixelWidthToExcel(cell.cell.Width ?? 150);
                    }

                    var cells = sheet.Cells[rows.r + firstRow, cell.c + 1];

                    cells.StyleName = cell.cell.Type == ReportCellType.ColumnHeader ? "epos-header" : "epos-cell";
                    cells.Value = cell.cell.Value;
                }
            }

            return await excelPackage.GetAsByteArrayAsync();
        }
        private double PixelWidthToExcel(int pixels)
        {
            var tempWidth = pixels * 0.14099;
            var correction = (tempWidth / 100) * -1.30;

            return tempWidth - correction;
        }

        private void CreateHeaderStyle(ExcelWorkbook workbook)
        {
            var style = workbook.Styles.CreateNamedStyle("epos-header").Style;
            style.Border.Right.Style = ExcelBorderStyle.Thin;
            style.Border.Left.Style = ExcelBorderStyle.Thin;
            style.Border.Top.Style = ExcelBorderStyle.Thin;
            style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            style.VerticalAlignment = ExcelVerticalAlignment.Center;
            style.WrapText = true;
        }

        private void CreateCellStyles(ExcelWorkbook workbook)
        {
            var style = workbook.Styles.CreateNamedStyle("epos-cell").Style;
            style.Border.Right.Style = ExcelBorderStyle.Thin;
            style.Border.Left.Style = ExcelBorderStyle.Thin;
            style.Border.Top.Style = ExcelBorderStyle.Thin;
            style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            style.Numberformat.Format = "0.00";

            style.WrapText = true;
        }
    }
}
