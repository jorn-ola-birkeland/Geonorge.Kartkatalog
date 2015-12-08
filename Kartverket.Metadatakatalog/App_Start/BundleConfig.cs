﻿using System.Configuration;
using System.Web;
using System.Web.Optimization;

namespace Kartverket.Metadatakatalog
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {

            bundles.Add(new ScriptBundle("~/bundles/shared-menu-scripts").Include(
            "~/KartverketSharedMenu/Scripts/angular.js",
            "~/KartverketSharedMenu/Scripts/jquery-1.11.3.min.js",
            "~/KartverketSharedMenu/Scripts/geonorge/app.js",
            "~/KartverketSharedMenu/Scripts/geonorge-top/searchOptions/" + ConfigurationManager.AppSettings["SearchOptionsFile"],
            "~/KartverketSharedMenu/Scripts/geonorge-top/searchTopController.js",
            "~/KartverketSharedMenu/Scripts/ui-bootstrap-0.14.3.js",
            "~/KartverketSharedMenu/Scripts/geonorge-common/baseUrl.js",
            "~/KartverketSharedMenu/Scripts/geonorge-common/common.js",
            "~/KartverketSharedMenu/Scripts/geonorge-top/menuTopController.js"
            ));


            bundles.Add(new StyleBundle("~/bundles/shared-menu-styles")
           .Include("~/KartverketSharedMenu/Styles/bootstrap.css")
           .Include("~/KartverketSharedMenu/Styles/geonorge-top/menuTop.css")
           .Include("~/KartverketSharedMenu/Styles/geonorge-top/logoTop.css")
           .Include("~/KartverketSharedMenu/Styles/geonorge-top/searchTop.css")
           );


            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/site.css",
                      "~/Content/themes/base/core.css",
                      "~/Content/themes/base/datepicker.css",
                      "~/Content/themes/base/theme.css",
                      "~/Content/custom.css",
                      "~/Content/geonorge-colors.css",
                      "~/Content/searchresult-layout.css",
                      "~/Content/custom-icons.css",
                      "~/Content/font-awesome.min.css",
                      "~/Content/forms.css",
                      "~/Content/chosen.css",
                      "~/Content/download.css",
                      "~/Content/map-modal.css"
                      ));


            bundles.Add(new ScriptBundle("~/bundles/js").Include(
               "~/Scripts/respond.js",
               "~/Scripts/jquery.cookie.js",
               "~/Scripts/jquery.validate*",
               "~/Scripts/modernizr-*",
               "~/Scripts/easyXDM.js",
               "~/Scripts/visninger.js"));

            bundles.Add(new StyleBundle("~/Content/shopping-cart").Include(
                "~/Content/shopping-cart.css"));

            bundles.Add(new ScriptBundle("~/bundles/shopping-cart").Include(
                "~/Scripts/shopping-cart.js"));

            BundleTable.EnableOptimizations = true;
        }
    }
}
