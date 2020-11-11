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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Dfs.Reporting.Olap.Models;
using Dfs.Reporting.Olap.Services.Saiku.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Dfs.Reporting.Olap.Services.Saiku
{
    public interface ISaikuHttpClient
    {
        //Task<SaikuMetadataResponse> GetMetadata(SaikuMetadataRequest request);
        Task<DimensionElement[]> GetDimensionElements(DatasourceMetadata metadata, string cube, string hierarchy, string level, string search);
        Task<SaikuQueryResult> ExecuteQuery(SaikuQuery saikuQuery);
        void SetHttpContext(HttpContext httpContext);
        Task SetSession(SaikuSetSession session);
    }

    public class SaikuHttpClient: ISaikuHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;
        private HttpContext _httpContext;

        public SaikuHttpClient(IConfiguration configuration)
        {
            _cookieContainer = new CookieContainer();

            var httpClientHandler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                UseDefaultCredentials = false
            };
            _httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri(configuration["Saiku:Path"])
            };

            var byteArray = Encoding.ASCII.GetBytes($"{configuration["Saiku:Login"]}:{configuration["Saiku:Password"]}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        public async Task<SaikuQueryResult> ExecuteQuery(SaikuQuery saikuQuery)
        {
            var requestAccepts = _httpClient.DefaultRequestHeaders.Accept;
            requestAccepts.Add(new MediaTypeWithQualityHeaderValue("application/json", 0.01));
            requestAccepts.Add(new MediaTypeWithQualityHeaderValue("text/javascript", 0.01));
            requestAccepts.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));

            var json = JsonSerializer.Serialize(saikuQuery, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
            });
            var response = await _httpClient.PostAsync("query/execute", new StringContent(json, Encoding.UTF8, "application/json"));

            var responseStream = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<SaikuQueryResult>(responseStream);
        }

        public void SetHttpContext(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public async Task<DimensionElement[]> GetDimensionElements(DatasourceMetadata metadata, string cube, string hierarchy, string level, string search)
        {
            await SetSession(new SaikuSetSession
            {
                Name = $"{metadata.Name}_{metadata.Saiku.Connection}",
                Cube = metadata.Saiku
            });

            var searchQuery = string.IsNullOrEmpty(search)? null : $"&search={Uri.EscapeDataString(search)}";
            var query = $"query/{metadata.Name}_{metadata.Saiku.Connection}/result/metadata/hierarchies/{Uri.EscapeDataString(hierarchy)}/levels/{Uri.EscapeDataString(level)}?searchlimit=1000{searchQuery}";

            var response = await _httpClient.GetAsync(query);

            var responseStream = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();
            
            return JsonSerializer.Deserialize<DimensionElement[]>(responseStream);
        }

        #region Saiku session
        public async Task SetSession(SaikuSetSession session)
        {
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
            });

            var response = await _httpClient.PostAsync($"query/{session.Name}", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("json", json),
            }));

            response.EnsureSuccessStatusCode();
        }

        #endregion
    }
}
