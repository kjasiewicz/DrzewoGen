using System.Web.Optimization;

namespace Db4o_lab2
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundleTable)
        {
            bundleTable.Add(new ScriptBundle("~/bundles/scripts").
                Include("~/Scripts/jquery-2.1.3.js",
                "~/Scripts/jquery.validate.js",
                "~/Scripts/jquery.validate.unobtrusive.js",
                "~/Scripts/bootstrap.js",
                "~/Scripts/bootstrap-datepicker.js",
                "~/Scripts/site.js"));
            BundleTable.EnableOptimizations = true;
        }
    }
}