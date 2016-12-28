using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace PnPTemplateManager.Licensing
{
    public class ValidWizdomLicense : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var queryParameters = HttpUtility.ParseQueryString(actionContext.Request.RequestUri.Query);
            var company = queryParameters["company"] as string;
            var md5Hash = queryParameters["key"] as string;
            return true;
            /*return !string.IsNullOrEmpty(company) &&
                !string.IsNullOrEmpty(md5Hash) &&
                LicenseValidator.ValidateLicense(company, md5Hash);*/
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.OK, new { Error = "License validation failed" });
        }
    }
}
