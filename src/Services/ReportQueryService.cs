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

using System.Threading.Tasks;
using Dfs.Reporting.Olap.Models;
using Dfs.Reporting.Olap.Services.Database;
using Dfs.Reporting.Olap.Services.Saiku;

namespace Dfs.Reporting.Olap.Services
{
    public interface IReportQueryService
    {
        Task<ReportQueryResult> Query(ReportQuery query, bool offFormatting = false);
    }

    public class ReportQueryService : IReportQueryService
    {
        private readonly IReportMetadataService _metadataService;
        private readonly IReportDatabaseService _databaseService;
        private readonly IReportSaikuService _saikuService;
        private readonly IReportStyleService _styleService;

        public ReportQueryService(IReportMetadataService metadataService, IReportDatabaseService databaseService, IReportSaikuService saikuService, IReportStyleService styleService)
        {
            _metadataService = metadataService;
            _databaseService = databaseService;
            _saikuService = saikuService;
            _styleService = styleService;
        }

        public async Task<ReportQueryResult> Query(ReportQuery query, bool offFormatting = false)
        {
            var metadata = await _metadataService.GetMetadata(new ReportMetadataRequest
            {
                Name = query.Name
            });

            var cells = metadata.SourceType switch
            {
                DatasourceMetadataSourceType.Database => await _databaseService.ExecuteQuery(query, metadata),
                DatasourceMetadataSourceType.Saiku => await _saikuService.ExecuteQuery(query, metadata),
                _ => null
            };

            switch (metadata.SourceType)
            {
                case DatasourceMetadataSourceType.Database:
                    await _databaseService.CalculateTotals(cells, metadata);
                    break;
                case DatasourceMetadataSourceType.Saiku:
                    await _saikuService.CalculateTotals(cells, metadata);
                    break;
            }

            _styleService.SetStyle(query, cells, metadata, offFormatting);

            return cells;
        }
    }
}
