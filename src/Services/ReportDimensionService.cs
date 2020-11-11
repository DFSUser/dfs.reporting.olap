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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfs.Reporting.Olap.Models;
using Dfs.Reporting.Olap.Services.Database;
using Dfs.Reporting.Olap.Services.Saiku;

namespace Dfs.Reporting.Olap.Services
{
    public interface IReportDimensionService
    {
        Task<IEnumerable<DimensionElement>> GetElements(DimensionElementRequest request);
    }

    public class ReportDimensionService : IReportDimensionService
    {
        private readonly IReportMetadataService _metadataService;
        private readonly IReportDatabaseService _databaseService;
        private readonly IReportSaikuService _saikuService;

        public ReportDimensionService(IReportMetadataService metadataService, IReportDatabaseService databaseService, IReportSaikuService saikuService)
        {
            _metadataService = metadataService;
            _databaseService = databaseService;
            _saikuService = saikuService;
        }

        public async Task<IEnumerable<DimensionElement>> GetElements(DimensionElementRequest request)
        {
            var metadata = await _metadataService.GetMetadata(new ReportMetadataRequest
            {
                Name = request.Name
            });

            var dimension = metadata.Dimensions.SelectMany(d=>d.Value).FirstOrDefault(d => d.Name == request.Dimension);

            if (dimension == null)
                throw new Exception($"Измерение {request.Dimension} в отчетной форме {request.Name} не найдено");

            return dimension.SourceType switch
            {
                DatasourceMetadataSourceType.Saiku => await _saikuService.GetElements(request, metadata),
                _ => await _databaseService.GetElements(request, dimension)
            };
        }
    }
}
