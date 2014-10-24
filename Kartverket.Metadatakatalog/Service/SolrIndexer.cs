﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kartverket.Metadatakatalog.Models;
using Microsoft.Practices.ServiceLocation;
using SolrNet;

namespace Kartverket.Metadatakatalog.Service
{
    public class SolrIndexer : Indexer
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISolrOperations<MetadataIndexDoc> _solr;

        public SolrIndexer()
        {
            _solr = ServiceLocator.Current.GetInstance<ISolrOperations<MetadataIndexDoc>>();
        }

        public void Index(IEnumerable<MetadataIndexDoc> docs)
        {
            Log.Info(string.Format("Indexing {0} docs", docs.Count()));
            _solr.AddRange(docs);
            _solr.Commit();
        }
    }
}