﻿using System;
using System.Web.Mvc;
using Kartverket.Metadatakatalog.Models;
using Kartverket.Metadatakatalog.Models.ViewModels;
using Kartverket.Metadatakatalog.Service;
using Kartverket.Metadatakatalog.Service.Search;

namespace Kartverket.Metadatakatalog.Controllers
{
    public class MetadataController : Controller
    {
        private readonly IMetadataService _metadataService;
        private readonly ISearchService _searchService;

        public MetadataController(IMetadataService metadataService, ISearchService searchService)
        {
            _metadataService = metadataService;
            _searchService = searchService;
        }

        public ActionResult Index(string uuid, string organization = null, string title = null)
        {
            MetadataViewModel model = null;
            try
            {
                model = _metadataService.GetMetadataByUuid(uuid);
            }
            catch (InvalidOperationException exception)
            {
                // todo log exception    
            }
            if (model == null)
                return new HttpNotFoundResult();
            
            SeoUrl url = model.CreateSeoUrl();
            if (!url.Matches(organization, title) && !string.IsNullOrWhiteSpace(organization))
                return RedirectToActionPermanent("Index", new { organization = url.Organization, title = url.Title, uuid = uuid });

            return View(model);
        }

        public ActionResult Organization(SearchByOrganizationParameters parameters)
        {
            parameters.AddDefaultFacetsIfMissing();
            SearchResult searchResult = _searchService.SearchByOrganization(parameters);
            var model = new SearchByOrganizationViewModel(parameters, searchResult);
            return View(model);
        }
    }
}