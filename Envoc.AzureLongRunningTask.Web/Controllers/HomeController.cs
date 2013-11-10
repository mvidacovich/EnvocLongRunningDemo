using System;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace Envoc.AzureLongRunningTask.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public string UploadFile(HttpPostedFileBase file, string id, string name, int? chunk, int? chunks)
        {
            if (chunks.HasValue && chunks > 4096)
            {
                throw new NotSupportedException("The file is too large.");
            }

            Thread.Sleep(100);

            return "ok";
        }
    }
}
