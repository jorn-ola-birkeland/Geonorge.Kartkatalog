﻿using System.Collections.Generic;
using Kartverket.Metadatakatalog.Models;
using Microsoft.Practices.ServiceLocation;
using SolrNet;
using SolrNet.Commands.Parameters;
using System;

namespace Kartverket.Metadatakatalog.Service.Application
{
    public class ApplicationService : IApplicationService
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ISolrOperations<ApplicationIndexDoc> _solrInstance;
        RegisterFetcher register;

        public ApplicationService()
        {
            _solrInstance = ServiceLocator.Current.GetInstance<ISolrOperations<ApplicationIndexDoc>>();
        }

        public SearchResult Applications(SearchParameters parameters)
        {
            ISolrQuery query = parameters.BuildQuery();
            try
            {
                SolrQueryResults<ApplicationIndexDoc> queryResults = _solrInstance.Query(query, new QueryOptions
                {                                     
                    //WMS lag skal få redusert sin boost
                    FilterQueries = parameters.BuildFilterQueries(),
                    OrderBy = parameters.OrderBy(),
                    Rows = parameters.Limit,
                    StartOrCursor = new StartOrCursor.Start(parameters.Offset - 1), //solr is zero-based - we use one-based indexing in api
                    Facet = parameters.BuildFacetParameters(),
                    Fields = new[] { "uuid", "title", "abstract", "purpose", "type", "theme", "organization", "organization_seo_lowercase", "organization_shortname", "placegroups", "organizationgroup",
                    "topic_category", "organization_logo_url",  "thumbnail_url","distribution_url","distribution_protocol","distribution_name","product_page_url", "date_published", "date_updated", "nationalinitiative",
                    "score", "ServiceDistributionProtocolForDataset", "ServiceDistributionUrlForDataset", "ServiceDistributionNameForDataset", "DistributionProtocols", "legend_description_url", "product_sheet_url", "product_specification_url", "area", "datasetservice", "popularMetadata", "bundle", "servicelayers", "accessconstraint", "servicedataset", "otherconstraintsaccess", "dataaccess", "ServiceDistributionUuidForDataset", "ServiceDistributionAccessConstraint", "parentidentifier", "DistributionType" }
                    //ExtraParams = new Dictionary<string, string> {
                    //    {"q", ""}
                    //}
                    
                });

                return CreateSearchResults(queryResults, parameters);
            }
            catch (Exception ex)
            {
                Log.Error("Error in search", ex);

                return CreateSearchResults(null, parameters);
            }
            
        }


        private SearchResult CreateSearchResults(SolrQueryResults<ApplicationIndexDoc> queryResults, SearchParameters parameters)
        {
            List<SearchResultItem> items = ParseResultDocuments(queryResults);

            List<Facet> facets = ParseFacetResults(queryResults);

            return new SearchResult
            {
                Items = items,
                Facets = facets,
                Limit = parameters.Limit,
                Offset = parameters.Offset,
                NumFound = GetNumFound(queryResults),
                Type = GetType(queryResults),
            };
        }

        private int GetNumFound(SolrQueryResults<ApplicationIndexDoc> queryResults)
        {
            return queryResults?.NumFound ?? 0;
        }

        private string GetType(SolrQueryResults<ApplicationIndexDoc> queryResults)
        {
            if (queryResults != null && queryResults.Count > 0)
            {
                return queryResults[0].ClassName;
            }
            return null;
        }

        private List<Facet> ParseFacetResults(SolrQueryResults<ApplicationIndexDoc> queryResults)
        {
            List<Facet> facets = new List<Facet>();
            if (queryResults != null)
            {
                foreach (var key in queryResults.FacetFields.Keys)
                {
                    var facet = new Facet(key);
                    foreach (var facetValueResult in queryResults.FacetFields[key])
                    {
                        facet.FacetResults.Add(new Facet.FacetValue(facetValueResult));
                    }
                    facets.Add(facet);
                }
            }
            return facets;
        }

        private List<SearchResultItem> ParseResultDocuments(SolrQueryResults<ApplicationIndexDoc> queryResults)
        {
            register = new RegisterFetcher();
            var items = new List<SearchResultItem>();
            if (queryResults != null)
            {
                foreach (var doc in queryResults)
                {
                    Log.Debug(doc.Score + " " + doc.Title + " " + doc.Uuid);

                    var item = new SearchResultItem(doc);
                    item.DistributionType = register.GetDistributionType(item.DistributionProtocol);
                    items.Add(item);
                }
            }
            return items;
        }
    }
}