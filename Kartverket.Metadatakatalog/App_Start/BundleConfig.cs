﻿using System.Web;
using System.Web.Optimization;

namespace Kartverket.Metadatakatalog
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"
                        ));
            //"~/Scripts/jquery.typeahead.js"

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"
                        ));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/common.css",
                      "~/Content/navbar.css",
                      "~/Content/searchbar.css",
                     
                      "~/Content/site.css"));
            // "~/Content/jquery.typeahead.css",
            //bundles.Add(new StyleBundle("~/Content/css").Include(
            //          "~/Content/bootstrap.css",
            //          "~/Content/geonorge-default.css",
            //          "~/Content/site.css"));
            // Set EnableOptimizations to false for debugging. For more information,
            // visit http://go.microsoft.com/fwlink/?LinkId=301862
            
            bundles.Add(new ScriptBundle("~/bundles/js").Include(
               "~/Scripts/visninger.js"));

            BundleTable.EnableOptimizations = true;
        }
    }
}
