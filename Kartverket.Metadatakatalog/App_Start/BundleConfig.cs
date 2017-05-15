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
            //***************** complete bundle - remember to update this as well as the separate bundles ****************//


            bundles.Add(new StyleBundle("~/Content/bower_components/kartverket-felleskomponenter/assets/css/styles").Include(
                "~/Content/bower_components/kartverket-felleskomponenter/assets/css/vendor.min.css",
                "~/Content/bower_components/kartverket-felleskomponenter/assets/css/vendorfonts.min.css",
                "~/Content/bower_components/kartverket-felleskomponenter/assets/css/main.min.css",
                "~/Content/temp.css"
                ));

            bundles.Add(new ScriptBundle("~/Content/bower_components/kartverket-felleskomponenter/assets/js/scripts").Include(
               "~/Content/bower_components/kartverket-felleskomponenter/assets/js/vendor.min.js",
               "~/Content/bower_components/vue/dist/vue.js",
               "~/Content/bower_components/kartverket-felleskomponenter/assets/js/main.min.js",
               "~/Content/bower_components/clipboard/dist/clipboard.min.js",
               "~/Content/bower_components/axios/dist/axios.min.js",
               "~/Scripts/site.js",
               "~/Scripts/visninger.js"
           ));

            //***************** separate bundles - used in www.geonorge.no/www.test.geonorge.no ****************//

            bundles.Add(new StyleBundle("~/Content/shopping-cart").Include(
                "~/Content/shopping-cart.css"));

            bundles.Add(new ScriptBundle("~/bundles/shopping-cart").Include(
                "~/Scripts/shopping-cart.js"));

            BundleTable.EnableOptimizations = false;
        }
    }
}
