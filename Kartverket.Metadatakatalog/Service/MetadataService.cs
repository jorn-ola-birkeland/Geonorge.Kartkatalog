﻿using System.Collections.Generic;
using System.Threading.Tasks;
using GeoNorgeAPI;
using Kartverket.Geonorge.Utilities;
using Kartverket.Geonorge.Utilities.Organization;
using Kartverket.Metadatakatalog.Models;
using www.opengis.net;
using System;
using Kartverket.Metadatakatalog.Service.Application;
using System.Linq;
using Kartverket.Metadatakatalog.Models.Api;
using Kartverket.Metadatakatalog.Models.ViewModels;
using Kartverket.Metadatakatalog.Service.Search;
using Kartverket.Metadatakatalog.Service.ServiceDirectory;
using static System.String;
using Contact = Kartverket.Metadatakatalog.Models.Contact;
using DistributionFormat = Kartverket.Metadatakatalog.Models.DistributionFormat;
using Keyword = Kartverket.Metadatakatalog.Models.Keyword;
using SearchParameters = Kartverket.Metadatakatalog.Models.SearchParameters;
using SearchResult = Kartverket.Metadatakatalog.Models.SearchResult;
using SolrNet;
using Kartverket.Metadatakatalog.Helpers;
using Kartverket.Metadatakatalog.Models.Translations;
using Resources;

namespace Kartverket.Metadatakatalog.Service
{
    public class MetadataService : IMetadataService
    {
        private readonly IGeoNorge _geoNorge;
        private readonly GeoNetworkUtil _geoNetworkUtil;
        private readonly IGeonorgeUrlResolver _geonorgeUrlResolver;
        private readonly IOrganizationService _organizationService;
        private readonly ISearchService _searchService;
        private readonly IServiceDirectoryService _searchServiceDirectoryService;
        private readonly ThemeResolver _themeResolver;
        public RegisterFetcher Register = new RegisterFetcher();
        public string Culture = Models.Translations.Culture.NorwegianCode;

        public MetadataService(IGeoNorge geoNorge, GeoNetworkUtil geoNetworkUtil, IGeonorgeUrlResolver geonorgeUrlResolver, IOrganizationService organizationService, ISearchService searchService, IServiceDirectoryService searchServiceDirectoryService, ThemeResolver themeResolver)
        {
            _geoNorge = geoNorge;
            _geoNetworkUtil = geoNetworkUtil;
            _geonorgeUrlResolver = geonorgeUrlResolver;
            _organizationService = organizationService;
            _searchService = searchService;
            _searchServiceDirectoryService = searchServiceDirectoryService;
            _themeResolver = themeResolver;
        }



        public List<Models.Api.Distribution> GetRelatedDistributionsForUuid(string uuid)
        {
            List<Models.Api.Distribution> distlist = new List<Models.Api.Distribution>();
            Culture = CultureHelper.GetCurrentCulture();
            //Henter distribusjoner - mulig å få raskere med å lese søkeindex
            MD_Metadata_Type mdMetadataType = _geoNorge.GetRecordByUuid(uuid);
            if (mdMetadataType == null)
                return null;

            var simpleMetadata = new SimpleMetadata(mdMetadataType);
            if (simpleMetadata.DistributionsFormats != null)
            {
                var distributionRows = new Dictionary<DistributionRow, Distribution>();
                foreach (var simpleMetadataDistribution in simpleMetadata.DistributionsFormats)
                {
                    var row = new DistributionRow(simpleMetadataDistribution);

                    if (distributionRows.ContainsKey(row))
                    {
                        var distributionRow = distributionRows[row];
                        distributionRow.DistributionFormats.Add(GetDistributionFormat(simpleMetadataDistribution));
                    }
                    else
                    {
                        var distribution = new Distribution();
                        distribution.Uuid = uuid;
                        distribution.Title = GetTranslation(simpleMetadata.Title, simpleMetadata.EnglishTitle);
                        distribution.Type = SimpleMetadataUtil.ConvertHierarchyLevelToType(simpleMetadata.HierarchyLevel);
                        distribution.DistributionFormats.Add(GetDistributionFormat(simpleMetadataDistribution));
                        distribution.Organization = simpleMetadata.ContactMetadata != null
                            ? GetTranslation(simpleMetadata.ContactMetadata.Organization,
                                simpleMetadata.ContactMetadata.OrganizationEnglish)
                            : UI.NotSet;
                        distribution.ShowDetailsUrl = "/metadata/org/title/" + uuid;
                        if (simpleMetadata.Constraints != null)
                            distribution.ServiceDistributionAccessConstraint = simpleMetadata.Constraints.AccessConstraints;
                        distribution.Protocol = Register.GetDistributionType(simpleMetadataDistribution.Protocol);

                        //Vis kart
                        if (SimpleMetadataUtil.ShowMapLink(simpleMetadataDistribution, simpleMetadata.HierarchyLevel))
                        {
                            distribution.MapUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["NorgeskartUrl"] +
                                         SimpleMetadataUtil.MapUrl(simpleMetadataDistribution, simpleMetadata.HierarchyLevel);
                            distribution.CanShowMapUrl = true;
                        }
                        //Last ned
                        if (SimpleMetadataUtil.ShowDownloadLink(simpleMetadataDistribution, simpleMetadata.HierarchyLevel))
                        {
                            distribution.DistributionUrl = simpleMetadataDistribution.URL;
                            distribution.CanShowDownloadUrl = true;
                        }
                        //Handlekurv
                        if (SimpleMetadataUtil.ShowDownloadService(simpleMetadataDistribution))
                        {
                            distribution.DownloadUrl = simpleMetadataDistribution.URL;
                            distribution.CanShowDownloadService = true;
                        }

                        //Åpne data, begrenset, skjermet
                        if (SimpleMetadataUtil.IsOpendata(simpleMetadata)) distribution.AccessIsOpendata = true;
                        if (SimpleMetadataUtil.IsRestricted(simpleMetadata)) distribution.AccessIsRestricted = true;
                        if (SimpleMetadataUtil.IsProtected(simpleMetadata)) distribution.AccessIsProtected = true;
                        distributionRows.Add(row, distribution);
                    }
                }

                foreach (var distribution in distributionRows)
                {
                    distlist.Add(distribution.Value);
                }

            }

            //Hente inn indeks og relaterte services
            distlist.AddRange(GetMetadataRelatedDistributions(uuid));
            if (simpleMetadata.IsService())
                distlist.AddRange(GetServiceDirectoryRelatedDistributions(uuid));
            distlist.AddRange(GetApplicationRelatedDistributions(uuid));
            return distlist;
        }

        private DistributionFormat GetDistributionFormat(SimpleDistribution simpleMetadataDistribution)
        {
            return new DistributionFormat
            {
                Name = Register.GetDistributionType(simpleMetadataDistribution.FormatName),
                Version = simpleMetadataDistribution.FormatVersion
            };
        }

        private List<Models.Api.Distribution> GetApplicationRelatedDistributions(string uuid)
        {
            List<Models.Api.Distribution> distlist = new List<Models.Api.Distribution>();

            SolrNet.ISolrOperations<ApplicationIndexDoc> _solrInstance;
            _solrInstance = MvcApplication.indexContainer.Resolve<ISolrOperations<ApplicationIndexDoc>>(CultureHelper.GetIndexCore(SolrCores.Applications));

            SolrNet.ISolrQuery query = new SolrNet.SolrQuery("applicationdataset:" + uuid + "*");
            try
            {
                SolrNet.SolrQueryResults<ApplicationIndexDoc> queryResults = _solrInstance.Query(query, new SolrNet.Commands.Parameters.QueryOptions
                {
                    Fields = new[] { "uuid", "title", "abstract", "purpose", "type", "theme", "organization", "organization_seo_lowercase", "placegroups", "organizationgroup",
                    "topic_category", "organization_logo_url",  "thumbnail_url","distribution_url","distribution_protocol","distribution_name","product_page_url", "date_published", "date_updated", "nationalinitiative",
                    "score", "ServiceDistributionProtocolForDataset", "ServiceDistributionUrlForDataset", "ServiceDistributionNameForDataset", "DistributionProtocols", "legend_description_url", "product_sheet_url", "product_specification_url", "area", "datasetservice", "popularMetadata", "bundle", "servicelayers", "accessconstraint", "servicedataset", "otherconstraintsaccess", "dataaccess", "ServiceDistributionUuidForDataset", "ServiceDistributionAccessConstraint", "parentidentifier" }

                });

                foreach (var result in queryResults)
                {
                    var md = new Models.Api.Distribution();
                    try
                    {
                        md.Uuid = result.Uuid;
                        md.Title = result.Title;
                        md.Type = "Applikasjon";
                        md.Organization = result.Organization;

                        md.DownloadUrl = result.DistributionUrl;

                        //Åpne data, begrenset, skjermet
                        if (SimpleMetadataUtil.IsOpendata(result.OtherConstraintsAccess)) md.AccessIsOpendata = true;
                        if (SimpleMetadataUtil.IsRestricted(result.OtherConstraintsAccess)) md.AccessIsRestricted = true;
                        if (SimpleMetadataUtil.IsProtected(result.AccessConstraint)) md.AccessIsProtected = true;

                        distlist.Add(md);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            catch (Exception ex) { }

            return distlist;
        }

        public SearchResultItemViewModel Metadata(string uuid)
        {
            SearchResultItem metadata = null;

            SolrNet.ISolrOperations<MetadataIndexDoc> _solrInstance;
            _solrInstance = MvcApplication.indexContainer.Resolve<ISolrOperations<MetadataIndexDoc>>(CultureHelper.GetIndexCore(SolrCores.Metadata));

            SolrNet.ISolrQuery query = new SolrNet.SolrQuery("uuid:" + uuid);
            try
            {
                SolrNet.SolrQueryResults<MetadataIndexDoc> queryResults = _solrInstance.Query(query, new SolrNet.Commands.Parameters.QueryOptions
                {
                    Fields = new[] { "uuid", "title", "abstract", "purpose", "type", "theme", "organization", "organization_seo_lowercase", "placegroups", "organizationgroup",
                    "topic_category", "organization_logo_url",  "thumbnail_url","distribution_url","distribution_protocol","distribution_name","product_page_url", "date_published", "date_updated", "nationalinitiative",
                    "score", "ServiceDistributionProtocolForDataset", "ServiceDistributionUrlForDataset", "ServiceDistributionNameForDataset", "DistributionProtocols", "legend_description_url", "product_sheet_url", "product_specification_url", "area", "datasetservice", "popularMetadata", "bundle", "servicelayers", "accessconstraint", "servicedataset", "otherconstraintsaccess", "dataaccess", "ServiceDistributionUuidForDataset", "ServiceDistributionAccessConstraint", "parentidentifier" }

                });

                metadata = new SearchResultItem(queryResults.FirstOrDefault());

            }
            catch (Exception ex) { }

            return new SearchResultItemViewModel(metadata);
        }

        private List<Models.Api.Distribution> GetServiceDirectoryRelatedDistributions(string uuid)
        {
            List<Models.Api.Distribution> distlist = new List<Models.Api.Distribution>();

            SearchParameters parameters = new SearchParameters();
            parameters.Text = uuid;
            SearchResult searchResult = _searchServiceDirectoryService.Services(parameters);

            if (searchResult != null && searchResult.NumFound > 0)
            {
                var serviceDatasets = searchResult.Items[0].ServiceDatasets;

                if (serviceDatasets != null && serviceDatasets.Count > 0)
                {
                    foreach (var relatert in serviceDatasets)
                    {
                        var relData = relatert.Split('|');

                        try
                        {
                            var tmp = new Models.Api.Distribution();
                            tmp.Uuid = relData[0] != null ? relData[0] : "";
                            tmp.Title = relData[1] != null ? relData[1] : "";
                            tmp.Type = relData[3] != null ? relData[3] : "";
                            tmp.Type = SimpleMetadataUtil.ConvertHierarchyLevelToType(tmp.Type);
                            tmp.DistributionFormats.Add(new DistributionFormat()
                            {
                                Name = relData[6] != null ? Register.GetDistributionType(relData[6]) : "",
                                Version = ""
                            });
                            tmp.DistributionName = relData[5] != null ? relData[5] : "";
                            tmp.Protocol = relData[6] != null ? relData[6] : "";
                            tmp.DistributionUrl = relData[7] != null ? relData[7] : "";
                            tmp.Organization = relData[4];
                            tmp.ShowDetailsUrl = "/metadata/org/title/" + tmp.Uuid;

                            //Åpne data, begrenset, skjermet
                            if (SimpleMetadataUtil.IsOpendata(relData[12])) tmp.AccessIsOpendata = true;
                            if (SimpleMetadataUtil.IsRestricted(relData[12])) tmp.AccessIsRestricted = true;
                            if (SimpleMetadataUtil.IsProtected(relData[11])) tmp.AccessIsProtected = true;

                            //Vis kart
                            if (relData[6] == "OGC:WMS" || relData[6] == "OGC:WFS")
                            {
                                tmp.MapUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["NorgeskartUrl"] + SimpleMetadataUtil.MapUrl(relData[7], relData[3], relData[6], relData[5]);
                                tmp.CanShowMapUrl = true;
                            }

                            distlist.Add(tmp);

                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }

                var serviceLayers = searchResult.Items[0].ServiceLayers;

                if (serviceLayers != null && serviceLayers.Count > 0)
                {

                    foreach (var relatert in serviceLayers)
                    {
                        var relData = relatert.Split('|');

                        try
                        {
                            var tmp = new Models.Api.Distribution();
                            tmp.Uuid = relData[0] != null ? relData[0] : "";
                            tmp.Title = relData[1] != null ? relData[1] : "";
                            string parentIdentifier = relData[2] != null ? relData[2] : "";
                            tmp.Type = relData[3] != null ? relData[3] : "";
                            if (tmp.Type == "service" && !IsNullOrEmpty(parentIdentifier))
                                tmp.Type = "servicelayer";
                            tmp.Type = SimpleMetadataUtil.ConvertHierarchyLevelToType(tmp.Type);
                            tmp.DistributionFormats.Add(new DistributionFormat()
                            {
                                Name = relData[6] != null ? Register.GetDistributionType(relData[6]) : "",
                                Version = ""
                            });
                            tmp.DistributionName = relData[5] != null ? relData[5] : "";
                            tmp.Protocol = relData[6] != null ? relData[6] : "";
                            tmp.DistributionUrl = relData[7] != null ? relData[7] : "";
                            tmp.Organization = relData[4];
                            tmp.ShowDetailsUrl = "/metadata/org/title/" + tmp.Uuid;

                            //Åpne data, begrenset, skjermet
                            if (SimpleMetadataUtil.IsOpendata(relData[12])) tmp.AccessIsOpendata = true;
                            if (SimpleMetadataUtil.IsRestricted(relData[12])) tmp.AccessIsRestricted = true;
                            if (SimpleMetadataUtil.IsProtected(relData[11])) tmp.AccessIsProtected = true;
                            tmp.ServiceDistributionAccessConstraint = !IsNullOrWhiteSpace(relData[12]) ? relData[12] : relData[11];

                            //Vis kart
                            if (relData[6] == "OGC:WMS" || relData[6] == "OGC:WFS")
                            {
                                tmp.MapUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["NorgeskartUrl"] + SimpleMetadataUtil.MapUrl(relData[7], relData[3], relData[6], relData[5]);
                                tmp.CanShowMapUrl = true;
                            }

                            distlist.Add(tmp);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }
            return distlist;
        }

        private List<Models.Api.Distribution> GetMetadataRelatedDistributions(string uuid)
        {
            Register = new RegisterFetcher();
            List<Models.Api.Distribution> distlist = new List<Models.Api.Distribution>();

            SearchParameters parameters = new SearchParameters();
            parameters.Text = uuid;
            SearchResult searchResult = _searchService.Search(parameters);

            if (searchResult != null && searchResult.NumFound > 0)
            {
                var datasetServices = searchResult.Items[0].DatasetServices;

                if (datasetServices != null && datasetServices.Count > 0)
                {
                    foreach (var relatert in datasetServices)
                    {
                        var relData = relatert.Split('|');

                        try
                        {
                            var tmp = new Models.Api.Distribution();
                            tmp.Uuid = relData[0] != null ? relData[0] : "";
                            tmp.Title = relData[1] != null ? relData[1] : "";
                            string parentIdentifier = relData[2] != null ? relData[2] : "";
                            tmp.Type = relData[3] != null ? relData[3] : "";
                            if (tmp.Type == "service" && !IsNullOrEmpty(parentIdentifier))
                                tmp.Type = "servicelayer";
                            tmp.Type = SimpleMetadataUtil.ConvertHierarchyLevelToType(tmp.Type);
                            tmp.DistributionFormats.Add(new DistributionFormat()
                            {
                                Name = relData[6] != null ? Register.GetDistributionType(relData[6]) : "",
                                Version = ""
                            });
                            tmp.Protocol = relData[6] != null ? Register.GetDistributionType(relData[6]) : "";
                            tmp.Organization = relData[4];
                            tmp.ShowDetailsUrl = "/metadata/org/title/" + tmp.Uuid;

                            //Åpne data, begrenset, skjermet
                            if (SimpleMetadataUtil.IsOpendata(relData[12])) tmp.AccessIsOpendata = true;
                            if (SimpleMetadataUtil.IsRestricted(relData[12])) tmp.AccessIsRestricted = true;
                            if (SimpleMetadataUtil.IsProtected(relData[11])) tmp.AccessIsProtected = true;
                            tmp.ServiceDistributionAccessConstraint = !IsNullOrWhiteSpace(relData[12]) ? relData[12] : relData[11];

                            //Vis kart
                            if (relData[6] == "OGC:WMS" || relData[6] == "OGC:WFS")
                            {
                                tmp.MapUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["NorgeskartUrl"] + SimpleMetadataUtil.MapUrl(relData[7], relData[3], relData[6], relData[5]);
                                tmp.CanShowMapUrl = true;
                            }

                            distlist.Add(tmp);

                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }

                var bundles = searchResult.Items[0].Bundles;

                if (bundles != null && bundles.Count > 0)
                {
                    foreach (var relatert in bundles)
                    {
                        var relData = relatert.Split('|');

                        try
                        {
                            var tmp = new Models.Api.Distribution();
                            tmp.Uuid = relData[0] != null ? relData[0] : "";
                            tmp.Title = relData[1] != null ? relData[1] : "";
                            string parentIdentifier = relData[2] != null ? relData[2] : "";
                            tmp.Type = "Datasett";
                            tmp.DistributionFormats.Add(new DistributionFormat()
                            {
                                Name = relData[6] != null ? Register.GetDistributionType(relData[6]) : "",
                                Version = ""
                            });
                            tmp.Protocol = relData[6] != null ? Register.GetDistributionType(relData[6]) : "";
                            tmp.Organization = relData[4];
                            tmp.ShowDetailsUrl = "/metadata/org/title/" + tmp.Uuid;

                            //Åpne data, begrenset, skjermet
                            if (SimpleMetadataUtil.IsOpendata(relData[12])) tmp.AccessIsOpendata = true;
                            if (SimpleMetadataUtil.IsRestricted(relData[12])) tmp.AccessIsRestricted = true;
                            if (SimpleMetadataUtil.IsProtected(relData[11])) tmp.AccessIsProtected = true;
                            tmp.ServiceDistributionAccessConstraint = !IsNullOrWhiteSpace(relData[12]) ? relData[12] : relData[11];

                            //Vis kart
                            if (relData[6] == "OGC:WMS" || relData[6] == "OGC:WFS")
                            {
                                tmp.MapUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["NorgeskartUrl"] + SimpleMetadataUtil.MapUrl(relData[7], relData[3], relData[6], relData[5]);
                                tmp.CanShowMapUrl = true;
                            }

                            distlist.Add(tmp);

                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }

            }
            return distlist;
        }


        public MetadataViewModel GetMetadataByUuid(string uuid)
        {
            MD_Metadata_Type mdMetadataType = _geoNorge.GetRecordByUuid(uuid);
            if (mdMetadataType == null)
                return null;

            var simpleMetadata = new SimpleMetadata(mdMetadataType);
            return CreateMetadataViewModel(simpleMetadata);
        }

        private MetadataViewModel CreateMetadataViewModel(SimpleMetadata simpleMetadata)
        {
            Culture = CultureHelper.GetCurrentCulture();
            Register = new RegisterFetcher();

            var metadata = new MetadataViewModel
            {
                Abstract = GetTranslation(simpleMetadata.Abstract, simpleMetadata.EnglishAbstract),
                BoundingBox = Convert(simpleMetadata.BoundingBox),
                Constraints = Convert(simpleMetadata.Constraints),
                ContactMetadata = Convert(simpleMetadata.ContactMetadata),
                ContactOwner = Convert(simpleMetadata.ContactOwner),
                ContactPublisher = Convert(simpleMetadata.ContactPublisher),
                DateCreated = simpleMetadata.DateCreated,
                DateMetadataUpdated = simpleMetadata.DateMetadataUpdated,
                DatePublished = simpleMetadata.DatePublished,
                DateUpdated = simpleMetadata.DateUpdated,
                DistributionDetails = Convert(simpleMetadata.DistributionDetails),
                DistributionFormat = Convert(simpleMetadata.DistributionFormat),
                EnglishAbstract = simpleMetadata.EnglishAbstract,
                EnglishTitle = simpleMetadata.EnglishTitle,
                HierarchyLevel = simpleMetadata.HierarchyLevel,
                KeywordsPlace = Convert(SimpleKeyword.Filter(simpleMetadata.Keywords, SimpleKeyword.TYPE_PLACE, null)),
                KeywordsTheme = Convert(SimpleKeyword.Filter(simpleMetadata.Keywords, SimpleKeyword.TYPE_THEME, null)),
                KeywordsInspire =
                    Convert(SimpleKeyword.Filter(simpleMetadata.Keywords, null, SimpleKeyword.THESAURUS_GEMET_INSPIRE_V1)),
                KeywordsNationalInitiative =
                    Convert(SimpleKeyword.Filter(simpleMetadata.Keywords, null,
                        SimpleKeyword.THESAURUS_NATIONAL_INITIATIVE)),
                KeywordsNationalTheme =
                    Convert(SimpleKeyword.Filter(simpleMetadata.Keywords, null, SimpleKeyword.THESAURUS_NATIONAL_THEME)),
                KeywordsOther = Convert(SimpleKeyword.Filter(simpleMetadata.Keywords, null, null)),
                KeywordsConcept =
                    Convert(SimpleKeyword.Filter(simpleMetadata.Keywords, null, SimpleKeyword.THESAURUS_CONCEPT)),
                LegendDescriptionUrl = simpleMetadata.LegendDescriptionUrl,
                MaintenanceFrequency = Register.GetMaintenanceFrequency(simpleMetadata.MaintenanceFrequency),
                MetadataLanguage = simpleMetadata.MetadataLanguage,
                MetadataStandard = simpleMetadata.MetadataStandard,
                MetadataStandardVersion = simpleMetadata.MetadataStandardVersion,
                OperatesOn = simpleMetadata.OperatesOn,
                ProcessHistory = GetTranslation(simpleMetadata.ProcessHistory, simpleMetadata.EnglishProcessHistory),
                ProductPageUrl = simpleMetadata.ProductPageUrl,
                ProductSheetUrl = simpleMetadata.ProductSheetUrl,
                ProductSpecificationUrl = simpleMetadata.ProductSpecificationUrl,
                CoverageUrl = simpleMetadata.CoverageUrl,
                Purpose = GetTranslation(simpleMetadata.Purpose, simpleMetadata.EnglishPurpose),
                QualitySpecifications = Convert(simpleMetadata.QualitySpecifications),
                ReferenceSystem = Convert(simpleMetadata.ReferenceSystem),
                ResolutionScale = simpleMetadata.ResolutionScale,
                SpatialRepresentation = Register.GetSpatialRepresentation(simpleMetadata.SpatialRepresentation),
                SpecificUsage = simpleMetadata.SpecificUsage,
                Status = Register.GetStatus(simpleMetadata.Status),
                SupplementalDescription = GetTranslation(simpleMetadata.SupplementalDescription, simpleMetadata.EnglishSupplementalDescription),
                HelpUrl = simpleMetadata.HelpUrl,
                Thumbnails = Convert(simpleMetadata.Thumbnails, simpleMetadata.Uuid),
                Title = GetTranslation(simpleMetadata.Title, simpleMetadata.EnglishTitle),
                TopicCategory = Register.GetTopicCategory(simpleMetadata.TopicCategory),
                Uuid = simpleMetadata.Uuid,
                ServiceUuid = simpleMetadata.Uuid,
                MetadataXmlUrl = _geoNetworkUtil.GetXmlDownloadUrl(simpleMetadata.Uuid),
                MetadataEditUrl = _geonorgeUrlResolver.EditMetadata(simpleMetadata.Uuid),
                ParentIdentifier = simpleMetadata.ParentIdentifier,
                DateMetadataValidFrom =
                    IsNullOrEmpty(simpleMetadata.ValidTimePeriod.ValidFrom)
                        ? (DateTime?)null
                        : DateTime.Parse(simpleMetadata.ValidTimePeriod.ValidFrom),
                DateMetadataValidTo =
                    IsNullOrEmpty(simpleMetadata.ValidTimePeriod.ValidTo)
                        ? (DateTime?)null
                        : DateTime.Parse(simpleMetadata.ValidTimePeriod.ValidTo),
                DistributionFormats = simpleMetadata.DistributionFormats,
                UnitsOfDistribution =
                    simpleMetadata.DistributionDetails != null
                        ? GetTranslation(simpleMetadata.DistributionDetails.UnitsOfDistribution, simpleMetadata.DistributionDetails.EnglishUnitsOfDistribution)
                        : null,
                ReferenceSystems =
                    simpleMetadata.ReferenceSystems != null ? Convert(simpleMetadata.ReferenceSystems) : null,
            };

            metadata.SetDistributionUrl();

            if (!IsNullOrEmpty(metadata.ParentIdentifier) && metadata.HierarchyLevel == "service")
                metadata.ServiceUuid = metadata.ParentIdentifier;

            if (simpleMetadata.ResourceReference != null)
            {
                metadata.ResourceReferenceCode = simpleMetadata.ResourceReference.Code != null
                    ? simpleMetadata.ResourceReference.Code
                    : null;
                metadata.ResourceReferenceCodespace = simpleMetadata.ResourceReference.Codespace != null
                    ? simpleMetadata.ResourceReference.Codespace
                    : null;
            }

            if (metadata.ContactOwner != null)
            {
                Task<Organization> getOrganizationTask =
                    _organizationService.GetOrganizationByName(metadata.ContactOwner.Organization);
                Organization organization = getOrganizationTask.Result;
                if (organization != null)
                {
                    metadata.OrganizationLogoUrl = organization.LogoUrl;
                }
            }

            SearchParameters parameters = new SearchParameters();
            parameters.Text = simpleMetadata.Uuid;
            SearchResult searchResult = _searchService.Search(parameters);

            if (searchResult != null && searchResult.NumFound > 0)
            {
                if (metadata.IsDataset())
                {

                    metadata.ServiceDistributionProtocolForDataset =
                    searchResult.Items[0].ServiceDistributionProtocolForDataset != null
                        ? searchResult.Items[0].ServiceDistributionProtocolForDataset
                        : null;
                    metadata.ServiceDistributionUrlForDataset = searchResult.Items[0].ServiceDistributionUrlForDataset !=
                                                                null
                        ? searchResult.Items[0].ServiceDistributionUrlForDataset
                        : null;
                    metadata.ServiceDistributionNameForDataset = searchResult.Items[0].ServiceDistributionNameForDataset !=
                                                                 null
                        ? searchResult.Items[0].ServiceDistributionNameForDataset
                        : null;
                    metadata.ServiceUuid = searchResult.Items[0].ServiceDistributionUuidForDataset != null
                        ? searchResult.Items[0].ServiceDistributionUuidForDataset
                        : null;
                }
                metadata.ServiceDistributionAccessConstraint = searchResult.Items[0].ServiceDistributionAccessConstraint;

                var datasetServices = searchResult.Items[0].DatasetServices;

                if (metadata.IsDataset() && datasetServices != null && datasetServices.Count > 0)
                {
                    metadata.Related = new List<MetadataViewModel>();

                    foreach (var relatert in datasetServices)
                    {
                        var relData = relatert.Split('|');

                        try
                        {
                            MetadataViewModel md = new MetadataViewModel();
                            md.Uuid = relData[0] != null ? relData[0] : "";
                            md.Title = relData[1] != null ? relData[1] : "";
                            md.ParentIdentifier = relData[2] != null ? relData[2] : "";
                            md.HierarchyLevel = relData[3] != null ? relData[3] : "";
                            md.HierarchyLevel = SimpleMetadataUtil.ConvertHierarchyLevelToType(md.HierarchyLevel);
                            md.ContactOwner = relData[4] != null
                                ? new Contact { Role = "owner", Organization = relData[4] }
                                : new Contact { Role = "owner", Organization = "" };
                            md.DistributionDetails = new DistributionDetails
                            {
                                Name = relData[5] != null ? relData[5] : "",
                                Protocol = relData[6] != null ? relData[6] : "",
                                ProtocolName = relData[6] != null ? Register.GetDistributionType(relData[6]) : "",
                                URL = relData[7] != null ? relData[7] : ""
                            };
                            if (!IsNullOrEmpty(relData[8]))
                                md.KeywordsNationalTheme = new List<Keyword>
                                {
                                    new Keyword
                                    {
                                        KeywordValue = relData[8],
                                        Thesaurus = SimpleKeyword.THESAURUS_NATIONAL_INITIATIVE
                                    }
                                };
                            md.OrganizationLogoUrl = relData[9];
                            if (!IsNullOrEmpty(relData[10]))
                            {
                                md.Thumbnails = new List<Thumbnail>();
                                md.Thumbnails.Add(new Thumbnail { Type = "miniatyrbilde", URL = relData[10] });
                            }

                            md.Constraints = new Constraints
                            {
                                AccessConstraints = relData[11],
                                OtherConstraintsAccess = relData[12]
                            };

                            if (md.IsService())
                            {
                                md.ServiceUuid = md.Uuid;
                                md.ServiceDistributionAccessConstraint = relData[12];
                                if (relData[6] == "OGC:WMS")
                                {
                                    md.ServiceDistributionProtocolForDataset = relData[6];
                                    md.ServiceDistributionUrlForDataset = relData[7];
                                }
                                if (relData[6] == "OGC:WFS")
                                {
                                    md.ServiceWfsDistributionUrlForDataset = relData[7];
                                }
                            }


                            SearchParameters parametersRelated = new SearchParameters();
                            parametersRelated.Text = md.Uuid;
                            SearchResult searchResultRelated = _searchService.Search(parametersRelated);

                            if (searchResultRelated != null && searchResultRelated.NumFound > 0)
                            {
                                if (md.IsDataset())
                                    md.ServiceUuid = searchResult.Items[0].ServiceDistributionUuidForDataset != null
                                        ? searchResult.Items[0].ServiceDistributionUuidForDataset
                                        : null;
                            }
                            md.AccessIsRestricted = md.IsRestricted();
                            md.AccessIsOpendata = md.IsOpendata();
                            md.AccessIsProtected = md.IsOffline();

                            md.CanShowMapUrl = md.ShowMapLink();
                            md.CanShowServiceMapUrl = md.ShowServiceMapLink();
                            md.CanShowDownloadService = md.ShowDownloadService();
                            md.CanShowDownloadUrl = md.ShowDownloadLink();
                            md.MapLink = md.MapUrl();
                            md.ServiceLink = md.ServiceUrl();



                            metadata.Related.Add(md);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }


                var bundles = searchResult.Items[0].Bundles;

                if (bundles != null && bundles.Count > 0)
                {
                    metadata.Related = new List<MetadataViewModel>();

                    foreach (var relatert in bundles)
                    {
                        var relData = relatert.Split('|');

                        try
                        {
                            MetadataViewModel md = new MetadataViewModel();
                            md.Uuid = relData[0] != null ? relData[0] : "";
                            md.Title = relData[1] != null ? relData[1] : "";
                            md.ParentIdentifier = relData[2] != null ? relData[2] : "";
                            md.HierarchyLevel = relData[3] != null ? relData[3] : "";
                            md.ContactOwner = relData[4] != null
                                ? new Contact { Role = "owner", Organization = relData[4] }
                                : new Contact { Role = "owner", Organization = "" };
                            md.DistributionDetails = new DistributionDetails
                            {
                                Name = relData[5] != null ? relData[5] : "",
                                Protocol = relData[6] != null ? relData[6] : "",
                                ProtocolName = relData[6] != null ? Register.GetDistributionType(relData[6]) : "",
                                URL = relData[7] != null ? relData[7] : ""
                            };
                            if (!IsNullOrEmpty(relData[8]))
                                md.KeywordsNationalTheme = new List<Keyword>
                                {
                                    new Keyword
                                    {
                                        KeywordValue = relData[8],
                                        Thesaurus = SimpleKeyword.THESAURUS_NATIONAL_INITIATIVE
                                    }
                                };
                            md.OrganizationLogoUrl = relData[9];
                            if (!IsNullOrEmpty(relData[10]))
                            {
                                md.Thumbnails = new List<Thumbnail>();
                                md.Thumbnails.Add(new Thumbnail { Type = "miniatyrbilde", URL = relData[10] });
                            }
                            md.Constraints = new Constraints
                            {
                                AccessConstraints = relData[11],
                                OtherConstraintsAccess = relData[12]
                            };
                            if (relData.ElementAtOrDefault(13) != null)
                                md.ServiceUuid = relData[13];
                            if (relData.ElementAtOrDefault(14) != null)
                                md.ServiceDistributionProtocolForDataset = relData[14];
                            if (relData.ElementAtOrDefault(15) != null)
                                md.ServiceDistributionUrlForDataset = relData[15];
                            if (relData.ElementAtOrDefault(16) != null)
                                md.ServiceDistributionNameForDataset = relData[16];
                            if (relData.ElementAtOrDefault(17) != null)
                                md.ServiceWfsDistributionUrlForDataset = relData[17];
                            if (relData.ElementAtOrDefault(18) != null)
                                md.ServiceDistributionAccessConstraint = relData[18];
                            if (relData.ElementAtOrDefault(19) != null)
                                md.ServiceWfsDistributionAccessConstraint = relData[19];

                            metadata.Related.Add(md);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }

                var serviceLayers = searchResult.Items[0].ServiceLayers;

                if (serviceLayers != null && serviceLayers.Count > 0)
                {
                    metadata.Related = new List<MetadataViewModel>();

                    foreach (var relatert in serviceLayers)
                    {
                        var relData = relatert.Split('|');

                        try
                        {
                            MetadataViewModel md = new MetadataViewModel();
                            md.Uuid = relData[0] != null ? relData[0] : "";
                            md.Title = relData[1] != null ? relData[1] : "";
                            md.ParentIdentifier = relData[2] != null ? relData[2] : "";
                            md.ServiceUuid = relData[2] != null ? relData[2] : "";
                            md.HierarchyLevel = relData[3] != null ? relData[3] : "";
                            md.ContactOwner = relData[4] != null
                                ? new Contact { Role = "owner", Organization = relData[4] }
                                : new Contact { Role = "owner", Organization = "" };
                            md.DistributionDetails = new DistributionDetails
                            {
                                Name = relData[5] != null ? relData[5] : "",
                                Protocol = relData[6] != null ? relData[6] : "",
                                ProtocolName = relData[6] != null ? Register.GetDistributionType(relData[6]) : "",
                                URL = relData[7] != null ? relData[7] : ""
                            };
                            if (!IsNullOrEmpty(relData[8]))
                                md.KeywordsNationalTheme = new List<Keyword>
                                {
                                    new Keyword
                                    {
                                        KeywordValue = relData[8],
                                        Thesaurus = SimpleKeyword.THESAURUS_NATIONAL_INITIATIVE
                                    }
                                };
                            md.OrganizationLogoUrl = relData[9];
                            if (!IsNullOrEmpty(relData[10]))
                            {
                                md.Thumbnails = new List<Thumbnail>();
                                md.Thumbnails.Add(new Thumbnail { Type = "miniatyrbilde", URL = relData[10] });
                            }
                            md.Constraints = new Constraints
                            {
                                AccessConstraints = relData[11],
                                OtherConstraintsAccess = relData[12]
                            };

                            md.AccessIsRestricted = md.IsRestricted();
                            md.AccessIsOpendata = md.IsOpendata();
                            md.AccessIsProtected = md.IsOffline();

                            md.CanShowMapUrl = md.ShowMapLink();
                            md.CanShowServiceMapUrl = md.ShowServiceMapLink();
                            md.CanShowDownloadService = md.ShowDownloadService();
                            md.CanShowDownloadUrl = md.ShowDownloadLink();

                            md.MapLink = md.MapUrl();
                            md.ServiceLink = md.ServiceUrl();

                            metadata.Related.Add(md);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }

                var serviceDatasets = searchResult.Items[0].ServiceDatasets;

                if (serviceDatasets != null && serviceDatasets.Count > 0)
                {
                    if (metadata.Related == null)
                        metadata.Related = new List<MetadataViewModel>();

                    foreach (var relatert in serviceDatasets)
                    {
                        var relData = relatert.Split('|');

                        try
                        {
                            MetadataViewModel md = new MetadataViewModel();
                            md.Uuid = relData[0] != null ? relData[0] : "";
                            md.Title = relData[1] != null ? relData[1] : "";
                            md.ParentIdentifier = relData[2] != null ? relData[2] : "";
                            md.HierarchyLevel = relData[3] != null ? relData[3] : "";
                            md.ContactOwner = relData[4] != null
                                ? new Contact { Role = "owner", Organization = relData[4] }
                                : new Contact { Role = "owner", Organization = "" };
                            md.DistributionDetails = new DistributionDetails
                            {
                                Name = relData[5] != null ? relData[5] : "",
                                Protocol = relData[6] != null ? relData[6] : "",
                                ProtocolName = relData[6] != null ? Register.GetDistributionType(relData[6]) : "",
                                URL = relData[7] != null ? relData[7] : ""
                            };
                            if (!IsNullOrEmpty(relData[8]))
                                md.KeywordsNationalTheme = new List<Keyword>
                                {
                                    new Keyword
                                    {
                                        KeywordValue = relData[8],
                                        Thesaurus = SimpleKeyword.THESAURUS_NATIONAL_INITIATIVE
                                    }
                                };
                            md.OrganizationLogoUrl = relData[9];
                            if (!IsNullOrEmpty(relData[10]))
                            {
                                md.Thumbnails = new List<Thumbnail>();
                                md.Thumbnails.Add(new Thumbnail { Type = "miniatyrbilde", URL = relData[10] });
                            }
                            if (!IsNullOrEmpty(relData[11]) && !IsNullOrEmpty(relData[12]))
                            {
                                md.Constraints = new Constraints
                                {
                                    AccessConstraints = relData[11],
                                    OtherConstraintsAccess = relData[12]
                                };
                            }

                            md.AccessIsRestricted = md.IsRestricted();
                            md.AccessIsOpendata = md.IsOpendata();
                            md.AccessIsProtected = md.IsOffline();

                            md.CanShowMapUrl = md.ShowMapLink();
                            md.CanShowDownloadService = md.ShowDownloadService();
                            md.CanShowDownloadUrl = md.ShowDownloadLink();

                            md.MapLink = md.MapUrl();
                            md.ServiceLink = md.ServiceUrl();

                            metadata.Related.Add(md);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }

            }

            metadata.AccessIsRestricted = metadata.IsRestricted();
            metadata.AccessIsOpendata = metadata.IsOpendata();
            metadata.AccessIsProtected = metadata.IsOffline();

            metadata.CanShowMapUrl = metadata.ShowMapLink();
            metadata.CanShowServiceMapUrl = metadata.ShowServiceMapLink();
            metadata.CanShowDownloadService = metadata.ShowDownloadService();
            metadata.CanShowDownloadUrl = metadata.ShowDownloadLink();

            metadata.MapLink = metadata.MapUrl();
            metadata.ServiceLink = metadata.ServiceUrl();

            metadata.CoverageUrl = metadata.GetCoverageLink();

            return metadata;
        }

        private string GetTranslation(string norwegian, string english)
        {
            if (Culture == Models.Translations.Culture.EnglishCode && !string.IsNullOrEmpty(english))
                return english;
            else
                return norwegian;
        }

        private List<Keyword> Convert(IEnumerable<SimpleKeyword> simpleKeywords)
        {
            var output = new List<Keyword>();
            foreach (var keyword in simpleKeywords)
            {
                output.Add(new Keyword
                {
                    EnglishKeyword = keyword.EnglishKeyword,
                    KeywordValue = keyword.Keyword,
                    Thesaurus = keyword.Thesaurus,
                    Type = keyword.Type,
                    KeywordLink = keyword.KeywordLink
                });
            }
            return output;
        }

        private List<Thumbnail> Convert(List<SimpleThumbnail> simpleThumbnails, string uuid)
        {
            var output = new List<Thumbnail>();
            foreach (var simpleThumbnail in simpleThumbnails)
            {
                output.Add(new Thumbnail
                {
                    Type = simpleThumbnail.Type,
                    URL = _geoNetworkUtil.GetThumbnailUrl(uuid, simpleThumbnail.URL)
                });
            }
            return output;
        }

        private List<ReferenceSystem> Convert(List<SimpleReferenceSystem> simpleReferenceSystems)
        {
            var output = new List<ReferenceSystem>();
            foreach (var tmp in simpleReferenceSystems)
            {
                output.Add(Convert(tmp));
            }
            return output;
        }

        private ReferenceSystem Convert(SimpleReferenceSystem simpleReferenceSystem)
        {
            ReferenceSystem output = null;
            if (simpleReferenceSystem != null)
            {
                output = new ReferenceSystem
                {
                    CoordinateSystem = Register.GetCoordinatesystemName(simpleReferenceSystem.CoordinateSystem),
                    CoordinateSystemUrl = simpleReferenceSystem.CoordinateSystem,
                    Namespace = simpleReferenceSystem.Namespace
                };
            }
            return output;
        }

        private List<QualitySpecification> Convert(List<SimpleQualitySpecification> simpleQualitySpecifications)
        {
            List<QualitySpecification> output = new List<QualitySpecification>();
            if (simpleQualitySpecifications != null)
            {
                foreach (var spec in simpleQualitySpecifications)
                {
                    output.Add(new QualitySpecification
                    {
                        Title = spec.Title,
                        Date = (spec.Date != null && !IsNullOrWhiteSpace(spec.Date)) ? DateTime.Parse(spec.Date) : (DateTime?)null,
                        //Date = simpleQualitySpecification.Date,
                        DateType = spec.DateType,
                        Explanation = GetTranslation(spec.Explanation, spec.EnglishExplanation),
                        Result = spec.Result
                    }
                    );
                }
            }
            return output;
        }

        private DistributionFormat Convert(SimpleDistributionFormat simpleDistributionFormat)
        {
            DistributionFormat output = null;
            if (simpleDistributionFormat != null)
            {
                output = new DistributionFormat
                {
                    Name = simpleDistributionFormat.Name,
                    Version = simpleDistributionFormat.Version
                };
            }
            return output;
        }

        private DistributionDetails Convert(SimpleDistributionDetails simpleDistributionDetails)
        {
            DistributionDetails output = null;
            if (simpleDistributionDetails != null)
            {
                output = new DistributionDetails
                {
                    Name = simpleDistributionDetails.Name,
                    Protocol = simpleDistributionDetails.Protocol,
                    ProtocolName = Register.GetDistributionType(simpleDistributionDetails.Protocol),
                    URL = simpleDistributionDetails.URL
                };
            }
            return output;
        }

        private Contact Convert(SimpleContact simpleContact)
        {
            Contact output = null;
            if (simpleContact != null)
            {
                output = new Contact
                {
                    Name = simpleContact.Name,
                    Email = simpleContact.Email,
                    Organization = simpleContact.Organization,
                    OrganizationEnglish = simpleContact.OrganizationEnglish,
                    Role = simpleContact.Role
                };
            }
            return output;
        }

        private Constraints Convert(SimpleConstraints simpleConstraints)
        {
            Constraints output = null;
            if (simpleConstraints != null)
            {
                output = new Constraints
                {
                    AccessConstraints = Register.GetRestriction(simpleConstraints.AccessConstraints, simpleConstraints.OtherConstraintsAccess),
                    OtherConstraints = simpleConstraints.OtherConstraints,
                    OtherConstraintsLink = simpleConstraints.OtherConstraintsLink,
                    OtherConstraintsLinkText = simpleConstraints.OtherConstraintsLinkText,
                    SecurityConstraints = Register.GetClassification(simpleConstraints.SecurityConstraints),
                    SecurityConstraintsNote = simpleConstraints.SecurityConstraintsNote,
                    UseConstraints = Register.GetRestriction(simpleConstraints.UseConstraints),
                    UseLimitations = GetTranslation(simpleConstraints.UseLimitations, simpleConstraints.EnglishUseLimitations),
                    OtherConstraintsAccess = simpleConstraints.OtherConstraintsAccess
                };
            }
            return output;
        }

        private BoundingBox Convert(SimpleBoundingBox simpleBoundingBox)
        {
            BoundingBox output = null;
            if (simpleBoundingBox != null)
            {
                output = new BoundingBox
                {
                    EastBoundLongitude = simpleBoundingBox.EastBoundLongitude,
                    NorthBoundLatitude = simpleBoundingBox.NorthBoundLatitude,
                    SouthBoundLatitude = simpleBoundingBox.SouthBoundLatitude,
                    WestBoundLongitude = simpleBoundingBox.WestBoundLongitude
                };
            }
            return output;
        }

    }
}