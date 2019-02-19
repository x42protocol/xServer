using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace x42.Feature.API.Requirements
{
    public class PrivateOnlyRequirement : AuthorizationHandler<PrivateOnlyRequirement>, IAuthorizationRequirement
    {
        public List<string> PrivateAddressList { get; private set; }

        public PrivateOnlyRequirement(List<string> privateAddressList)
        {
            PrivateAddressList = privateAddressList;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PrivateOnlyRequirement requirement)
        {
            if (context.Resource is AuthorizationFilterContext mvcContext)
            {
                string remoteIpList = mvcContext.HttpContext.Connection.RemoteIpAddress.ToString();

                if (PrivateAddressList.Contains(remoteIpList))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    // There isn't currently a built-in call to result in a status code for .Fail() so we will do it manually.
                    mvcContext.Result = new JsonResult("Not Authorized") { StatusCode = 403 };
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}