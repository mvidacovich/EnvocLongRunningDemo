using System;
using System.Web.Mvc;

namespace Envoc.AzureLongRunningTask.Web.Controllers
{
    public class HomeController : Controller
    {
        [Authorize]
        public ActionResult Test()
        {
            NotificationHub.SendNotification(User.Identity.Name, new ClientNotification
            {
                Type = "foo",
                Content = "bar " + Guid.NewGuid()
            });
            return new EmptyResult();
        }

        public ActionResult Index()
        {
            return View();
        }
    }
}