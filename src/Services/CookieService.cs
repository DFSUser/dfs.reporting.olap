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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Dfs.Reporting.Olap.Services
{
    public interface ICookieService
    {
        Dictionary<string, string> ReadClaims();
    }
    public class CookieService : ICookieService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public CookieService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public Dictionary<string, string> ReadClaims()
        {
            var context = _contextAccessor.HttpContext;

            if (!context.Request.Cookies.ContainsKey("AuthID"))
                return null;

            var cookie = context.Request.Cookies["AuthID"];

            var handler = new JwtSecurityTokenHandler();
            //var jsonToken = handler.ReadToken(cookie);
            var tokenS = handler.ReadToken(cookie) as JwtSecurityToken;

            return tokenS?.Claims.ToDictionary(c => c.Type, c => c.Value);
        }
    }
}
