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
using System.Threading.Tasks;
using Dfs.Reporting.Olap.Models;
using Dfs.Reporting.Olap.Services;
using Dfs.Reporting.Olap.Services.Saiku;
using Microsoft.AspNetCore.Mvc;

namespace Dfs.Reporting.Olap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OlapController : ControllerBase
    {
        private readonly IReportMetadataService _metadataService;
        private readonly IReportDimensionService _dimensionService;
        private readonly IReportQueryService _queryService;
        private readonly IExportReportService _exportReportService;
        private readonly ISaikuHttpClient _saikuHttpClient;

        public OlapController(IReportMetadataService metadataService, IReportDimensionService dimensionService, IReportQueryService queryService, IExportReportService exportReportService, ISaikuHttpClient saikuHttpClient)
        {
            _metadataService = metadataService;
            _dimensionService = dimensionService;
            _queryService = queryService;
            _exportReportService = exportReportService;
            _saikuHttpClient = saikuHttpClient;
        }

        [HttpPost]
        public async Task<ActionResult<ReportMetadataResponse>> GetMetadata([FromBody] ReportMetadataRequest meta)
        {
            try
            {
                var metadata = await _metadataService.GetReportMetadata(meta);

                return Ok(metadata);
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    e.Message,
                    e.StackTrace
                });
            }
        }

        [HttpPost("dimension")]
        public async Task<ActionResult<IEnumerable<DimensionElement>>> GetDimensionElements([FromBody] DimensionElementRequest request)
        {
            try
            {
                _saikuHttpClient.SetHttpContext(HttpContext);

                return Ok(await _dimensionService.GetElements(request));
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    e.Message,
                    e.StackTrace
                });
            }
        }

        [HttpPost("execute")]
        public async Task<ActionResult> ExecuteQuery([FromBody] ReportQuery query)
        {
            try
            {
                _saikuHttpClient.SetHttpContext(HttpContext);

                return Ok(await _queryService.Query(query));
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    e.Message,
                    e.StackTrace
                });
            }
        }

        [HttpPost("export")]
        public async Task<ActionResult> ExportQuery([FromBody] ReportQuery query)
        {
            try
            {
                _saikuHttpClient.SetHttpContext(HttpContext);

                var (buffer, fileName) = await _exportReportService.Export(query);

                return File(buffer, "application/octet-stream", fileName);
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    e.Message,
                    e.StackTrace
                });
            }
        }

        [HttpPost("push")]
        public async Task<ActionResult> PushReport([FromBody] DatasourceMetadata metadata)
        {
            try
            {
                await _metadataService.Push(metadata);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    e.Message,
                    e.StackTrace
                });
            }
        }

        [HttpPost("pull")]
        public async Task<ActionResult> PullReport([FromBody] ReportMetadataRequest metadata)
        {
            try
            {
                return Ok(await _metadataService.Pull(metadata));
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    e.Message,
                    e.StackTrace
                });
            }
        }
    }
}
