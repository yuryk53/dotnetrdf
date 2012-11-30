using System;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Query.Builder
{
    public interface IDescribeGraphPatternBuilder
    {
        /// <summary>
        /// Adds triple patterns to the SPARQL query or graph pattern
        /// </summary>
        IDescribeGraphPatternBuilder Where(params ITriplePattern[] triplePatterns);
        /// <summary>
        /// Adds triple patterns to the SPARQL query or graph pattern
        /// </summary>
        IDescribeGraphPatternBuilder Where(Action<ITriplePatternBuilder> buildTriplePatterns);
    }
}