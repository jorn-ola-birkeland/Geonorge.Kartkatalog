﻿using System.Collections.Generic;
using System.Linq;

namespace Kartverket.Metadatakatalog.Models.Api
{
    public class Facet
    {
        /// <summary>
        /// The name of the facet field
        /// </summary>
        public string FacetField { get; set; }
        /// <summary>
        /// The facet result
        /// </summary>
        public List<FacetValue> FacetResults { get; set; }
        public Facet() { }
        private Facet(Models.Facet item)
        {
            FacetField = item.FacetField;
            FacetResults = FacetValue.CreateFromList(item.FacetResults);
        }

        public static List<Facet> CreateFromList(IEnumerable<Models.Facet> facets)
        {
            return facets.Select(item => new Facet(item)).ToList();
        }

        public class FacetValue
        {
            /// <summary>
            /// The name of the facet result
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// The number of items that has this facet
            /// </summary>
            public int Count { get; set; }
            public FacetValue() { }

            private FacetValue(Models.Facet.FacetValue item)
            {
                Name = item.Name;
                Count = item.Count;
            }

            public static List<FacetValue> CreateFromList(IEnumerable<Models.Facet.FacetValue> facetResults)
            {
                return facetResults.Select(item => new FacetValue(item)).ToList();
            }
        }

    }
}