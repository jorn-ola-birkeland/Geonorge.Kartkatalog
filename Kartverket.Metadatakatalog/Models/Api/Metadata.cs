﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Xml;

namespace Kartverket.Metadatakatalog.Models.Api
{
    public class Metadata
    {
        /// <summary>
        /// The uniqueidentifier
        /// </summary>
        public string Uuid { get; set; }
        /// <summary>
        /// The title
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The abstract
        /// </summary>
        public string @Abstract { get; set; }
        /// <summary>
        /// The type of metadata
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The theme
        /// </summary>
        public string Theme { get; set; }
        /// <summary>
        /// The owner of the metadata
        /// </summary>
        public string Organization { get; set; }
        /// <summary>
        /// The logo for the organization
        /// </summary>
        public string OrganizationLogo { get; set; }
        /// <summary>
        /// Illustrative image of the metadata
        /// </summary>
        public string ThumbnailUrl { get; set; }
        /// <summary>
        /// Url for downloading dataset/service
        /// </summary>
        public string DistributionUrl { get; set; }
        /// <summary>
        /// The protocol used for downloading
        /// </summary>
        public string DistributionProtocol { get; set; }
        /// <summary>
        /// Url to metadata details page
        /// </summary>
        public string ShowDetailsUrl { get; set; }
        /// <summary>
        /// Url to metadata owner organization details page
        /// </summary>
        public string OrganizationUrl { get; set; }
        /// <summary>
        /// The layer for services
        /// </summary>
        public string DistributionName { get; set; }
        /// <summary>
        /// True if one of the nationalinitiativs(Samarbeid og lover) is "Åpne data"
        /// </summary>
        public bool IsOpenData { get; set; }
        /// <summary>
        /// <summary>
        /// True if one of the nationalinitiativs(Samarbeid og lover) is "Det offentlige kartgrunnlaget"
        /// </summary>
        public bool IsDokData { get; set; }
        /// <summary>
        /// Url for legend/drawing rules
        /// </summary>
        public string LegendDescriptionUrl { get; set; }
        /// <summary>
        /// Url for productsheet
        /// </summary>
        public string ProductSheetUrl { get; set; }
        /// <summary>
        /// Url for detailed spesifications
        /// </summary>
        public string ProductSpecificationUrl { get; set; }
        /// <summary>
        /// Services for dataset
        /// </summary>
        public List<string> DatasetServices { get; set; }
        /// <summary>
        ///  Datasets for service
        /// </summary>
        public List<string> ServiceDatasets { get; set; }
        /// <summary>
        /// Bundles for dataset
        /// </summary>
        public List<string> Bundles { get; set; }
        /// <summary>
        /// ServiceLayers
        /// </summary>
        public List<string> ServiceLayers { get; set; }
        /// <summary>
        /// AccessConstraint
        /// </summary>
        public string AccessConstraint { get; set; }
        /// <summary>
        /// OtherConstraintsAccess
        /// </summary>
        public string OtherConstraintsAccess { get; set; }
        /// <summary>
        /// DataAccess
        /// </summary>
        public string DataAccess { get; set; }
        /// <summary>
        /// Url for service mapped to dataset (wms)
        /// </summary>
        public string ServiceDistributionUrlForDataset { get; set; }
        /// <summary>
        /// Url for service mapped to dataset (wfs)
        /// </summary>
        public string ServiceWfsDistributionUrlForDataset { get; set; }

        /// <summary>
        /// DistributionType
        /// </summary>
        //public string DistributionType { get; set; } 
        public string DistributionType { get; set; }
        /// <summary>
        /// URL for Get Capabilities
        /// </summary>
        public string GetCapabilitiesUrl { get; set; }


        public Metadata() { 
        }
        public Metadata(SearchResultItem item, UrlHelper urlHelper)
        {
            Uuid = item.Uuid;
            Title = item.Title;
            Abstract = item.Abstract;
            Type = item.Type;
            Theme = item.Theme;
            Organization = item.Organization;
            OrganizationLogo = item.OrganizationLogoUrl;
            ThumbnailUrl = item.ThumbnailUrl;
            DistributionUrl = item.DistributionUrl;
            DistributionProtocol = item.DistributionProtocol;
            DistributionName = item.DistributionName;
            DistributionType = item.DistributionType;
            GetCapabilitiesUrl = GetGetCapabilitiesUrl(item);
            if (urlHelper != null)
            {
                ShowDetailsUrl = WebConfigurationManager.AppSettings["KartkatalogenUrl"] + "metadata/uuid/" + item.Uuid;
                string s = new SeoUrl(item.Organization, "").Organization;
                OrganizationUrl = WebConfigurationManager.AppSettings["KartkatalogenUrl"] + "metadata/" + s;
            }

            if (item.NationalInitiative != null && item.NationalInitiative.Contains("Åpne data"))
                IsOpenData = true;
            else IsOpenData = false;

            if (item.NationalInitiative != null && item.NationalInitiative.Contains("Det offentlige kartgrunnlaget"))
                IsDokData = true;
            else IsDokData = false;


            LegendDescriptionUrl = item.LegendDescriptionUrl;
            ProductSheetUrl = item.ProductSheetUrl;
            ProductSpecificationUrl = item.ProductSpecificationUrl;
            DatasetServices = item.DatasetServices;
            ServiceDatasets = item.ServiceDatasets;
            Bundles = item.Bundles;
            ServiceLayers = item.ServiceLayers;
            AccessConstraint = item.AccessConstraint;
            OtherConstraintsAccess = item.OtherConstraintsAccess;
            DataAccess = item.DataAccess;
            ServiceDistributionUrlForDataset = item.ServiceDistributionUrlForDataset;
            ServiceWfsDistributionUrlForDataset = item.ServiceWfsDistributionUrlForDataset != null ? item.ServiceWfsDistributionUrlForDataset : WfsServiceUrl();
        }

        private string GetGetCapabilitiesUrl(SearchResultItem item)
        {
            DistributionDetails distributionDetails = new DistributionDetails(null, item.DistributionProtocol, null, item.DistributionUrl);
            return distributionDetails.DistributionDetailsGetCapabilitiesUrl();
        }

        public static List<Metadata> CreateFromList(IEnumerable<SearchResultItem> items, UrlHelper urlHelper)
        {
            return items.Select(item => new Metadata(item, urlHelper)).ToList();
        }

        public string WfsServiceUrl()
        {
            string wfsServiceUrl = "";

            if(DatasetServices != null)
            { 
                foreach (var service in DatasetServices)
                {
                    var data = service.Split('|');
                    if (data.Count() >= 7 && data[6].ToString() == "OGC:WFS")
                        wfsServiceUrl = data[7].ToString();
                }
            }

            return wfsServiceUrl;
        }

        private System.Xml.XmlDocument AtomFeedDoc;
        XmlNamespaceManager nsmgr;

        public string AtomFeed()
        {
            string atomFeed = "";
            MemoryCache memoryCache = MemoryCache.Default;
            AtomFeedDoc = memoryCache.Get("AtomFeedDoc") as System.Xml.XmlDocument;
            if (AtomFeedDoc == null)
                SetAtomFeed();

            atomFeed = GetAtomFeed();

            return atomFeed;
        }

        private string GetAtomFeed()
        {
            nsmgr = new XmlNamespaceManager(AtomFeedDoc.NameTable);
            nsmgr.AddNamespace("ns", "http://www.w3.org/2005/Atom");
            nsmgr.AddNamespace("georss", "http://www.georss.org/georss");
            nsmgr.AddNamespace("inspire_dls", "http://inspire.ec.europa.eu/schemas/inspire_dls/1.0");

            string feed = "";
            XmlNode entry = AtomFeedDoc.SelectSingleNode("//ns:feed/ns:entry[inspire_dls:spatial_dataset_identifier_code='" + Uuid + "']/ns:link", nsmgr);
            if (entry != null) {
                feed = entry.InnerText;
            }

            return feed;
        }

        private void SetAtomFeed()
        {
            AtomFeedDoc = new XmlDocument();
            AtomFeedDoc.Load("https://nedlasting.geonorge.no/geonorge/Tjenestefeed.xml");

            MemoryCache memoryCache = MemoryCache.Default;
            memoryCache.Add("AtomFeedDoc", AtomFeedDoc, new DateTimeOffset(DateTime.Now.AddDays(1)));
        }
    }
}