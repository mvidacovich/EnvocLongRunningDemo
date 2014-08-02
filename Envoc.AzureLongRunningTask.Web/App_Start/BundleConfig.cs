using System.Web;
using System.Web.Optimization;

namespace Envoc.AzureLongRunningTask.Web
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/headscripts").Include(
                      "~/Scripts/modernizr-*",
                      "~/Scripts/holder.js"));

            var bottom = new ScriptBundle("~/bundles/bodyscripts") ;
            bottom.Include(
                "~/Scripts/angular.js",
                "~/Scripts/jquery-{version}.js",
                "~/Scripts/bootstrap.js",
                "~/Scripts/respond.js",
                "~/Scripts/moxie.js",
                "~/Scripts/underscore.js",
                "~/Scripts/plupload.dev.js",
                "~/Scripts/jquery.signalR-2.1.0.js");
            bottom.IncludeDirectory("~/Scripts/controllers/", "*.js", false);
            bundles.Add(bottom);

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));

            // Set EnableOptimizations to false for debugging. For more information,
            // visit http://go.microsoft.com/fwlink/?LinkId=301862
            BundleTable.EnableOptimizations = false;
        }
    }
}
