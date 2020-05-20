﻿using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bit.Core;
using Bit.Core.Exceptions;
using Bit.Core.Models.Api.Public;
using Bit.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.Public.Controllers
{
    [Route("public/organization")]
    [Authorize("Organization")]
    public class OrganizationController : Controller
    {
        private readonly IOrganizationService _organizationService;
        private readonly CurrentContext _currentContext;
        private readonly GlobalSettings _globalSettings;

        public OrganizationController(
            IOrganizationService organizationService,
            CurrentContext currentContext,
            GlobalSettings globalSettings)
        {
            _organizationService = organizationService;
            _currentContext = currentContext;
            _globalSettings = globalSettings;
        }

        /// <summary>
        /// Import members and groups.
        /// </summary>
        /// <remarks>
        /// Import members and groups from an external system.
        /// </remarks>
        /// <param name="model">The request model.</param>
        [HttpPost("import")]
        [ProducesResponseType(typeof(MemberResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponseModel), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Import([FromBody]OrganizationImportRequestModel model)
        {
            if (!_globalSettings.SelfHosted &&
                (model.Groups.Count() > 2000 || model.Members.Count(u => !u.Deleted) > 2000))
            {
                throw new BadRequestException("You cannot import this much data at once.");
            }

            await _organizationService.ImportAsync(
                _currentContext.OrganizationId.Value,
                null,
                model.Groups.Select(g => g.ToImportedGroup(_currentContext.OrganizationId.Value)),
                model.Members.Where(u => !u.Deleted).Select(u => u.ToImportedOrganizationUser()),
                model.Members.Where(u => u.Deleted).Select(u => u.ExternalId),
                model.OverwriteExisting.GetValueOrDefault());
            return new OkResult();
        }
    }
}
