﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.FullText;
using VDS.RDF.Query.FullText.Schema;
using VDS.RDF.Query.FullText.Search;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Query
{
    static class FullTextExtensions
    {
        private static NodeFactory _factory = new NodeFactory();
        private static SHA256Managed _sha256;

        internal static ISet ToSet(this IFullTextSearchResult result, String matchVar, String scoreVar)
        {
            Set s = new Set();
            if (matchVar != null) s.Add(matchVar, result.Node);
            if (scoreVar != null) s.Add(scoreVar, result.Score.ToLiteral(_factory));
            return s;
        }

        internal static IFullTextSearchResult ToResult(this Document doc, double score, IFullTextIndexSchema schema)
        {
            //First get the node type
            Field nodeTypeField = doc.GetField(schema.NodeTypeField);
            if (nodeTypeField == null) throw new RdfQueryException("Node Type field " + schema.NodeTypeField + " not present on a retrieved document.  Please check you have configured the Index Schema correctly");
            NodeType nodeType;
            try 
            {
                nodeType = (NodeType)Enum.Parse(typeof(NodeType), nodeTypeField.StringValue());
            } 
            catch 
            {
                throw new RdfQueryException("Node Type field " + schema.NodeTypeField + " contained an invalid value '" + nodeTypeField.StringValue() + "'.  Please check you have configured the Index Schema correctly");
            }

            //Then get the node value
            Field nodeValueField = doc.GetField(schema.NodeValueField);
            if (nodeValueField == null) throw new RdfQueryException("Node Value field " + schema.NodeValueField + " not present on a retrieved document.  Please check you have configured the Index Schema correctly");
            String nodeValue = nodeValueField.StringValue();

            //Then depending on the Node Type determine whether we need to obtain the Meta Field as well
            switch (nodeType)
            {
                case NodeType.Blank:
                    //Can just create a Blank Node
                    return new FullTextSearchResult(_factory.CreateBlankNode(nodeValue), score);

                case NodeType.Literal:
                    //Need to get Meta field to determine whether we have a language or datatype present
                    Field nodeMetaField = doc.GetField(schema.NodeMetaField);
                    if (nodeMetaField == null)
                    {
                        //Assume a Plain Literal
                        return new FullTextSearchResult(_factory.CreateLiteralNode(nodeValue), score);
                    }
                    else
                    {
                        String nodeMeta = nodeMetaField.StringValue();
                        if (nodeMeta.StartsWith("@"))
                        {
                            //Language Specified literal
                            return new FullTextSearchResult(_factory.CreateLiteralNode(nodeValue, nodeMeta), score);
                        }
                        else
                        {
                            //Assume a Datatyped literal
                            return new FullTextSearchResult(_factory.CreateLiteralNode(nodeValue, new Uri(nodeMeta)), score);
                        }
                    }
                    break;

                case NodeType.Uri:
                    //Can just create a URI Node
                    return new FullTextSearchResult(_factory.CreateUriNode(new Uri(nodeValue)), score);

                default:
                    throw new RdfQueryException("Only Blank, Literal and URI Nodes may be retrieved from a Lucene Document");
            }
        }

        internal static String ToLuceneFieldValue(this NodeType type)
        {
            return ((int)type).ToString();
        }

        internal static String ToLuceneFieldValue(this INode n)
        {
            switch (n.NodeType)
            {
                case NodeType.Blank:
                    return ((IBlankNode)n).InternalID;
                case NodeType.Literal:
                    return ((ILiteralNode)n).Value;
                case NodeType.Uri:
                    return n.ToString();
                default:
                    throw new FullTextIndexException("Only Blank, Literal and URI Nodes may be indexed using Lucene");
            }
        }

        internal static String ToLuceneFieldMeta(this INode n)
        {
            switch (n.NodeType)
            {
                case NodeType.Blank:
                case NodeType.Uri:
                    return null;

                case NodeType.Literal:
                    ILiteralNode lit = (ILiteralNode)n;
                    if (lit.DataType != null)
                    {
                        return lit.DataType.ToString();
                    }
                    else if (!lit.Language.Equals(String.Empty))
                    {
                        return "@" + lit.Language;
                    }
                    else
                    {
                        return null;
                    }

                default:
                    throw new FullTextIndexException("Only Blank, Literal and URI Nodes may be indexed using Lucene");
            }
        }

        /// <summary>
        /// Gets a SHA256 Hash for a String
        /// </summary>
        /// <param name="s">String to hash</param>
        /// <returns></returns>
        internal static String GetSha256Hash(this String s)
        {
            if (s == null) throw new ArgumentNullException("s");

            //Only instantiate the SHA256 class when we first use it
            if (_sha256 == null) _sha256 = new SHA256Managed();

            Byte[] input = Encoding.UTF8.GetBytes(s);
            Byte[] output = _sha256.ComputeHash(input);

            StringBuilder hash = new StringBuilder();
            foreach (Byte b in output)
            {
                hash.Append(b.ToString("x2"));
            }

            return hash.ToString();
        }

        internal static void SerializeConfiguration(this Directory directory, ConfigurationSerializationContext context)
        {
            INode rdfType = context.Graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            INode dnrType = ConfigurationLoader.CreateConfigurationNode(context.Graph, ConfigurationLoader.PropertyType);
            INode indexClass = context.Graph.CreateUriNode(new Uri(FullTextHelper.ClassIndex));
            INode dirObj = context.NextSubject;

            context.Graph.Assert(dirObj, rdfType, indexClass);
            context.Graph.Assert(dirObj, context.Graph.CreateUriNode(new Uri(FullTextHelper.PropertyEnsureIndex)), (true).ToLiteral(context.Graph));
            if (directory is RAMDirectory)
            {
                context.Graph.Assert(dirObj, dnrType, context.Graph.CreateLiteralNode(directory.GetType().Name + ", Lucene.Net"));
            }
            else if (directory is FSDirectory)
            {
                context.Graph.Assert(dirObj, dnrType, context.Graph.CreateLiteralNode(directory.GetType().Name + ", Lucene.Net"));
                context.Graph.Assert(dirObj, ConfigurationLoader.CreateConfigurationNode(context.Graph, ConfigurationLoader.PropertyFromFile), context.Graph.CreateLiteralNode(((FSDirectory)directory).GetDirectory().FullName));
            }
            else
            {
                throw new DotNetRdfConfigurationException("dotNetRDF.Query.FullText only supports automatically serializing configuration for Lucene indexes that use RAMDirectory or FSDirectory currently");
            }
        }

        internal static void SerializeConfiguration(this Analyzer analyzer, ConfigurationSerializationContext context)
        {
            INode rdfType = context.Graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            INode dnrType = ConfigurationLoader.CreateConfigurationNode(context.Graph, ConfigurationLoader.PropertyType);
            INode analyzerClass = context.Graph.CreateUriNode(new Uri(FullTextHelper.ClassAnalyzer));
            INode analyzerObj = context.NextSubject;

            Type t = analyzer.GetType();
            if (t.GetConstructor(Type.EmptyTypes) != null || t.GetConstructor(new Type[] { typeof(Lucene.Net.Util.Version) }) != null)
            {
                context.Graph.Assert(analyzerObj, rdfType, analyzerClass);
                context.Graph.Assert(analyzerClass, dnrType, context.Graph.CreateLiteralNode(t.Name + ", " + t.Assembly.FullName));
            }
            else
            {
                throw new DotNetRdfConfigurationException("dotNetRDF.Query.FullText only supports automatically serializing configuration for Lucene analyzers that have an unparameterised constructor or a constructor that takes a Version parameter");
            }
        }
    }
}
