using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Envoc.AzureLongRunningTask.Web.Controllers
{
    // Note: this is not secure :)
    public class AccountController : Controller
    {
        [HttpPost]
        public ActionResult LogOff()
        {
            Response.SetCookie(new HttpCookie(FormsAuthentication.FormsCookieName, ""));
            return RedirectToAction("Index","Home");
        }
        [HttpPost]
        public ActionResult Login(string username)
        {
            FormsAuthentication.SetAuthCookie(username,true);
            return RedirectToAction("Index", "Home");
        }
    }
}